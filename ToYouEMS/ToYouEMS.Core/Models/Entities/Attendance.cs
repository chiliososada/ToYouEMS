namespace ToYouEMS.ToYouEMS.Core.Models.Entities
{
    public class Attendance
    {
        public int AttendanceID { get; set; }
        public int UserID { get; set; }
        public string? Month { get; set; }  // 允许为空
        public string? FileUrl { get; set; }  // 允许为空
        public DateTime UploadDate { get; set; }  // 允许为空
        public AttendanceStatus Status { get; set; }
        public int WorkHours { get; set; } = 0; // 设置默认值 // 改为decimal并允许为空
        public decimal? TransportationFee { get; set; } = 0m;  // 允许为空
        public string? TransportationFileUrl { get; set; }  // 允许为空
        public string? Comments { get; set; }  // 对应text类型
        public int? ReviewerID { get; set; }  // 允许为空

        // 导航属性
        public User User { get; set; } = null!;
        public User? Reviewer { get; set; }

    }


    public enum AttendanceStatus
    {
        Pending,
        Approved,
        Rejected
    }
}
