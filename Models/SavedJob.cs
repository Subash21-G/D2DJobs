namespace JobForFresher.Models
{
    public class SavedJob
    {
        public int Id { get; set; }

        public int JobId { get; set; }

        public DateTime SavedDate { get; set; } = DateTime.Now;
    }
}