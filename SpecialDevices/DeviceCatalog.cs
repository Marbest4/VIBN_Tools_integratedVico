using System.ComponentModel.DataAnnotations;

namespace VIBN_Tools.SpecialDevices
{
    public class DeviceCatalog
    {

        public enum DeviceManufacturer
        {
            AtlasCopco,
            Cognex,
            Grob,
            Ipg,
            Keyence,
            Kuka,
            Lenze,
            Promess,
        }



        public enum AtlasCopcoDeviceTypes
        {
            [Display(Name = "SYS6000 Glueing (BMW)")]
            Sys6000_Glueing_BMW,

            [Display(Name = "SYS6000 Glueing (VASS)")]
            Sys6000_Glueing_VASS,
        }

        public enum CognexDeviceTypes
        {
            [Display(Name = "Dataman / DMR")]
            DatamanDMR,
        }

        public enum GrobDeviceTypes
        {
            [Display(Name = "SimMode Siemens")]
            SimModeSiemens,

            [Display(Name = "Safe PN-PN")]
            SafePnPn,
        }

        public enum IpgDeviceTypes
        {
            [Display(Name = "Laser Picker")]
            LaserPicker,
        }

        public enum KeyenceDeviceTypes
        {
            [Display(Name = "SR2000")]
            SR2000,
        }

        public enum KukaDeviceTypes
        {
            [Display(Name = "Robot Safety")]
            RobotSafety,
        }

        public enum LenzeDeviceTypes
        {
            [Display(Name = "i950")]
            I950,

            [Display(Name = "8400 Motec")]
            Motec8400,

            [Display(Name = "8400 Protec")]
            Protec8400,
        }

        public enum PromessDeviceTypes
        {
            [Display(Name = "Spindle UP")]
            SpindleUp,
        }



        public static string GetDisplayName(Enum value)
        {
            if (value == null)
                return string.Empty;

            var field = value.GetType().GetField(value.ToString());
            if (field == null)
                return value.ToString();

            var attr = field.GetCustomAttributes(typeof(DisplayAttribute), false)
                            .FirstOrDefault() as DisplayAttribute;

            return attr?.Name ?? value.ToString();
        }



        public static readonly IReadOnlyDictionary<DeviceManufacturer, Type> DeviceTypeEnums = new Dictionary<DeviceManufacturer, Type>()
        {
            {DeviceManufacturer.AtlasCopco, typeof(AtlasCopcoDeviceTypes) },
            {DeviceManufacturer.Cognex, typeof(CognexDeviceTypes) },
            {DeviceManufacturer.Grob, typeof(GrobDeviceTypes) },
            {DeviceManufacturer.Ipg, typeof(IpgDeviceTypes) },
            {DeviceManufacturer.Keyence, typeof(KeyenceDeviceTypes) },
            {DeviceManufacturer.Kuka, typeof(KukaDeviceTypes) },
            {DeviceManufacturer.Lenze, typeof(LenzeDeviceTypes) },
            {DeviceManufacturer.Promess, typeof(PromessDeviceTypes) },
        };


    }

}
