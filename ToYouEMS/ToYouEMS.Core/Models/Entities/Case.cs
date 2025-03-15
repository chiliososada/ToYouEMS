namespace ToYouEMS.ToYouEMS.Core.Models.Entities
{
    public class Case
    {
        public int CaseID { get; set; }
        public string CaseName { get; set; } // 保持不变，因为这是必填字段
        public string? CompanyName { get; set; } // 添加可空标记
        public string? Position { get; set; }
        public DateTime? InterviewDate { get; set; } // 已经是可空的
        public string? Location { get; set; }
        public string? ContactPerson { get; set; }
        public string? ContactInfo { get; set; }
        public string Description { get; set; }
        public CaseStatus Status { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }

        // 导航属性
        public User Creator { get; set; }
        public ICollection<Question> Questions { get; set; }
    }

    public enum CaseStatus
    {
        Active,
        Completed,
        Cancelled
    }
}
