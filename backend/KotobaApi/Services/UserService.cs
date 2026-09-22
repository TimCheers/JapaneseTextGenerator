using KotobaApi.Data;
using KotobaApi.Models;
using Microsoft.EntityFrameworkCore;

namespace KotobaApi.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _db;
    public UserService(AppDbContext db) => _db = db;

    public async Task<UserDto?> GetByIdAsync(Guid id)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
        return user is null ? null : ToDto(user);
    }

    public async Task<UserDto> RegisterAsync(RegisterUserDto dto)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            DisplayName = dto.DisplayName,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            NativeLanguage = dto.NativeLanguage
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return ToDto(user);
    }

    private static UserDto ToDto(User u) =>
        new(u.Id, u.DisplayName, u.Email, u.NativeLanguage, u.CreatedAt);
}