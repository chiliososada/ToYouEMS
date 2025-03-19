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
        public async Task<IActionResult> GetAttendances([FromQuery] AttendanceQueryParams queryParams)
        {
            try
            {
                var userId = int.Parse(User.FindFirst("sub")?.Value);
                var userType = User.FindFirst("userType")?.Value;

                IQueryable<Attendance> query;

                // 根据用户角色决定查询范围
                if (userType == UserType.Admin.ToString()) // 只有管理员 (UserType.Admin = 2)
                {
                    // 管理员可以看到所有的勤务表
                    query = _unitOfWork.Attendances.Find(a => true);
                }
                else
                {
                    // 学生和老师只能看到自己的勤务表
                    query = _unitOfWork.Attendances.Find(a => a.UserID == userId);
                }

                // 应用筛选条件
                if (!string.IsNullOrEmpty(queryParams.Month))
                {
                    query = query.Where(a => a.Month.Contains(queryParams.Month));
                }

                if (queryParams.Status.HasValue)
                {
                    query = query.Where(a => a.Status == queryParams.Status.Value);
                }

                if (queryParams.UserId.HasValue)
                {
                    query = query.Where(a => a.UserID == queryParams.UserId.Value);
                }

                // 计算总数
                var totalCount = await query.CountAsync();
                var pageCount = (int)Math.Ceiling(totalCount / (double)queryParams.PageSize);

                // 应用排序
                query = ApplySorting(query, queryParams.SortBy, queryParams.SortDirection);

                // 应用分页
                var items = await query
                    .Skip((queryParams.PageNumber - 1) * queryParams.PageSize)
                    .Take(queryParams.PageSize)
                    .Include(a => a.User)
                    .Select(a => new AttendanceDetailDTO
                    {
                        AttendanceID = a.AttendanceID,
                        UserID = a.UserID,
                        Username = a.User.Username,
                        Month = a.Month,
                        FileUrl = a.FileUrl,
                        TransportationFileUrl = a.TransportationFileUrl, // 假设这个字段以后会添加
                        UploadDate = a.UploadDate,
                        Status = a.Status,
                        WorkHours = a.WorkHours, // 假设这个字段以后会添加
                        TransportationFee = a.TransportationFee ?? 0m, // 假设这个字段以后会添加
                        Comments = a.Comments,
                        ReviewerID = a.ReviewerID,
                        ReviewerName = a.ReviewerID.HasValue ? a.Reviewer.Username : null
                    })
                    .ToListAsync();

                return Ok(new PagedResult<AttendanceDetailDTO>
                {
                    Items = items,
                    TotalCount = totalCount,
                    PageCount = pageCount,
                    CurrentPage = queryParams.PageNumber,
                    PageSize = queryParams.PageSize
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"获取勤务表列表错误: {ex.Message}" });
            }
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

        [HttpGet("transportation-template")]
        public IActionResult GetTransportationTemplate()
        {
            // 返回交通费模板的URL
            var templateUrl = _fileStorageService.GetFileUrl("transportation_template.xlsx", "templates");

            // 检查模板是否存在
            if (!_fileStorageService.FileExists(templateUrl))
            {
                return NotFound(new { message = "交通费模板不存在" });
            }

            return Ok(new { templateUrl });
        }

        [HttpPost]
        public async Task<IActionResult> UploadAttendance([FromForm] AttendanceUploadRequest request)
        {
            try
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

                string fileUrl;
                Attendance attendance;

                if (existingAttendance != null)
                {
                    // 删除旧文件
                    if (!string.IsNullOrEmpty(existingAttendance.FileUrl) && _fileStorageService.FileExists(existingAttendance.FileUrl))
                    {
                        await _fileStorageService.DeleteFileAsync(existingAttendance.FileUrl);
                    }

                    // 保存新文件
                    fileUrl = await _fileStorageService.SaveFileAsync(request.File, "attendances");

                    // 更新记录
                    existingAttendance.FileUrl = fileUrl;
                    existingAttendance.UploadDate = DateTime.UtcNow;
                    existingAttendance.Status = AttendanceStatus.Pending;
                    existingAttendance.WorkHours = request.WorkHours; // 新字段
                    existingAttendance.TransportationFee = request.TransportationFee; // 新字段
                    existingAttendance.Comments = null; // 清空旧的评论
                    existingAttendance.ReviewerID = null; // 清空旧的审核员

                    _unitOfWork.Attendances.Update(existingAttendance);
                    attendance = existingAttendance;
                }
                else
                {
                    // 保存文件
                    fileUrl = await _fileStorageService.SaveFileAsync(request.File, "attendances");

                    // 创建新记录
                    attendance = new Attendance
                    {
                        UserID = userId,
                        Month = request.Month,
                        FileUrl = fileUrl,
                        UploadDate = DateTime.UtcNow,
                        Status = AttendanceStatus.Pending,
                        WorkHours = request.WorkHours, // 新字段
                        TransportationFee = request.TransportationFee // 新字段
                    };

                    await _unitOfWork.Attendances.AddAsync(attendance);
                }

                // 处理交通费文件上传
                if (request.TransportationFile != null && request.TransportationFile.Length > 0)
                {
                    var transportExtension = Path.GetExtension(request.TransportationFile.FileName).ToLowerInvariant();
                    if (!allowedExtensions.Contains(transportExtension))
                    {
                        return BadRequest(new { message = "交通费文件仅支持PDF、Word和Excel文件格式" });
                    }

                    // 删除旧的交通费文件（如果存在）
                    if (!string.IsNullOrEmpty(attendance.TransportationFileUrl) && _fileStorageService.FileExists(attendance.TransportationFileUrl))
                    {
                        await _fileStorageService.DeleteFileAsync(attendance.TransportationFileUrl);
                    }

                    // 保存新的交通费文件
                    var transportFileUrl = await _fileStorageService.SaveFileAsync(request.TransportationFile, "transportations");
                    attendance.TransportationFileUrl = transportFileUrl;
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

                return Ok(new { message = "勤务表上传成功", fileUrl });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"上传勤务表错误: {ex.Message}" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAttendance(int id)
        {
            try
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

                // 删除勤务表文件
                if (!string.IsNullOrEmpty(attendance.FileUrl) && _fileStorageService.FileExists(attendance.FileUrl))
                {
                    await _fileStorageService.DeleteFileAsync(attendance.FileUrl);
                }

                // 删除交通费文件（如果存在）
                if (!string.IsNullOrEmpty(attendance.TransportationFileUrl) && _fileStorageService.FileExists(attendance.TransportationFileUrl))
                {
                    await _fileStorageService.DeleteFileAsync(attendance.TransportationFileUrl);
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
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"删除勤务表错误: {ex.Message}" });
            }
        }

        [HttpPost("{id}/review")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ReviewAttendance(int id, AttendanceReviewRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst("sub")?.Value);

                var attendance = await _unitOfWork.Attendances.GetByIdAsync(id);
                if (attendance == null)
                {
                    return NotFound(new { message = "勤务表不存在" });
                }

                attendance.Status = request.Status;
                attendance.Comments = request.Comments;
                attendance.ReviewerID = userId;

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
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"审核勤务表错误: {ex.Message}" });
            }
        }

        #region 工具方法

        private IQueryable<Attendance> ApplySorting(IQueryable<Attendance> query, string sortBy, string sortDirection)
        {
            // 默认排序方向
            var isDescending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);

            // 根据字段排序
            switch (sortBy?.ToLower())
            {
                case "month":
                    return isDescending
                        ? query.OrderByDescending(a => a.Month)
                        : query.OrderBy(a => a.Month);
                case "uploaddate":
                    return isDescending
                        ? query.OrderByDescending(a => a.UploadDate)
                        : query.OrderBy(a => a.UploadDate);
                case "status":
                    return isDescending
                        ? query.OrderByDescending(a => a.Status)
                        : query.OrderBy(a => a.Status);
                case "workhours":
                    return isDescending
                        ? query.OrderByDescending(a => a.WorkHours)
                        : query.OrderBy(a => a.WorkHours);
                case "transportationfee":
                    return isDescending
                        ? query.OrderByDescending(a => a.TransportationFee)
                        : query.OrderBy(a => a.TransportationFee);
                case "userid":
                    return isDescending
                        ? query.OrderByDescending(a => a.UserID)
                        : query.OrderBy(a => a.UserID);
                default:
                    // 默认按上传日期排序
                    return isDescending
                        ? query.OrderByDescending(a => a.UploadDate)
                        : query.OrderBy(a => a.UploadDate);
            }
        }

        #endregion
    }

    #region DTO Models

    public class AttendanceDetailDTO
    {
        public int AttendanceID { get; set; }
        public int UserID { get; set; }
        public string Username { get; set; }
        public string Month { get; set; }
        public string FileUrl { get; set; }
        public string TransportationFileUrl { get; set; }
        public DateTime UploadDate { get; set; }
        public AttendanceStatus Status { get; set; }
        public int WorkHours { get; set; }
        public decimal TransportationFee { get; set; }
        public string Comments { get; set; }
        public int? ReviewerID { get; set; }
        public string ReviewerName { get; set; }
    }

    public class AttendanceUploadRequest
    {
        public IFormFile File { get; set; }
        public IFormFile? TransportationFile { get; set; }
        public string Month { get; set; } // 格式: YYYY-MM
        public int WorkHours { get; set; }
        public decimal TransportationFee { get; set; }
    }

    public class AttendanceReviewRequest
    {
        public AttendanceStatus Status { get; set; }
        public string Comments { get; set; }
    }

    public class AttendanceQueryParams
    {
        private int _pageNumber = 1;
        private int _pageSize = 10;

        public string? Month { get; set; }
        public AttendanceStatus? Status { get; set; }
        public int? UserId { get; set; }
        public string SortBy { get; set; } = "uploadDate";
        public string SortDirection { get; set; } = "desc";

        public int PageNumber
        {
            get => _pageNumber;
            set => _pageNumber = value < 1 ? 1 : value;
        }

        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value < 1 ? 10 : (value > 100 ? 100 : value);
        }
    }



    #endregion
}
