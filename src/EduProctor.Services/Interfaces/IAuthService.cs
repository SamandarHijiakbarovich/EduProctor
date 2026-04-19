using EduProctor.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EduProctor.Services.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto> LoginAsync(LoginDto dto);
        Task<AuthResponseDto> RegisterAsync(RegisterDto dto);
        Task<AuthResponseDto> RefreshTokenAsync(string refreshToken);
        Task LogoutAsync(Guid userId);
        Task<UserDto> GetCurrentUserAsync(Guid userId);
        Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordDto dto);
    }
}
