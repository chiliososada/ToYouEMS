namespace ToYouEMS.ToYouEMS.Core.Models.Entities
{
    public class Resume
    {
        public int ResumeID { get; set; }
        public int UserID { get; set; }
        public string? FileName { get; set; }  // 使用可空字符串
        public string? FileUrl { get; set; }   // 使用可空字符串
        public DateTime UploadDate { get; set; }
        public ResumeStatus Status { get; set; }
        public string? Comments { get; set; }  // 使用可空字符串
        public int? ReviewerID { get; set; }

        // 导航属性
        public User User { get; set; }
        public User Reviewer { get; set; }
    }

    public enum ResumeStatus
    {
        Pending,
        Approved,
        Rejected
    }
}
