namespace ToYouEMS.ToYouEMS.Core.Models.Entities
{
    public class Stat
    {
        public int StatID { get; set; }
        public string StatCategory { get; set; }
        public string StatName { get; set; }
        public int StatValue { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
