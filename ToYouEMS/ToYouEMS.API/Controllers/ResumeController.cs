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
    public class ResumeController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileStorageService _fileStorageService;

        public ResumeController(IUnitOfWork unitOfWork, IFileStorageService fileStorageService)
        {
            _unitOfWork = unitOfWork;
            _fileStorageService = fileStorageService;
        }

        [HttpGet]
        public async Task<IActionResult> GetResumes()
        {
            var userId = int.Parse(User.FindFirst("sub")?.Value);
            var userType = User.FindFirst("userType")?.Value;

            IQueryable<Resume> query;

            // 根据用户角色决定查询范围
            if (userType == UserType.Student.ToString())
            {
                // 学生只能看到自己的简历
                query = _unitOfWork.Resumes.Find(r => r.UserID == userId);
            }
            else
            {
                // 老师和管理员可以看到所有的简历
                query = _unitOfWork.Resumes.Find(r => true);
            }

            // 获取结果并转为DTO
            var resumes = await query
                .OrderByDescending(r => r.UploadDate)
                .Select(r => new ResumeDTO
                {
                    ResumeID = r.ResumeID,
                    UserID = r.UserID,
                    FileName = r.FileName,
                    FileUrl = r.FileUrl,
                    UploadDate = r.UploadDate,
                    Status = r.Status,
                    Comments = r.Comments,
                    ReviewerID = r.ReviewerID
                })
                .ToListAsync();

            return Ok(resumes);
        }

        [HttpGet("template")]
        public IActionResult GetTemplate()
        {
            // 返回简历模板的URL
           


            string fileUrl = _fileStorageService.GetFileUrl("resume_template.xlsx", "templates");
            string filePath = _fileStorageService.GetFilePath(fileUrl);

            // 检查模板是否存在
            if (!_fileStorageService.FileExists(fileUrl))
            {
                return NotFound(new { message = "简历模板不存在" });
            }
            string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

            // 获取原始文件名 (如果有存储)
            string originalFileName = "resume_template.xlsx";

            // 读取文件内容并返回
            var fileBytes = System.IO.File.ReadAllBytes(filePath);
            return File(fileBytes, contentType, originalFileName);
            //return Ok(new { templateUrl });
        }

        [HttpPost]
        public async Task<IActionResult> UploadResume([FromForm] ResumeUploadRequest request)
        {
            var userId = int.Parse(User.FindFirst("sub")?.Value);

            if (request.Resume == null || request.Resume.Length == 0)
            {
                return BadRequest(new { message = "请选择要上传的简历文件" });
            }

            // 检查文件类型
            var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx" };
            var extension = Path.GetExtension(request.Resume.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                return BadRequest(new { message = "仅支持PDF、Word和Excel文件格式" });
            }

            // 保存简历文件
            var fileUrl = await _fileStorageService.SaveFileAsync(request.Resume, "resumes");

            var resume = new Resume
            {
                UserID = userId,
                FileName = request.Resume.FileName,
                FileUrl = fileUrl,
                UploadDate = DateTime.Now,
                Status = ResumeStatus.Pending
            };

            await _unitOfWork.Resumes.AddAsync(resume);
            await _unitOfWork.CompleteAsync();

            // 记录日志
            await _unitOfWork.Logs.AddAsync(new Log
            {
                UserID = userId,
                Action = "UploadResume",
                Description = $"上传了简历: {request.Resume.FileName}",
                LogTime = DateTime.Now
            });
            await _unitOfWork.CompleteAsync();

            return Ok(new { message = "简历上传成功", fileUrl });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteResume(int id)
        {
            var userId = int.Parse(User.FindFirst("sub")?.Value);
            var userType = User.FindFirst("userType")?.Value;

            var resume = await _unitOfWork.Resumes.GetByIdAsync(id);
            if (resume == null)
            {
                return NotFound(new { message = "简历不存在" });
            }

            // 检查权限
            if (userType == UserType.Student.ToString() && resume.UserID != userId)
            {
                return Forbid();
            }

            // 删除文件
            if (!string.IsNullOrEmpty(resume.FileUrl) && _fileStorageService.FileExists(resume.FileUrl))
            {
                await _fileStorageService.DeleteFileAsync(resume.FileUrl);
            }

            _unitOfWork.Resumes.Remove(resume);
            await _unitOfWork.CompleteAsync();

            // 记录日志
            await _unitOfWork.Logs.AddAsync(new Log
            {
                UserID = userId,
                Action = "DeleteResume",
                Description = $"删除了简历: {resume.FileName}",
                LogTime = DateTime.Now
            });
            await _unitOfWork.CompleteAsync();

            return Ok(new { message = "简历删除成功" });
        }

        [HttpPost("{id}/review")]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> ReviewResume(int id, ResumeReviewRequest request)
        {
            var userId = int.Parse(User.FindFirst("sub")?.Value);

            var resume = await _unitOfWork.Resumes.GetByIdAsync(id);
            if (resume == null)
            {
                return NotFound(new { message = "简历不存在" });
            }

            resume.Status = request.Status;
            resume.Comments = request.Comments;
            resume.ReviewerID = userId;

            _unitOfWork.Resumes.Update(resume);
            await _unitOfWork.CompleteAsync();

            // 记录日志
            await _unitOfWork.Logs.AddAsync(new Log
            {
                UserID = userId,
                Action = "ReviewResume",
                Description = $"审核了简历: {resume.FileName}, 状态: {request.Status}",
                LogTime = DateTime.Now
            });
            await _unitOfWork.CompleteAsync();

            return Ok(new { message = "简历审核完成" });
        }
    }

    public class ResumeDTO
    {
        public int ResumeID { get; set; }
        public int UserID { get; set; }
        public string FileName { get; set; }
        public string FileUrl { get; set; }
        public DateTime UploadDate { get; set; }
        public ResumeStatus Status { get; set; }
        public string Comments { get; set; }
        public int? ReviewerID { get; set; }
    }

    public class ResumeUploadRequest
    {
        public Microsoft.AspNetCore.Http.IFormFile Resume { get; set; }
    }

    public class ResumeReviewRequest
    {
        public ResumeStatus Status { get; set; }
        public string Comments { get; set; }
    }
}
