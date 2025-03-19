using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ToYouEMS.ToYouEMS.Core.Interfaces;
using ToYouEMS.ToYouEMS.Core.Models.DTOs;
using ToYouEMS.ToYouEMS.Core.Models.Entities;

    namespace ToYouEMS.ToYouEMS.API.Controllers
    {
        [Authorize(Roles = "Admin")] // 只允许管理员访问
        [Route("api/[controller]")]
        [ApiController]
        public class UserController : ControllerBase
        {
            private readonly IUnitOfWork _unitOfWork;

            public UserController(IUnitOfWork unitOfWork)
            {
                _unitOfWork = unitOfWork;
            }

            // 获取所有用户
            [HttpGet]
            public async Task<IActionResult> GetUsers()
            {
                var users = await _unitOfWork.Users.Find(u => true)
                    .Select(u => new UserDTO
                    {
                        UserID = u.UserID,
                        Username = u.Username,
                        Email = u.Email,
                        UserType = u.UserType,
                        IsActive = u.IsActive
                    })
                    .ToListAsync();

                return Ok(users);
            }

            // 根据ID获取用户
            [HttpGet("{id}")]
            public async Task<IActionResult> GetUserById(int id)
            {
                var user = await _unitOfWork.Users.Find(u => u.UserID == id)
                    .Select(u => new UserDTO
                    {
                        UserID = u.UserID,
                        Username = u.Username,
                        Email = u.Email,
                        UserType = u.UserType,
                        IsActive = u.IsActive
                    })
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return NotFound(new { message = "用户不存在" });
                }

                return Ok(user);
            }

            // 管理员重置用户密码
            [HttpPost("reset-password")]
            public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
            {
                var user = await _unitOfWork.Users.GetByIdAsync(request.UserId);
                if (user == null)
                {
                    return NotFound(new { message = "用户不存在" });
                }

                // 使用BCrypt哈希新密码
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
                user.Password = hashedPassword;

                _unitOfWork.Users.Update(user);
                await _unitOfWork.CompleteAsync();

                // 记录日志
                var adminId = int.Parse(User.FindFirst("sub")?.Value);
                await _unitOfWork.Logs.AddAsync(new Log
                {
                    UserID = adminId,
                    Action = "ResetPassword",
                    Description = $"管理员重置了用户 {user.Username} (ID: {user.UserID}) 的密码",
                    LogTime = DateTime.UtcNow
                });
                await _unitOfWork.CompleteAsync();

                return Ok(new { message = "密码已重置成功" });
            }

            // 更新用户状态（启用/禁用）
            [HttpPut("{id}/status")]
            public async Task<IActionResult> UpdateUserStatus(int id, [FromBody] UpdateUserStatusRequest request)
            {
                var user = await _unitOfWork.Users.GetByIdAsync(id);
                if (user == null)
                {
                    return NotFound(new { message = "用户不存在" });
                }

                // 防止禁用最后一个管理员账户
                if (user.UserType == UserType.Admin && !request.IsActive)
                {
                    // 检查系统中是否还有其他活跃的管理员
                    var activeAdminCount = await _unitOfWork.Users
                        .Find(u => u.UserType == UserType.Admin && u.IsActive && u.UserID != id)
                        .CountAsync();

                    if (activeAdminCount == 0)
                    {
                        return BadRequest(new { message = "无法禁用最后一个管理员账户" });
                    }
                }

                user.IsActive = request.IsActive;
                _unitOfWork.Users.Update(user);
                await _unitOfWork.CompleteAsync();

                // 记录日志
                var adminId = int.Parse(User.FindFirst("sub")?.Value);
                await _unitOfWork.Logs.AddAsync(new Log
                {
                    UserID = adminId,
                    Action = "UpdateUserStatus",
                    Description = $"管理员将用户 {user.Username} (ID: {user.UserID}) 的状态更新为 {(request.IsActive ? "启用" : "禁用")}",
                    LogTime = DateTime.UtcNow
                });
                await _unitOfWork.CompleteAsync();

                return Ok(new { message = $"用户状态已更新为 {(request.IsActive ? "启用" : "禁用")}" });
            }

            // 更新用户信息
            [HttpPut("{id}")]
            public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request)
            {
                var user = await _unitOfWork.Users.GetByIdAsync(id);
                if (user == null)
                {
                    return NotFound(new { message = "用户不存在" });
                }

                // 检查用户名是否已被使用
                if (request.Username != user.Username)
                {
                    var existingUsername = await _unitOfWork.Users.SingleOrDefaultAsync(u => u.Username == request.Username);
                    if (existingUsername != null)
                    {
                        return BadRequest(new { message = "用户名已被使用" });
                    }
                }

                // 检查邮箱是否已被使用
                if (request.Email != user.Email)
                {
                    var existingEmail = await _unitOfWork.Users.SingleOrDefaultAsync(u => u.Email == request.Email);
                    if (existingEmail != null)
                    {
                        return BadRequest(new { message = "电子邮箱已被使用" });
                    }
                }

                // 更新用户信息
                user.Username = request.Username ?? user.Username;
                user.Email = request.Email ?? user.Email;

                // 如果要改变用户类型，需要检查是否会导致没有管理员
                if (request.UserType.HasValue && request.UserType.Value != (int)user.UserType)
                {
                    // 如果将管理员改为其他类型，检查是否还有其他管理员
                    if (user.UserType == UserType.Admin && request.UserType.Value != (int)UserType.Admin)
                    {
                        // 检查系统中是否还有其他活跃的管理员
                        var activeAdminCount = await _unitOfWork.Users
                            .Find(u => u.UserType == UserType.Admin && u.IsActive && u.UserID != id)
                            .CountAsync();

                        if (activeAdminCount == 0)
                        {
                            return BadRequest(new { message = "无法改变最后一个管理员的用户类型" });
                        }
                    }

                    user.UserType = (UserType)request.UserType.Value;
                }

                _unitOfWork.Users.Update(user);
                await _unitOfWork.CompleteAsync();

                // 记录日志
                var adminId = int.Parse(User.FindFirst("sub")?.Value);
                await _unitOfWork.Logs.AddAsync(new Log
                {
                    UserID = adminId,
                    Action = "UpdateUser",
                    Description = $"管理员更新了用户 {user.Username} (ID: {user.UserID}) 的信息",
                    LogTime = DateTime.UtcNow
                });
                await _unitOfWork.CompleteAsync();

                return Ok(new { message = "用户信息已更新" });
            }
        }

        // Request DTOs
        public class ResetPasswordRequest
        {
            public int UserId { get; set; }
            public string NewPassword { get; set; }
        }

        public class UpdateUserStatusRequest
        {
            public bool IsActive { get; set; }
        }

        public class UpdateUserRequest
        {
            public string Username { get; set; }
            public string Email { get; set; }
            public int? UserType { get; set; }
        }
    }