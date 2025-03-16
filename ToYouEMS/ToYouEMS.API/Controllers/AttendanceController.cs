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
    public class AttendanceController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileStorageService _fileStorageService;

        public AttendanceController(IUnitOfWork unitOfWork, IFileStorageService fileStorageService)
        {
            _unitOfWork = unitOfWork;
            _fileStorageService = fileStorageService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAttendances()
        {
            var userId = int.Parse(User.FindFirst("sub")?.Value);
            var userType = User.FindFirst("userType")?.Value;

            IQueryable<Attendance> query;   

            // 根据用户角色决定查询范围
            if (userType == UserType.Student.ToString())
            {
                // 学生只能看到自己的勤务表
                query = _unitOfWork.Attendances.Find(a => a.UserID == userId);
            }
            else
            {
                // 老师和管理员可以看到所有的勤务表
                query = _unitOfWork.Attendances.Find(a => true);
            }

            // 获取结果并转为DTO
            var attendances = await query
                .OrderByDescending(a => a.UploadDate)
                .Select(a => new AttendanceDTO
                {
                    AttendanceID = a.AttendanceID,
                    UserID = a.UserID,
                    Month = a.Month,
                    FileUrl = a.FileUrl,
                    UploadDate = a.UploadDate,
                    Status = a.Status
                })
                .ToListAsync();

            return Ok(attendances);
        }

        [HttpGet("template")]
        public IActionResult GetTemplate()
        {
            // 返回勤务表模板的URL
            var templateUrl = _fileStorageService.GetFileUrl("attendance_template.xlsx", "templates");

            // 检查模板是否存在
            if (!_fileStorageService.FileExists(templateUrl))
            {
                return NotFound(new { message = "勤务表模板不存在" });
            }

            return Ok(new { templateUrl });
        }

        [HttpPost]
        public async Task<IActionResult> UploadAttendance([FromForm] AttendanceUploadRequest request)
        {
            var userId = int.Parse(User.FindFirst("sub")?.Value);

            if (request.File == null || request.File.Length == 0)
            {
                return BadRequest(new { message = "请选择要上传的勤务表文件" });
            }

            if (string.IsNullOrEmpty(request.Month))
            {
                return BadRequest(new { message = "请提供勤务表月份" });
            }

            // 验证月份格式 (YYYY-MM)
            if (!System.Text.RegularExpressions.Regex.IsMatch(request.Month, @"^\d{4}-\d{2}$"))
            {
                return BadRequest(new { message = "月份格式必须为YYYY-MM" });
            }

            // 检查文件类型
            var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx" };
            var extension = Path.GetExtension(request.File.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                return BadRequest(new { message = "仅支持PDF、Word和Excel文件格式" });
            }

            // 检查是否已有同月份的勤务表
            var existingAttendance = await _unitOfWork.Attendances
                .SingleOrDefaultAsync(a => a.UserID == userId && a.Month == request.Month);

            if (existingAttendance != null)
            {
                // 删除旧文件
                if (!string.IsNullOrEmpty(existingAttendance.FileUrl) && _fileStorageService.FileExists(existingAttendance.FileUrl))
                {
                    await _fileStorageService.DeleteFileAsync(existingAttendance.FileUrl);
                }

                // 保存新文件
                var fileUrl = await _fileStorageService.SaveFileAsync(request.File, "attendances");

                // 更新记录
                existingAttendance.FileUrl = fileUrl;
                existingAttendance.UploadDate = DateTime.UtcNow;
                existingAttendance.Status = AttendanceStatus.Pending;

                _unitOfWork.Attendances.Update(existingAttendance);
            }
            else
            {
                // 保存文件
                var fileUrl = await _fileStorageService.SaveFileAsync(request.File, "attendances");

                // 创建新记录
                var attendance = new Attendance
                {
                    UserID = userId,
                    Month = request.Month,
                    FileUrl = fileUrl,
                    UploadDate = DateTime.UtcNow,
                    Status = AttendanceStatus.Pending
                };

                await _unitOfWork.Attendances.AddAsync(attendance);
            }

            await _unitOfWork.CompleteAsync();

            // 记录日志
            await _unitOfWork.Logs.AddAsync(new Log
            {
                UserID = userId,
                Action = "UploadAttendance",
                Description = $"上传了{request.Month}月份的勤务表",
                LogTime = DateTime.UtcNow
            });
            await _unitOfWork.CompleteAsync();

            return Ok(new { message = "勤务表上传成功" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAttendance(int id)
        {
            var userId = int.Parse(User.FindFirst("sub")?.Value);
            var userType = User.FindFirst("userType")?.Value;

            var attendance = await _unitOfWork.Attendances.GetByIdAsync(id);
            if (attendance == null)
            {
                return NotFound(new { message = "勤务表不存在" });
            }

            // 检查权限
            if (userType == UserType.Student.ToString() && attendance.UserID != userId)
            {
                return Forbid();
            }

            // 如果勤务表已审批通过，学生不能删除
            if (userType == UserType.Student.ToString() && attendance.Status == AttendanceStatus.Approved)
            {
                return BadRequest(new { message = "已审批通过的勤务表不能删除" });
            }

            // 删除文件
            if (!string.IsNullOrEmpty(attendance.FileUrl) && _fileStorageService.FileExists(attendance.FileUrl))
            {
                await _fileStorageService.DeleteFileAsync(attendance.FileUrl);
            }

            _unitOfWork.Attendances.Remove(attendance);
            await _unitOfWork.CompleteAsync();

            // 记录日志
            await _unitOfWork.Logs.AddAsync(new Log
            {
                UserID = userId,
                Action = "DeleteAttendance",
                Description = $"删除了{attendance.Month}月份的勤务表",
                LogTime = DateTime.UtcNow
            });
            await _unitOfWork.CompleteAsync();

            return Ok(new { message = "勤务表删除成功" });
        }

        [HttpPost("{id}/review")]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> ReviewAttendance(int id, AttendanceReviewRequest request)
        {
            var userId = int.Parse(User.FindFirst("sub")?.Value);

            var attendance = await _unitOfWork.Attendances.GetByIdAsync(id);
            if (attendance == null)
            {
                return NotFound(new { message = "勤务表不存在" });
            }

            attendance.Status = request.Status;
            _unitOfWork.Attendances.Update(attendance);
            await _unitOfWork.CompleteAsync();

            // 记录日志
            await _unitOfWork.Logs.AddAsync(new Log
            {
                UserID = userId,
                Action = "ReviewAttendance",
                Description = $"审核了{attendance.Month}月份的勤务表，状态: {request.Status}",
                LogTime = DateTime.UtcNow
            });
            await _unitOfWork.CompleteAsync();

            return Ok(new { message = "勤务表审核完成" });
        }
    }

    public class AttendanceDTO
    {
        public int AttendanceID { get; set; }
        public int UserID { get; set; }
        public string Month { get; set; }
        public string FileUrl { get; set; }
        public DateTime UploadDate { get; set; }
        public AttendanceStatus Status { get; set; }
    }

    public class AttendanceUploadRequest
    {
        public IFormFile File { get; set; }
        public string Month { get; set; } // 格式: YYYY-MM
    }

    public class AttendanceReviewRequest
    {
        public AttendanceStatus Status { get; set; }
    }
}
