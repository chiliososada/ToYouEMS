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


            // 检查文件大小（限制为100MB）
            if (request.Recording.Length > 100 * 1024 * 1024)
            {
                return BadRequest(new { message = "文件大小不能超过100MB" });
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


        // 文件路径: ToYouEMS/ToYouEMS.API/Controllers/RecordingController.cs
        // 分段上传技术:

        [HttpPost("init-chunked-upload")]
        public async Task<IActionResult> InitializeChunkedUpload([FromBody] InitChunkedUploadRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.FileName))
                {
                    return BadRequest(new { message = "文件名不能为空" });
                }

                var tempFilePath = await _fileStorageService.InitializeChunkedUploadAsync(request.FileName, "recordings");
                return Ok(new { tempFilePath, message = "分片上传初始化成功" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"初始化分片上传失败: {ex.Message}" });
            }
        }

        [HttpPost("upload-chunk")]
        public async Task<IActionResult> UploadChunk([FromForm] UploadChunkRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.TempFilePath))
                {
                    return BadRequest(new { message = "临时文件路径不能为空" });
                }

                if (request.Chunk == null || request.Chunk.Length == 0)
                {
                    return BadRequest(new { message = "分片数据不能为空" });
                }

                await _fileStorageService.AppendChunkAsync(
                    request.TempFilePath,
                    request.Chunk.OpenReadStream(),
                    request.ChunkIndex
                );

                return Ok(new
                {
                    message = $"分片 {request.ChunkIndex + 1}/{request.TotalChunks} 上传成功",
                    progress = (int)Math.Round((request.ChunkIndex + 1) * 100.0 / request.TotalChunks)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"上传分片失败: {ex.Message}" });
            }
        }

        [HttpPost("complete-chunked-upload")]
        public async Task<IActionResult> CompleteChunkedUpload([FromBody] CompleteChunkedUploadRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst("sub")?.Value);

                if (string.IsNullOrEmpty(request.TempFilePath) ||
                    string.IsNullOrEmpty(request.FileName) ||
                    string.IsNullOrEmpty(request.Title) ||
                    string.IsNullOrEmpty(request.CaseContent) ||
                    string.IsNullOrEmpty(request.CaseInformation))
                {
                    return BadRequest(new { message = "请求参数不完整" });
                }

                // 完成分片上传，获取最终文件URL
                var fileUrl = await _fileStorageService.CompleteChunkedUploadAsync(
                    request.TempFilePath,
                    request.FileName,
                    "recordings"
                );

                // 创建录音记录
                var recording = new Recording
                {
                    UserID = userId,
                    Title = request.Title,
                    FileName = request.FileName,
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
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"完成分片上传失败: {ex.Message}" });
            }
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
    //分段上传技术
    // 添加对应的请求模型
    public class InitChunkedUploadRequest
    {
        public string FileName { get; set; }
    }

    public class UploadChunkRequest
    {
        public string TempFilePath { get; set; }
        public IFormFile Chunk { get; set; }
        public int ChunkIndex { get; set; }
        public int TotalChunks { get; set; }
    }

    public class CompleteChunkedUploadRequest
    {
        public string TempFilePath { get; set; }
        public string FileName { get; set; }
        public string Title { get; set; }
        public string CaseContent { get; set; }
        public string CaseInformation { get; set; }
    }
}
