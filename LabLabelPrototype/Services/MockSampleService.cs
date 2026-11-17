
using System.Collections.Generic;
using LabLabelPrototype.Models;

namespace LabLabelPrototype.Services
{
    public class MockSampleService
    {
        public List<IncubationSample> GetSamples()
        {
            return new List<IncubationSample>
            {
                new IncubationSample
                {
                    TrayNumber = "115155",
                    ReadingDay = "TORSDAG",
                    ReadingTime = "13:30",
                    ToleranceHours = 3,
                    Incubator = "Heating Room",
                    PUID = "QBC9"
                },

                new IncubationSample
                {
                    TrayNumber = "11338",
                    ReadingDay = "LÖRDAG",
                    ReadingTime = "10:00",
                    ToleranceHours = 2,
                    Incubator = "S9",
                    PUID = "R8JZ"
                }
            };
        }
    }
}
