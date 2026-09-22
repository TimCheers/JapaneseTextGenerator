namespace KotobaApi.Services;

public interface IUserService
{
    Task<UserDto?> GetByIdAsync(Guid id);
    Task<UserDto> RegisterAsync(RegisterUserDto dto);
}