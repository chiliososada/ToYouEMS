namespace ToYouEMS.ToYouEMS.Core.Models.Entities
{
    public class Log
    {
        public int LogID { get; set; }
        public int? UserID { get; set; }
        public string Action { get; set; }
        public string Description { get; set; }
        public DateTime LogTime { get; set; }

        // 导航属性
        public User User { get; set; }
    }
}
