namespace ToYouEMS.ToYouEMS.Core.Models.Entities
{

    public class Recording
    {
        public int RecordingID { get; set; }
        public int UserID { get; set; }
        public string Title { get; set; }
        public string FileName { get; set; }
        public string FileUrl { get; set; }
        public DateTime UploadDate { get; set; }
        public string CaseContent { get; set; }
        public string CaseInformation { get; set; }

        // 导航属性
        public User User { get; set; }
    }
}
