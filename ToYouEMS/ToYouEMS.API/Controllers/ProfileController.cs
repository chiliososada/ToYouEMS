using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToYouEMS.ToYouEMS.Core.Interfaces;
using ToYouEMS.ToYouEMS.Core.Models.Entities;

namespace ToYouEMS.ToYouEMS.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ProfileController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileStorageService _fileStorageService; // 添加文件存储服务

        public ProfileController(IUnitOfWork unitOfWork, IFileStorageService fileStorageService)
        {
            _unitOfWork = unitOfWork;
            _fileStorageService = fileStorageService;
        }

        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            var userId = int.Parse(User.FindFirst("sub")?.Value);
            var profile = await _unitOfWork.Profiles.SingleOrDefaultAsync(p => p.UserID == userId);

            if (profile == null)
            {
                return NotFound(new { message = "未找到个人资料" });
            }

            return Ok(profile);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateProfile(ProfileUpdateRequest request)
        {
            var userId = int.Parse(User.FindFirst("sub")?.Value);
            var profile = await _unitOfWork.Profiles.SingleOrDefaultAsync(p => p.UserID == userId);

            if (profile == null)
            {
                return NotFound(new { message = "未找到个人资料" });
            }

            // 更新个人资料
            profile.FullName = request.FullName;
            profile.Gender = request.Gender;
            profile.BirthDate = request.BirthDate;
            profile.BirthPlace = request.BirthPlace;
            profile.Address = request.Address;
            profile.Introduction = request.Introduction;
            profile.Hobbies = request.Hobbies;

            _unitOfWork.Profiles.Update(profile);
            await _unitOfWork.CompleteAsync();

            // 记录日志
            await _unitOfWork.Logs.AddAsync(new Log
            {
                UserID = userId,
                Action = "UpdateProfile",
                Description = "更新了个人资料",
                LogTime = DateTime.UtcNow
            });
            await _unitOfWork.CompleteAsync();

            return Ok(new { message = "个人资料更新成功" });
        }

        [HttpPost("avatar")]
        public async Task<IActionResult> UploadAvatar([FromForm] AvatarUploadRequest request)
        {
            var userId = int.Parse(User.FindFirst("sub")?.Value);
            var profile = await _unitOfWork.Profiles.SingleOrDefaultAsync(p => p.UserID == userId);

            if (profile == null)
            {
                return NotFound(new { message = "未找到个人资料" });
            }

            if (request.Avatar == null || request.Avatar.Length == 0)
            {
                return BadRequest(new { message = "请选择要上传的头像" });
            }

            // 检查文件类型
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(request.Avatar.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                return BadRequest(new { message = "仅支持JPG、PNG和GIF格式的图片" });
            }

            // 删除旧头像文件（如果存在）
            if (!string.IsNullOrEmpty(profile.AvatarUrl) && _fileStorageService.FileExists(profile.AvatarUrl))
            {
                await _fileStorageService.DeleteFileAsync(profile.AvatarUrl);
            }

            // 保存新头像
            var avatarUrl = await _fileStorageService.SaveFileAsync(request.Avatar, "avatars");
            profile.AvatarUrl = avatarUrl;

            _unitOfWork.Profiles.Update(profile);
            await _unitOfWork.CompleteAsync();

            // 记录日志
            await _unitOfWork.Logs.AddAsync(new Log
            {
                UserID = userId,
                Action = "UploadAvatar",
                Description = "上传了新头像",
                LogTime = DateTime.UtcNow
            });
            await _unitOfWork.CompleteAsync();

            return Ok(new { message = "头像上传成功", avatarUrl });
        }
    }

    public class ProfileUpdateRequest
    {
        public string FullName { get; set; }
        public string Gender { get; set; }
        public DateTime? BirthDate { get; set; }
        public string BirthPlace { get; set; }
        public string Address { get; set; }
        public string Introduction { get; set; }
        public string Hobbies { get; set; }
    }

    public class AvatarUploadRequest
    {
        public Microsoft.AspNetCore.Http.IFormFile Avatar { get; set; }
    }
}
