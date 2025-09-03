using System.Collections.Generic;


namespace CATERINGMANAGEMENT.Models
{
    public class GroupSchedule
    {
        public Reservation? Reservation {  get; set; }
        public List<Worker>? Workers { get; set; }

        public string WorkerNames => Workers != null
        ? string.Join(", ", Workers.Select(w => w.Name))
        : string.Empty;
    }
}
