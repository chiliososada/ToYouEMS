namespace ToYouEMS.ToYouEMS.Core.Models.Entities
{
    public class Question
    {
        public int QuestionID { get; set; }
        public int CaseID { get; set; }
        public int UserID { get; set; }
        public string QuestionText { get; set; }
        public string Answer { get; set; }
        public QuestionSource Source { get; set; }
        public QuestionStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }


        // 导航属性
        public Case Case { get; set; }
        public User User { get; set; }
        public ICollection<QuestionRevision> Revisions { get; set; } = new List<QuestionRevision>();
    }

    public enum QuestionSource
    {
        Personal,
        Company
    }

    public enum QuestionStatus
    {
        Pending,
        Approved,
        Rejected
    }
}
