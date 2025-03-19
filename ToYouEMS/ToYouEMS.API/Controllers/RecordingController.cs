using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ToYouEMS.ToYouEMS.Core.Interfaces;
using ToYouEMS.ToYouEMS.Core.Models.Entities;

namespace ToYouEMS.ToYouEMS.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class RecordingController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileStorageService _fileStorageService;

        public RecordingController(IUnitOfWork unitOfWork, IFileStorageService fileStorageService)
        {
            _unitOfWork = unitOfWork;
            _fileStorageService = fileStorageService;
        }

        [HttpGet]
        public async Task<IActionResult> GetRecordings()
        {
            var userId = int.Parse(User.FindFirst("sub")?.Value);
            var userType = User.FindFirst("userType")?.Value;

            IQueryable<Recording> query;

            // 根据用户角色决定查询范围
            if (userType == UserType.Admin.ToString()) // 只有管理员 (UserType.Admin = 2)
            {
                // 管理员可以看到所有的
                query = _unitOfWork.Recordings.Find(r => true);
            }
            else
            {
                // 学生和老师只能看到自己的
                query = _unitOfWork.Recordings.Find(r => r.UserID == userId);
            }

            // 获取结果并转为DTO
            var recordings = await query
                .OrderByDescending(r => r.UploadDate)
                .Select(r => new RecordingDTO
                {
                    RecordingID = r.RecordingID,
                    UserID = r.UserID,
                    Title = r.Title,
                    FileName = r.FileName,
                    FileUrl = r.FileUrl,
                    UploadDate = r.UploadDate,
                    CaseContent = r.CaseContent,
                    CaseInformation = r.CaseInformation,
                    Username = r.User.Username
                })
                .ToListAsync();

            return Ok(recordings);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetRecording(int id)
        {
            var userId = int.Parse(User.FindFirst("sub")?.Value);
            var userType = User.FindFirst("userType")?.Value;

            var recording = await _unitOfWork.Recordings.Find(r => r.RecordingID == id)
                .Include(r => r.User)
                .FirstOrDefaultAsync();

            if (recording == null)
            {
                return NotFound(new { message = "录音不存在" });
            }

            // 检查权限（学生只能查看自己的录音）
            if (userType == UserType.Student.ToString() && recording.UserID != userId)
            {
                return Forbid();
            }

            var recordingDTO = new RecordingDTO
            {
                RecordingID = recording.RecordingID,
                UserID = recording.UserID,
                Title = recording.Title,
                FileName = recording.FileName,
                FileUrl = recording.FileUrl,
                UploadDate = recording.UploadDate,
                CaseContent = recording.CaseContent,
                CaseInformation = recording.CaseInformation,
                Username = recording.User.Username
            };

            return Ok(recordingDTO);
        }

        [HttpPost]
        public async Task<IActionResult> UploadRecording([FromForm] RecordingUploadRequest request)
        {
            var userId = int.Parse(User.FindFirst("sub")?.Value);

            if (request.Recording == null || request.Recording.Length == 0)
            {
                return BadRequest(new { message = "请选择要上传的录音文件" });
            }

            if (string.IsNullOrEmpty(request.Title))
            {
                return BadRequest(new { message = "请提供录音标题" });
            }

            if (string.IsNullOrEmpty(request.CaseContent))
            {
                return BadRequest(new { message = "请提供案件内容" });
            }

            if (string.IsNullOrEmpty(request.CaseInformation))
            {
                return BadRequest(new { message = "请提供案件信息" });
            }

            // 检查文件类型
            var allowedExtensions = new[] { ".mp3", ".wav", ".ogg", ".m4a", ".aac", ".webm" };
            var extension = Path.GetExtension(request.Recording.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                return BadRequest(new { message = "仅支持MP3、WAV、OGG、M4A、AAC和WEBM格式的音频文件" });
            }

            // 检查文件大小（限制为20MB）
            if (request.Recording.Length > 20 * 1024 * 1024)
            {
                return BadRequest(new { message = "文件大小不能超过20MB" });
            }

            // 保存录音文件
            var fileUrl = await _fileStorageService.SaveFileAsync(request.Recording, "recordings");

            var recording = new Recording
            {
                UserID = userId,
                Title = request.Title,
                FileName = request.Recording.FileName,
                FileUrl = fileUrl,
                UploadDate = DateTime.UtcNow,
                CaseContent = request.CaseContent,
                CaseInformation = request.CaseInformation
            };

            await _unitOfWork.Recordings.AddAsync(recording);
            await _unitOfWork.CompleteAsync();

            // 记录日志
            await _unitOfWork.Logs.AddAsync(new Log
            {
                UserID = userId,
                Action = "UploadRecording",
                Description = $"上传了录音: {request.Title}",
                LogTime = DateTime.UtcNow
            });
            await _unitOfWork.CompleteAsync();

            return Ok(new { message = "录音上传成功", fileUrl });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRecording(int id)
        {
            var userId = int.Parse(User.FindFirst("sub")?.Value);
            var userType = User.FindFirst("userType")?.Value;

            var recording = await _unitOfWork.Recordings.GetByIdAsync(id);
            if (recording == null)
            {
                return NotFound(new { message = "录音不存在" });
            }

            // 检查权限
            if (userType == UserType.Student.ToString() && recording.UserID != userId)
            {
                return Forbid();
            }

            // 删除文件
            if (!string.IsNullOrEmpty(recording.FileUrl) && _fileStorageService.FileExists(recording.FileUrl))
            {
                await _fileStorageService.DeleteFileAsync(recording.FileUrl);
            }

            _unitOfWork.Recordings.Remove(recording);
            await _unitOfWork.CompleteAsync();

            // 记录日志
            await _unitOfWork.Logs.AddAsync(new Log
            {
                UserID = userId,
                Action = "DeleteRecording",
                Description = $"删除了录音: {recording.Title}",
                LogTime = DateTime.UtcNow
            });
            await _unitOfWork.CompleteAsync();

            return Ok(new { message = "录音删除成功" });
        }
    }

    public class RecordingDTO
    {
        public int RecordingID { get; set; }
        public int UserID { get; set; }
        public string Title { get; set; }
        public string FileName { get; set; }
        public string FileUrl { get; set; }
        public DateTime UploadDate { get; set; }
        public string CaseContent { get; set; }
        public string CaseInformation { get; set; }
        public string Username { get; set; }
    }

    public class RecordingUploadRequest
    {
        public IFormFile Recording { get; set; }
        public string Title { get; set; }
        public string CaseContent { get; set; }
        public string CaseInformation { get; set; }
    }
}
