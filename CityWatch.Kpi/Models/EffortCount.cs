namespace CityWatch.Kpi.Models
{
    public class EffortCount
    {
        public int WeekNumber { get; set; }

        public int Flir { get; set; }

        public int Wand { get; set; }

        public bool IsEmpty
        { 
            get { return Wand == 0 && Flir == 0; }
        }
    }
}
