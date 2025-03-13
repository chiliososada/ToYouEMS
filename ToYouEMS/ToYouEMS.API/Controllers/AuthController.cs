using Microsoft.AspNetCore.Mvc;
using ToYouEMS.ToYouEMS.Core.Interfaces;
using ToYouEMS.ToYouEMS.Core.Models.DTOs;

namespace ToYouEMS.ToYouEMS.API.Controllers
{

        public class AuthController : ControllerBase
        {
            private readonly IAuthService _authService;

            public AuthController(IAuthService authService)
            {
                _authService = authService;
            }

            [HttpPost("login")]
            public async Task<IActionResult> Login(LoginRequest request)
            {
                var response = await _authService.LoginAsync(request);

                if (!response.IsSuccess)
                {
                    return BadRequest(response);
                }

                return Ok(response);
            }

            [HttpPost("register")]
            public async Task<IActionResult> Register(RegisterRequest request)
            {
                var response = await _authService.RegisterAsync(request);

                if (!response.IsSuccess)
                {
                    return BadRequest(response);
                }

                return Ok(response);
            }

            [HttpPost("change-password")]
            public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
            {
                var userId = int.Parse(User.FindFirst("sub")?.Value);
                var result = await _authService.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword);

                if (!result)
                {
                    return BadRequest(new { message = "当前密码不正确" });
                }

                return Ok(new { message = "密码修改成功" });
            }
        }

        public class ChangePasswordRequest
        {
            public string CurrentPassword { get; set; }
            public string NewPassword { get; set; }
        }
    }

