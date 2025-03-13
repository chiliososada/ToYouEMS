using System.Security.Claims;
using System.Text;
using ToYouEMS.ToYouEMS.Core.Interfaces;
using ToYouEMS.ToYouEMS.Core.Models.DTOs;
using ToYouEMS.ToYouEMS.Core.Models.Entities;

namespace ToYouEMS.ToYouEMS.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;

        public AuthService(IUnitOfWork unitOfWork, IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _configuration = configuration;
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            var user = await _unitOfWork.Users.SingleOrDefaultAsync(u => u.Username == request.Username);

            if (user == null || !BC.Verify(request.Password, user.Password))
            {
                return new AuthResponse
                {
                    IsSuccess = false,
                    Message = "用户名或密码错误"
                };
            }

            if (!user.IsActive)
            {
                return new AuthResponse
                {
                    IsSuccess = false,
                    Message = "用户账号已被禁用"
                };
            }

            return new AuthResponse
            {
                UserID = user.UserID,
                Username = user.Username,
                UserType = user.UserType,
                Token = GenerateJwtToken(user),
                IsSuccess = true,
                Message = "登录成功"
            };
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            var usernameExists = await _unitOfWork.Users.SingleOrDefaultAsync(u => u.Username == request.Username);
            if (usernameExists != null)
            {
                return new AuthResponse
                {
                    IsSuccess = false,
                    Message = "用户名已存在"
                };
            }

            var emailExists = await _unitOfWork.Users.SingleOrDefaultAsync(u => u.Email == request.Email);
            if (emailExists != null)
            {
                return new AuthResponse
                {
                    IsSuccess = false,
                    Message = "电子邮箱已存在"
                };
            }

            var hashedPassword = BC.HashPassword(request.Password);

            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                Password = hashedPassword,
                UserType = request.UserType,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.CompleteAsync();

            // 创建默认的个人资料
            var profile = new Profile
            {
                UserID = user.UserID,
                FullName = user.Username // 默认使用用户名作为全名
            };

            await _unitOfWork.Profiles.AddAsync(profile);
            await _unitOfWork.CompleteAsync();

            // 记录日志
            await _unitOfWork.Logs.AddAsync(new Log
            {
                UserID = user.UserID,
                Action = "Register",
                Description = $"用户 {user.Username} 注册成功",
                LogTime = DateTime.Now
            });
            await _unitOfWork.CompleteAsync();

            return new AuthResponse
            {
                UserID = user.UserID,
                Username = user.Username,
                UserType = user.UserType,
                Token = GenerateJwtToken(user),
                IsSuccess = true,
                Message = "注册成功"
            };
        }

        public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null || !BC.Verify(currentPassword, user.Password))
            {
                return false;
            }

            user.Password = BC.HashPassword(newPassword);
            _unitOfWork.Users.Update(user);
            await _unitOfWork.CompleteAsync();

            // 记录日志
            await _unitOfWork.Logs.AddAsync(new Log
            {
                UserID = userId,
                Action = "ChangePassword",
                Description = $"用户 {user.Username} 修改了密码",
                LogTime = DateTime.Now
            });
            await _unitOfWork.CompleteAsync();

            return true;
        }

        private string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserID.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
                new Claim("userType", user.UserType.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"]
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
