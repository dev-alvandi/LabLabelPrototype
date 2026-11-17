
namespace LabLabelPrototype.Models
{
    public class IncubationSample
    {
        public string TrayNumber { get; set; } = "";

        public string ReadingDay { get; set; } = "";

        public string ReadingTime { get; set; } = "";

        public int ToleranceHours { get; set; }

        public string Incubator { get; set; } = "";

        public string PUID { get; set; } = "";
    }
}
