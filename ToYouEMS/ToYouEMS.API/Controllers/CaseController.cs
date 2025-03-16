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
        public class CaseController : ControllerBase
        {
            private readonly IUnitOfWork _unitOfWork;

            public CaseController(IUnitOfWork unitOfWork)
            {
                _unitOfWork = unitOfWork;
            }

            // 获取案例列表
            [HttpGet]
            public async Task<IActionResult> GetCases([FromQuery] CaseQueryParams queryParams)
            {
                var userId = int.Parse(User.FindFirst("sub")?.Value);
                var userType = User.FindFirst("userType")?.Value;

                IQueryable<Case> query = _unitOfWork.Cases.Find(c => true);

                // 应用筛选条件
                if (!string.IsNullOrEmpty(queryParams.Keyword))
                {
                    query = query.Where(c => c.CaseName.Contains(queryParams.Keyword) ||
                                             c.CompanyName.Contains(queryParams.Keyword) ||
                                             c.Position.Contains(queryParams.Keyword));
                }

                if (queryParams.Status.HasValue)
                {
                    query = query.Where(c => c.Status == queryParams.Status.Value);
                }

                if (userType == UserType.Student.ToString() && queryParams.OnlyMyCase)
                {
                    query = query.Where(c => c.CreatedBy == userId);
                }

                // 应用排序
                query = ApplySorting(query, queryParams.SortBy, queryParams.SortDescending);

                // 计算总数
                var totalCount = await query.CountAsync();
                var pageCount = (int)Math.Ceiling(totalCount / (double)queryParams.PageSize);

                // 应用分页
                var items = await query
                    .Include(c => c.Creator)
                    .Skip((queryParams.PageNumber - 1) * queryParams.PageSize)
                    .Take(queryParams.PageSize)
                    .Select(c => new CaseListItemDTO
                    {
                        CaseID = c.CaseID,
                        CaseName = c.CaseName,
                        CompanyName = c.CompanyName,
                        Position = c.Position,
                        InterviewDate = c.InterviewDate,
                        Location = c.Location,
                        Status = c.Status,
                        CreatorName = c.Creator.Username,
                        CreatedAt = c.CreatedAt,
                        QuestionCount = c.Questions.Count
                    })
                    .ToListAsync();

                return Ok(new PagedResult<CaseListItemDTO>
                {
                    Items = items,
                    TotalCount = totalCount,
                    PageCount = pageCount,
                    CurrentPage = queryParams.PageNumber,
                    PageSize = queryParams.PageSize
                });
            }

            // 获取案例详情
            [HttpGet("{id}")]
            public async Task<IActionResult> GetCaseDetail(int id)
            {
                var @case = await _unitOfWork.Cases.Find(c => c.CaseID == id)
                    .Include(c => c.Creator)
                    .Include(c => c.Questions.Where(q => q.Status == QuestionStatus.Approved))
                    .FirstOrDefaultAsync();

                if (@case == null)
                {
                    return NotFound(new { message = "案例不存在" });
                }

                var result = new CaseDetailDTO
                {
                    CaseID = @case.CaseID,
                    CaseName = @case.CaseName,
                    CompanyName = @case.CompanyName,
                    Position = @case.Position,
                    InterviewDate = @case.InterviewDate,
                    Location = @case.Location,
                    ContactPerson = @case.ContactPerson,
                    ContactInfo = @case.ContactInfo,
                    Description = @case.Description,
                    Status = @case.Status,
                    CreatorID = @case.CreatedBy,
                    CreatorName = @case.Creator.Username,
                    CreatedAt = @case.CreatedAt,
                    Questions = @case.Questions.Select(q => new QuestionBriefDTO
                    {
                        QuestionID = q.QuestionID,
                        QuestionText = q.QuestionText,
                        Source = q.Source
                    }).ToList()
                };

                return Ok(result);
            }

            // 创建案例
            [HttpPost]
            public async Task<IActionResult> CreateCase(CaseCreateRequest request)
            {
                var userId = int.Parse(User.FindFirst("sub")?.Value);

                var @case = new Case
                {
                    CaseName = request.CaseName,
                    CompanyName = request.CompanyName,
                    Position = request.Position,
                    InterviewDate = request.InterviewDate,
                    Location = request.Location,
                    ContactPerson = request.ContactPerson,
                    ContactInfo = request.ContactInfo,
                    Description = request.Description,
                    Status = CaseStatus.Active,
                    CreatedBy = userId,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Cases.AddAsync(@case);
                await _unitOfWork.CompleteAsync();

                // 记录日志
                await _unitOfWork.Logs.AddAsync(new Log
                {
                    UserID = userId,
                    Action = "CreateCase",
                    Description = $"用户创建了案例: {@case.CaseName}",
                    LogTime = DateTime.UtcNow
                });
                await _unitOfWork.CompleteAsync();

                return CreatedAtAction(nameof(GetCaseDetail), new { id = @case.CaseID },
                    new { caseId = @case.CaseID, message = "案例创建成功" });
            }

            // 更新案例
            [HttpPut("{id}")]
            public async Task<IActionResult> UpdateCase(int id, CaseUpdateRequest request)
            {
                var userId = int.Parse(User.FindFirst("sub")?.Value);
                var userType = User.FindFirst("userType")?.Value;

                var @case = await _unitOfWork.Cases.GetByIdAsync(id);
                if (@case == null)
                {
                    return NotFound(new { message = "案例不存在" });
                }

                // 检查权限
                if (userType == UserType.Student.ToString() && @case.CreatedBy != userId)
                {
                    return Forbid();
                }

                @case.CaseName = request.CaseName ?? @case.CaseName;
                @case.CompanyName = request.CompanyName ?? @case.CompanyName;
                @case.Position = request.Position ?? @case.Position;
                @case.InterviewDate = request.InterviewDate ?? @case.InterviewDate;
                @case.Location = request.Location ?? @case.Location;
                @case.ContactPerson = request.ContactPerson ?? @case.ContactPerson;
                @case.ContactInfo = request.ContactInfo ?? @case.ContactInfo;
                @case.Description = request.Description ?? @case.Description;

                if (request.Status.HasValue)
                {
                    @case.Status = request.Status.Value;
                }

                _unitOfWork.Cases.Update(@case);
                await _unitOfWork.CompleteAsync();

                // 记录日志
                await _unitOfWork.Logs.AddAsync(new Log
                {
                    UserID = userId,
                    Action = "UpdateCase",
                    Description = $"用户更新了案例: {@case.CaseName}",
                    LogTime = DateTime.UtcNow
                });
                await _unitOfWork.CompleteAsync();

                return Ok(new { message = "案例更新成功" });
            }
        [HttpGet("positions")] // 这样将映射到 /api/Case/positions
        public async Task<IActionResult> GetPositions()
        {
            try
            {
                // 加入调试信息
               

                // 查询所有非空的职位
                var positions = await _unitOfWork.Cases.Find(c => c.Position != null && c.Position != string.Empty)
                    .Select(c => c.Position)
                    .Distinct()
                    .ToListAsync();

               
                return Ok(positions);
            }
            catch (Exception ex)
            {
               
                return StatusCode(500, new { message = $"获取职位列表出错: {ex.Message}" });
            }
        }
        // 删除案例
        [HttpDelete("{id}")]
            public async Task<IActionResult> DeleteCase(int id)
            {
                var userId = int.Parse(User.FindFirst("sub")?.Value);
                var userType = User.FindFirst("userType")?.Value;

                var @case = await _unitOfWork.Cases.GetByIdAsync(id);
                if (@case == null)
                {
                    return NotFound(new { message = "案例不存在" });
                }

                // 检查权限
                if (userType == UserType.Student.ToString() && @case.CreatedBy != userId)
                {
                    return Forbid();
                }

                // 检查是否有关联的问题
                var hasQuestions = await _unitOfWork.Questions.Find(q => q.CaseID == id).AnyAsync();
                if (hasQuestions)
                {
                    return BadRequest(new { message = "案例存在关联的问题，无法删除" });
                }

                _unitOfWork.Cases.Remove(@case);
                await _unitOfWork.CompleteAsync();

                // 记录日志
                await _unitOfWork.Logs.AddAsync(new Log
                {
                    UserID = userId,
                    Action = "DeleteCase",
                    Description = $"用户删除了案例: {@case.CaseName}",
                    LogTime = DateTime.UtcNow
                });
                await _unitOfWork.CompleteAsync();

                return Ok(new { message = "案例删除成功" });
            }

            #region 工具方法

            private IQueryable<Case> ApplySorting(IQueryable<Case> query, string sortBy, bool sortDescending)
            {
                switch (sortBy?.ToLower())
                {
                    case "createdat":
                        return sortDescending
                            ? query.OrderByDescending(c => c.CreatedAt)
                            : query.OrderBy(c => c.CreatedAt);
                    case "casename":
                        return sortDescending
                            ? query.OrderByDescending(c => c.CaseName)
                            : query.OrderBy(c => c.CaseName);
                    case "companyname":
                        return sortDescending
                            ? query.OrderByDescending(c => c.CompanyName)
                            : query.OrderBy(c => c.CompanyName);
                    case "interviewdate":
                        return sortDescending
                            ? query.OrderByDescending(c => c.InterviewDate)
                            : query.OrderBy(c => c.InterviewDate);
                    default:
                        return sortDescending
                            ? query.OrderByDescending(c => c.CreatedAt)
                            : query.OrderBy(c => c.CreatedAt);
                }
            }

            #endregion
        }

        #region DTO 类

        public class CaseQueryParams
        {
            public string Keyword { get; set; }
            public CaseStatus? Status { get; set; }
            public bool OnlyMyCase { get; set; } = false;
            public string SortBy { get; set; } = "CreatedAt";
            public bool SortDescending { get; set; } = true;
            public int PageNumber { get; set; } = 1;
            public int PageSize { get; set; } = 10;
        }

        public class CaseListItemDTO
        {
            public int CaseID { get; set; }
            public string CaseName { get; set; }
            public string CompanyName { get; set; }
            public string Position { get; set; }
            public DateTime? InterviewDate { get; set; }
            public string Location { get; set; }
            public CaseStatus Status { get; set; }
            public string CreatorName { get; set; }
            public DateTime CreatedAt { get; set; }
            public int QuestionCount { get; set; }
        }

        public class CaseDetailDTO
        {
            public int CaseID { get; set; }
            public string CaseName { get; set; }
            public string CompanyName { get; set; }
            public string Position { get; set; }
            public DateTime? InterviewDate { get; set; }
            public string Location { get; set; }
            public string ContactPerson { get; set; }
            public string ContactInfo { get; set; }
            public string Description { get; set; }
            public CaseStatus Status { get; set; }
            public int CreatorID { get; set; }
            public string CreatorName { get; set; }
            public DateTime CreatedAt { get; set; }
            public List<QuestionBriefDTO> Questions { get; set; } = new List<QuestionBriefDTO>();
        }

        public class QuestionBriefDTO
        {
            public int QuestionID { get; set; }
            public string QuestionText { get; set; }
            public QuestionSource Source { get; set; }
        }

        public class CaseCreateRequest
        {
            public string CaseName { get; set; }
            public string? CompanyName { get; set; }
            public string? Position { get; set; }
            public DateTime? InterviewDate { get; set; }
            public string? Location { get; set; }
            public string? ContactPerson { get; set; }
            public string? ContactInfo { get; set; }
            public string Description { get; set; }
        }

        public class CaseUpdateRequest
        {
            public string CaseName { get; set; }
            public string CompanyName { get; set; }
            public string Position { get; set; }
            public DateTime? InterviewDate { get; set; }
            public string Location { get; set; }
            public string ContactPerson { get; set; }
            public string ContactInfo { get; set; }
            public string Description { get; set; }
            public CaseStatus? Status { get; set; }
        }

        public class PagedResult<T>
        {
            public List<T> Items { get; set; }
            public int TotalCount { get; set; }
            public int PageCount { get; set; }
            public int CurrentPage { get; set; }
            public int PageSize { get; set; }
        }

        #endregion
    
}
