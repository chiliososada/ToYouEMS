namespace ToYouEMS.ToYouEMS.Core.Models.Entities
{
    public class Attendance
    {
        public int AttendanceID { get; set; }
        public int UserID { get; set; }
        public string Month { get; set; }
        public string FileUrl { get; set; }
        public DateTime UploadDate { get; set; }
        public AttendanceStatus Status { get; set; }

        // 导航属性
        public User User { get; set; }
    }

    public enum AttendanceStatus
    {
        Pending,
        Approved,
        Rejected
    }
}
