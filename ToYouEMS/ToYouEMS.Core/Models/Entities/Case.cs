namespace ToYouEMS.ToYouEMS.Core.Models.Entities
{
    public class Case
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
