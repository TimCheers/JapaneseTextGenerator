namespace KotobaApi.Services;

public interface IUserService
{
    Task<UserDto?> GetByIdAsync(Guid id);
    Task<AuthResponseDto> RegisterAsync(RegisterUserDto dto);
    Task<AuthResponseDto?> LoginAsync(LoginDto dto);
}