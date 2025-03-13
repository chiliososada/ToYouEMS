namespace ToYouEMS.ToYouEMS.Core.Models.Entities
{
    public class Profile
    {
        public int ProfileID { get; set; }
        public int UserID { get; set; }
        public string FullName { get; set; }
        public string Gender { get; set; }
        public DateTime? BirthDate { get; set; }
        public string BirthPlace { get; set; }
        public string Address { get; set; }
        public string Introduction { get; set; }
        public string Hobbies { get; set; }
        public string AvatarUrl { get; set; }

        // 导航属性
        public User User { get; set; }
    }
}
