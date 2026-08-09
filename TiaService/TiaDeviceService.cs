using VIBN_Tools.SpecialDevices;

namespace VIBN_Tools.TiaService
{
    public class TiaDeviceService
    {

        public static async Task<List<SpecialDevice>> GetTiaHardwareDevices()
        {
            return new List<SpecialDevice>();
        }





        // Hier irgendwo Dictionary zum Mapping definieren. oder device Factory implementieren







    }




    public record TiaDeviceDefinition(string Name, string Gsd);
}
