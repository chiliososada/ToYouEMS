using ToYouEMS.ToYouEMS.Core.Models.DTOs;

namespace ToYouEMS.ToYouEMS.Core.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
    }
}
