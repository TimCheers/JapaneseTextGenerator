using KotobaApi.Data;
using KotobaApi.Models;
using Microsoft.EntityFrameworkCore;

namespace KotobaApi.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _db;
    private readonly ITokenService _tokenService;

    public UserService(AppDbContext db, ITokenService tokenService)
    {
        _db = db;
        _tokenService = tokenService;
    }

    public async Task<UserDto?> GetByIdAsync(Guid id)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
        return user is null ? null : ToDto(user);
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterUserDto dto)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            DisplayName = dto.DisplayName,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            NativeLanguage = dto.NativeLanguage
        };

        if (await _db.Users.AnyAsync(u => u.Email == user.Email)) throw new InvalidOperationException();
        
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return ToDto(_tokenService.CreateToken(user),
            new UserDto(user.Id, user.DisplayName, user.Email, user.NativeLanguage, user.CreatedAt));
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginDto dto)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user is null) return null;
        if (BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return ToDto(_tokenService.CreateToken(user),
                new UserDto(user.Id, user.DisplayName, user.Email, user.NativeLanguage, user.CreatedAt));
        else return null;
    }

    private static AuthResponseDto ToDto(string token, UserDto u) =>
        new(token, u);

    private static UserDto ToDto(User u) =>
        new(u.Id, u.DisplayName, u.Email, u.NativeLanguage, u.CreatedAt);
}