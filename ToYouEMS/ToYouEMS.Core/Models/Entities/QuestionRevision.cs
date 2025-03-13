namespace ToYouEMS.ToYouEMS.Core.Models.Entities
{
    public class QuestionRevision
    {
        public int RevisionID { get; set; }
        public int QuestionID { get; set; }
        public int UserID { get; set; }
        public string RevisionText { get; set; }
        public RevisionType Type { get; set; }
        public string Comments { get; set; }
        public DateTime CreatedAt { get; set; }

        // 导航属性
        public Question Question { get; set; }
        public User User { get; set; }
    }

    public enum RevisionType
    {
        Answer,         // 初始答案
        TeacherEdit,    // 老师修改
        TeacherComment  // 老师评论
    }
}
