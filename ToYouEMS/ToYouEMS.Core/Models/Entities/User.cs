using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ToYouEMS.ToYouEMS.Core.Models.Entities
{
    public class User
    {
        public int UserID { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public UserType UserType { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        // 导航属性
        public Profile Profile { get; set; }
        public ICollection<Case> Cases { get; set; }
        public ICollection<Question> Questions { get; set; }
        public ICollection<Resume> Resumes { get; set; }
        public ICollection<Attendance> Attendances { get; set; }
        public ICollection<Log> Logs { get; set; }
    }

    public enum UserType
    {
        Student,
        Teacher,
        Admin
    }
}
