using System.ComponentModel.DataAnnotations;
using ToYouEMS.ToYouEMS.Core.Models.Entities;

namespace ToYouEMS.ToYouEMS.Core.Models.DTOs
{
    public class QuestionCreateRequest
    {
        [Required]
        public int CaseID { get; set; }

        [Required]
        public string QuestionText { get; set; }

        public string Answer { get; set; }

        public QuestionSource Source { get; set; } = QuestionSource.Personal;
    }

    // 问题修改请求DTO
    public class QuestionUpdateRequest
    {
        public string QuestionText { get; set; }
        public string Answer { get; set; }
    }

    // 问题评论/修改请求DTO
    public class QuestionRevisionCreateRequest
    {
        [Required]
        public string RevisionText { get; set; }

        public RevisionType Type { get; set; } = RevisionType.TeacherComment;

        public string Comments { get; set; }
    }

    // 问题审批请求DTO
    public class QuestionApprovalRequest
    {
        [Required]
        public QuestionStatus Status { get; set; }

        public string Comments { get; set; }
    }

    // 问题查询参数DTO
    public class QuestionQueryParams
    {
        public int? CaseID { get; set; }
        public string? Keyword { get; set; }
        public QuestionSource? Source { get; set; }
        public QuestionStatus? Status { get; set; }
        public int? UserID { get; set; }
        public string? Position { get; set; } // 添加职位属性
        public string SortBy { get; set; } = "CreatedAt";
        public bool SortDescending { get; set; } = true;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    // 问题详情响应DTO
    public class QuestionDetailDTO
    {
        public int QuestionID { get; set; }
        public int CaseID { get; set; }
        public string CaseName { get; set; }
        public string CompanyName { get; set; }
        public string Position { get; set; }
        public int UserID { get; set; }
        public string Username { get; set; }
        public string QuestionText { get; set; }
        public string Answer { get; set; }
        public QuestionSource Source { get; set; }
        public QuestionStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<QuestionRevisionDTO> Revisions { get; set; } = new List<QuestionRevisionDTO>();
    }

    // 问题简要响应DTO（用于列表）
    public class QuestionListItemDTO
    {
        public string Position { get; set; }
        public int QuestionID { get; set; }
        public int CaseID { get; set; }
        public string CaseName { get; set; }
        public string CompanyName { get; set; }
        public int UserID { get; set; }
        public string Username { get; set; }
        public string QuestionText { get; set; }
        public QuestionSource Source { get; set; }
        public QuestionStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public int RevisionCount { get; set; }
    }

    // 问题修订响应DTO
    public class QuestionRevisionDTO
    {
        public int RevisionID { get; set; }
        public int UserID { get; set; }
        public string Username { get; set; }
        public string RevisionText { get; set; }
        public RevisionType Type { get; set; }
        public string Comments { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // 分页响应DTO
    public class PagedResult<T>
    {
        public List<T> Items { get; set; }
        public int TotalCount { get; set; }
        public int PageCount { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
    }
}
