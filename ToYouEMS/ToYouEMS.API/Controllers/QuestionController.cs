using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ToYouEMS.ToYouEMS.Core.Interfaces;
using ToYouEMS.ToYouEMS.Core.Models.DTOs;
using ToYouEMS.ToYouEMS.Core.Models.Entities;

namespace ToYouEMS.ToYouEMS.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class QuestionController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public QuestionController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        #region 问题管理 API

        // 获取问题列表（带筛选条件）
        [HttpGet]
       
        public async Task<IActionResult> GetQuestions([FromQuery] QuestionQueryParams queryParams)
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirst("sub")?.Value);
                var userType = User.FindFirst("userType")?.Value;
                IQueryable<Question> query = _unitOfWork.Questions.Find(q => true);

                // 应用筛选条件
                if (queryParams.CaseID.HasValue)
                {
                    query = query.Where(q => q.CaseID == queryParams.CaseID.Value);
                }

                if (!string.IsNullOrEmpty(queryParams.Keyword))
                {
                    query = query.Where(q => q.QuestionText.Contains(queryParams.Keyword) ||
                                             q.Answer.Contains(queryParams.Keyword));
                }

                // 修改职位过滤的实现方式 - 使用EF兼容的方式
                if (!string.IsNullOrEmpty(queryParams.Position))
                {
                    var positions = queryParams.Position.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                               .Where(p => !string.IsNullOrWhiteSpace(p))
                                               .ToArray();

                    if (positions.Length > 0)
                    {
                        // 为每个职位创建一个独立的条件，然后将它们用OR组合
                        var positionsQuery = query;
                        bool isFirst = true;

                        foreach (var position in positions)
                        {
                            if (isFirst)
                            {
                                // 第一个条件直接设置
                                positionsQuery = query.Where(q => q.Case != null &&
                                                              q.Case.Position != null &&
                                                              q.Case.Position.Contains(position));
                                isFirst = false;
                            }
                            else
                            {
                                // 后续条件使用Union合并结果
                                positionsQuery = positionsQuery.Union(
                                    query.Where(q => q.Case != null &&
                                                 q.Case.Position != null &&
                                                 q.Case.Position.Contains(position))
                                );
                            }
                        }

                        // 使用处理后的查询
                        query = positionsQuery;
                    }
                }

                if (queryParams.Source.HasValue)
                {
                    query = query.Where(q => q.Source == queryParams.Source.Value);
                }

                if (queryParams.Status.HasValue)
                {
                    query = query.Where(q => q.Status == queryParams.Status.Value);
                }

                if (queryParams.UserID.HasValue)
                {
                    query = query.Where(q => q.UserID == queryParams.UserID.Value);
                }

                // 根据用户角色限制查询范围
                if (userType == UserType.Student.ToString())
                {
                    // 学生只能看自己的未审批问题和所有已审批问题
                    query = query.Where(q => q.UserID == currentUserId || q.Status == QuestionStatus.Approved);
                }
                else if (userType == UserType.Teacher.ToString())
                {
                    // 老师可以看所有问题
                }

                // 应用排序
                query = ApplySorting(query, queryParams.SortBy, queryParams.SortDescending);

                // 计算总数
                var totalCount = await query.CountAsync();
                var pageCount = (int)Math.Ceiling(totalCount / (double)queryParams.PageSize);

                // 应用分页并安全地投影数据
                var items = await query
                    .Skip((queryParams.PageNumber - 1) * queryParams.PageSize)
                    .Take(queryParams.PageSize)
                    .Include(q => q.User)
                    .Include(q => q.Case)
                    .Include(q => q.Revisions)
                    .Select(q => new QuestionListItemDTO
                    {
                        QuestionID = q.QuestionID,
                        CaseID = q.CaseID,
                        CaseName = q.Case != null ? q.Case.CaseName : string.Empty,
                        CompanyName = q.Case != null ? q.Case.CompanyName : string.Empty,
                        Position = q.Case != null ? q.Case.Position : string.Empty, // 添加Position字段
                        UserID = q.UserID,
                        Username = q.User != null ? q.User.Username : string.Empty,
                        QuestionText = q.QuestionText ?? string.Empty,
                        Source = q.Source,
                        Status = q.Status,
                        CreatedAt = q.CreatedAt,
                        RevisionCount = q.Revisions != null ? q.Revisions.Count : 0
                    })
                    .ToListAsync();

                return Ok(new PagedResult<QuestionListItemDTO>
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
                // 记录详细异常
                Console.WriteLine($"获取问题列表时发生错误: {ex.Message}");
                Console.WriteLine($"异常详情: {ex.StackTrace}");

                // 返回友好错误信息
                return StatusCode(500, new
                {
                    message = "获取问题列表时发生服务器错误",
                    error = ex.Message,
                    details = ex.InnerException?.Message
                });
            }
        }

        // 获取问题详情
        [HttpGet("{id}")]
        public async Task<IActionResult> GetQuestionDetail(int id)
        {
            var currentUserId = int.Parse(User.FindFirst("sub")?.Value);
            var userType = User.FindFirst("userType")?.Value;

            var question = await _unitOfWork.Questions.Find(q => q.QuestionID == id)
                .Include(q => q.User)
                .Include(q => q.Case)
                .Include(q => q.Revisions)
                .ThenInclude(r => r.User)
                .FirstOrDefaultAsync();

            if (question == null)
            {
                return NotFound(new { message = "问题不存在" });
            }

            // 检查权限
            if (userType == UserType.Student.ToString() &&
                question.UserID != currentUserId &&
                question.Status != QuestionStatus.Approved)
            {
                return Forbid();
            }

            var result = new QuestionDetailDTO
            {
                QuestionID = question.QuestionID,
                CaseID = question.CaseID,
                CaseName = question.Case.CaseName,
                CompanyName = question.Case.CompanyName,
                Position = question.Case.Position,
                UserID = question.UserID,
                Username = question.User.Username,
                QuestionText = question.QuestionText,
                Answer = question.Answer,
                Source = question.Source,
                Status = question.Status,
                CreatedAt = question.CreatedAt,
                Revisions = question.Revisions.Select(r => new QuestionRevisionDTO
                {
                    RevisionID = r.RevisionID,
                    UserID = r.UserID,
                    Username = r.User.Username,
                    RevisionText = r.RevisionText,
                    Type = r.Type,
                    Comments = r.Comments,
                    CreatedAt = r.CreatedAt
                }).OrderByDescending(r => r.CreatedAt).ToList()
            };

            return Ok(result);
        }

        // 创建问题
        [HttpPost]
        public async Task<IActionResult> CreateQuestion(QuestionCreateRequest request)
        {
            var userId = int.Parse(User.FindFirst("sub")?.Value);
            var userType = User.FindFirst("userType")?.Value;

            // 验证案例是否存在
            var caseExists = await _unitOfWork.Cases.GetByIdAsync(request.CaseID);
            if (caseExists == null)
            {
                return BadRequest(new { message = "指定的案例不存在" });
            }

            // 修改后：学生创建的问题都需要审核，无论来源
            QuestionStatus status = userType == UserType.Student.ToString()
      ? QuestionStatus.Pending
      : QuestionStatus.Approved;

            // 创建问题
            var question = new Question
            {
                CaseID = request.CaseID,
                UserID = userId,
                QuestionText = request.QuestionText,
                Answer = request.Answer,
                Source = request.Source,
                Status = status,
                CreatedAt = DateTime.Now
            };

            await _unitOfWork.Questions.AddAsync(question);
            await _unitOfWork.CompleteAsync();

            // 如果有答案，创建初始修订记录
            if (!string.IsNullOrEmpty(request.Answer))
            {
                var revision = new QuestionRevision
                {
                    QuestionID = question.QuestionID,
                    UserID = userId,
                    RevisionText = request.Answer,
                    Type = RevisionType.Answer,
                    Comments = "创建者答案",
                    CreatedAt = DateTime.Now
                };

                await _unitOfWork.QuestionRevisions.AddAsync(revision);
                await _unitOfWork.CompleteAsync();
            }

            // 记录日志
            await _unitOfWork.Logs.AddAsync(new Log
            {
                UserID = userId,
                Action = "CreateQuestion",
                Description = $"用户创建了问题: {request.QuestionText}",
                LogTime = DateTime.Now
            });
            await _unitOfWork.CompleteAsync();

            return CreatedAtAction(nameof(GetQuestionDetail), new { id = question.QuestionID },
                new { questionId = question.QuestionID, message = "问题创建成功" });
        }

        // 更新问题
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateQuestion(int id, QuestionUpdateRequest request)
        {
            var userId = int.Parse(User.FindFirst("sub")?.Value);
            var userType = User.FindFirst("userType")?.Value;

            var question = await _unitOfWork.Questions.GetByIdAsync(id);
            if (question == null)
            {
                return NotFound(new { message = "问题不存在" });
            }

            // 检查权限
            if (userType == UserType.Student.ToString() && question.UserID != userId)
            {
                return Forbid();
            }

            // 更新问题内容
            if (!string.IsNullOrEmpty(request.QuestionText))
            {
                question.QuestionText = request.QuestionText;
            }

            // 如果有新答案，更新答案并创建修订记录
            if (!string.IsNullOrEmpty(request.Answer) && request.Answer != question.Answer)
            {
                // 保存旧答案到修订记录
                if (!string.IsNullOrEmpty(question.Answer))
                {
                    var revision = new QuestionRevision
                    {
                        QuestionID = question.QuestionID,
                        UserID = userId,
                        RevisionText = request.Answer,
                        Type = userType == UserType.Teacher.ToString() ? RevisionType.TeacherEdit : RevisionType.Answer,
                        Comments = "更新回答",
                        CreatedAt = DateTime.Now
                    };

                    await _unitOfWork.QuestionRevisions.AddAsync(revision);
                }

                // 更新问题答案
                question.Answer = request.Answer;
            }

            _unitOfWork.Questions.Update(question);
            await _unitOfWork.CompleteAsync();

            // 记录日志
            await _unitOfWork.Logs.AddAsync(new Log
            {
                UserID = userId,
                Action = "UpdateQuestion",
                Description = $"用户更新了问题: {question.QuestionText}",
                LogTime = DateTime.Now
            });
            await _unitOfWork.CompleteAsync();

            return Ok(new { message = "问题更新成功" });
        }

        // 删除问题
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteQuestion(int id)
        {
            var userId = int.Parse(User.FindFirst("sub")?.Value);
            var userType = User.FindFirst("userType")?.Value;

            var question = await _unitOfWork.Questions.GetByIdAsync(id);
            if (question == null)
            {
                return NotFound(new { message = "问题不存在" });
            }

            // 检查权限
            if (userType == UserType.Student.ToString() && question.UserID != userId)
            {
                return Forbid();
            }

            // 删除相关的修订记录
            var revisions = await _unitOfWork.QuestionRevisions.Find(r => r.QuestionID == id).ToListAsync();
            _unitOfWork.QuestionRevisions.RemoveRange(revisions);

            // 删除问题
            _unitOfWork.Questions.Remove(question);
            await _unitOfWork.CompleteAsync();

            // 记录日志
            await _unitOfWork.Logs.AddAsync(new Log
            {
                UserID = userId,
                Action = "DeleteQuestion",
                Description = $"用户删除了问题: {question.QuestionText}",
                LogTime = DateTime.Now
            });
            await _unitOfWork.CompleteAsync();

            return Ok(new { message = "问题删除成功" });
        }

        #endregion

        #region 问题修订和评论 API

        // 添加修订/评论
        [HttpPost("{id}/revisions")]
        public async Task<IActionResult> AddRevision(int id, QuestionRevisionCreateRequest request)
        {
            var userId = int.Parse(User.FindFirst("sub")?.Value);
            var userType = User.FindFirst("userType")?.Value;

            var question = await _unitOfWork.Questions.GetByIdAsync(id);
            if (question == null)
            {
                return NotFound(new { message = "问题不存在" });
            }

            //// 只允许老师添加评论和修订
            //if (userType == UserType.Student.ToString() && request.Type != RevisionType.Answer)
            //{
            //    return Forbid();
            //}

            var revision = new QuestionRevision
            {
                QuestionID = id,
                UserID = userId,
                RevisionText = request.RevisionText,
                Type = request.Type,
                Comments = request.Comments,
                CreatedAt = DateTime.Now
            };

            await _unitOfWork.QuestionRevisions.AddAsync(revision);

            // 如果是教师编辑答案，则更新问题主表的答案字段
            if (userType == UserType.Teacher.ToString() && request.Type == RevisionType.TeacherEdit)
            {
                question.Answer = request.RevisionText;
                _unitOfWork.Questions.Update(question);
            }

            await _unitOfWork.CompleteAsync();

            // 记录日志
            await _unitOfWork.Logs.AddAsync(new Log
            {
                UserID = userId,
                Action = "AddRevision",
                Description = $"用户添加了修订/评论到问题: {question.QuestionText}",
                LogTime = DateTime.Now
            });
            await _unitOfWork.CompleteAsync();

            return Ok(new { revisionId = revision.RevisionID, message = "修订/评论添加成功" });
        }

        // 获取问题的修订/评论列表
        [HttpGet("{id}/revisions")]
        public async Task<IActionResult> GetRevisions(int id)
        {
            var currentUserId = int.Parse(User.FindFirst("sub")?.Value);
            var userType = User.FindFirst("userType")?.Value;

            var question = await _unitOfWork.Questions.GetByIdAsync(id);
            if (question == null)
            {
                return NotFound(new { message = "问题不存在" });
            }

            // 检查权限
            if (userType == UserType.Student.ToString() &&
                question.UserID != currentUserId &&
                question.Status != QuestionStatus.Approved)
            {
                return Forbid();
            }

            var revisions = await _unitOfWork.QuestionRevisions.Find(r => r.QuestionID == id)
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new QuestionRevisionDTO
                {
                    RevisionID = r.RevisionID,
                    UserID = r.UserID,
                    Username = r.User.Username,
                    RevisionText = r.RevisionText,
                    Type = r.Type,
                    Comments = r.Comments,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();

            return Ok(revisions);
        }

        #endregion

        #region 问题审批 API

        // 审批问题
        [HttpPost("{id}/approve")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApproveQuestion(int id, QuestionApprovalRequest request)
        {
            var userId = int.Parse(User.FindFirst("sub")?.Value);

            var question = await _unitOfWork.Questions.GetByIdAsync(id);
            if (question == null)
            {
                return NotFound(new { message = "问题不存在" });
            }

            question.Status = request.Status;
            _unitOfWork.Questions.Update(question);

            // 添加审批评论作为修订
            if (!string.IsNullOrEmpty(request.Comments))
            {
                var revision = new QuestionRevision
                {
                    QuestionID = id,
                    UserID = userId,
                    RevisionText = question.Answer ?? "",
                    Type = RevisionType.TeacherComment,
                    Comments = request.Comments,
                    CreatedAt = DateTime.Now
                };

                await _unitOfWork.QuestionRevisions.AddAsync(revision);
            }

            await _unitOfWork.CompleteAsync();

            // 记录日志
            await _unitOfWork.Logs.AddAsync(new Log
            {
                UserID = userId,
                Action = "ApproveQuestion",
                Description = $"管理员审批了问题: {question.QuestionText}, 状态: {request.Status}",
                LogTime = DateTime.Now
            });
            await _unitOfWork.CompleteAsync();

            return Ok(new { message = "问题审批成功" });
        }

        // 获取待审批问题列表
        [HttpGet("pending")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPendingQuestions([FromQuery] QuestionQueryParams queryParams)
        {
            // 强制设置筛选条件为待审批状态
            queryParams.Status = QuestionStatus.Pending;

            return await GetQuestions(queryParams);
        }

        #endregion

        #region 工具方法

        private IQueryable<Question> ApplySorting(IQueryable<Question> query, string sortBy, bool sortDescending)
        {
            switch (sortBy?.ToLower())
            {
                case "createdat":
                    return sortDescending
                        ? query.OrderByDescending(q => q.CreatedAt)
                        : query.OrderBy(q => q.CreatedAt);
                case "questiontext":
                    return sortDescending
                        ? query.OrderByDescending(q => q.QuestionText)
                        : query.OrderBy(q => q.QuestionText);
                case "username":
                    return sortDescending
                        ? query.OrderByDescending(q => q.User.Username)
                        : query.OrderBy(q => q.User.Username);
                case "casename":
                    return sortDescending
                        ? query.OrderByDescending(q => q.Case.CaseName)
                        : query.OrderBy(q => q.Case.CaseName);
                case "companyname":
                    return sortDescending
                        ? query.OrderByDescending(q => q.Case.CompanyName)
                        : query.OrderBy(q => q.Case.CompanyName);
                default:
                    return sortDescending
                        ? query.OrderByDescending(q => q.CreatedAt)
                        : query.OrderBy(q => q.CreatedAt);
            }
        }

        #endregion
    }
}
