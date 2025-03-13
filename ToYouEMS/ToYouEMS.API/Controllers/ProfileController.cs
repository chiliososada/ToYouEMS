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

        public ProfileController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
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
                LogTime = DateTime.Now
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

            // 处理头像上传
            // TODO: 实现文件存储服务，这里只是示例
            var avatarUrl = "/uploads/avatars/" + Guid.NewGuid().ToString() + ".jpg";
            profile.AvatarUrl = avatarUrl;

            _unitOfWork.Profiles.Update(profile);
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
