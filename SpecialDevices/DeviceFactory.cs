using MaNiAC_Tool.SpecialDevices.FieldDevices.Cognex;
using MaNiAC_Tool.SpecialDevices.FieldDevices.IPG;
using MaNiAC_Tool.SpecialDevices.FieldDevices.Keyence;
using MaNiAC_Tool.SpecialDevices.FieldDevices.Lenze;
using MaNiAC_Tool.SpecialDevices.FieldDevices.Promess;
using MaNiAC_Tool.SpecialDevices.RobotApplications.AtlasCopco;
using MaNiAC_Tool.SpecialDevices.RobotApplications.Safety;
using VIBN_Tools.GlobalClasses;
using VIBN_Tools.SpecialDevices.Devices.Grob;
using static VIBN_Tools.SpecialDevices.DeviceCatalog;

namespace VIBN_Tools.SpecialDevices
{
    public static class DeviceFactory
    {
        public static readonly Dictionary<(DeviceManufacturer, Enum), Func<string, SpecialDeviceAddresses, RobotType?, SpecialDevice>> DeviceFactoryMap
            = new()
            {
                // AtlasCopco
                {(DeviceManufacturer.AtlasCopco, AtlasCopcoDeviceTypes.Sys6000_Glueing_BMW), (p,a,r) => new AtlasCopcoSys6000_Glueing_BMW(p,a,r!.Value)},
                {(DeviceManufacturer.AtlasCopco, AtlasCopcoDeviceTypes.Sys6000_Glueing_VASS), (p,a,r) => new AtlasCopcoSys6000_Glueing_VASS(p,a,r!.Value)},

                // Cognex
                {(DeviceManufacturer.Cognex, CognexDeviceTypes.DatamanDMR), (p,a,r) => new CognexDatamanDMR(p,a)},

                // Grob
                {(DeviceManufacturer.Grob, GrobDeviceTypes.SimModeSiemens), (p,a,r) => new SimModeSiemens(p,a)},
                {(DeviceManufacturer.Grob, GrobDeviceTypes.SafePnPn), (p,a,r) => new SafePnPN(p,a)},

                // IPG
                {(DeviceManufacturer.Ipg, IpgDeviceTypes.LaserPicker), (p,a,r) => new IpgLaserPicker(p,a)},

                // Keyence
                {(DeviceManufacturer.Keyence, KeyenceDeviceTypes.SR2000), (p,a,r) => new KeyenceSR2000(p,a)},

                // Kuka
                {(DeviceManufacturer.Kuka, KukaDeviceTypes.RobotSafety), (p,a,r) => new KukaSafety(p,a)},

                // Lenze
                {(DeviceManufacturer.Lenze, LenzeDeviceTypes.I950), (p,a,r) => new LenzeI950(p,a)},
                {(DeviceManufacturer.Lenze, LenzeDeviceTypes.Motec8400), (p,a,r) => new Lenze8400Motec(p,a)},
                {(DeviceManufacturer.Lenze, LenzeDeviceTypes.Protec8400), (p,a,r) => new Lenze8400Protec(p,a)},

                // Promess
                {(DeviceManufacturer.Promess, PromessDeviceTypes.SpindleUp), (p,a,r) => new PromessSpindleUP(p,a)},

            };


        public static SpecialDevice Create(DeviceManufacturer manufacturer, Enum deviceType, string prefix, SpecialDeviceAddresses addresses, RobotType? robotType = null)
        {
            if (DeviceFactoryMap.TryGetValue((manufacturer, deviceType), out var ctor))
                return ctor(prefix, addresses, robotType);

            throw new NotSupportedException($"Unknown device combination: {manufacturer} / {deviceType}");
        }

    }










    public class DeviceMetadata
    {
        public bool RequiresRobotType { get; init; }


        public static readonly Dictionary<(DeviceManufacturer, Enum), DeviceMetadata> MetadataMap
        = new()
        {
            {(DeviceManufacturer.AtlasCopco, AtlasCopcoDeviceTypes.Sys6000_Glueing_BMW),
                new DeviceMetadata { RequiresRobotType = true }},

            {(DeviceManufacturer.AtlasCopco, AtlasCopcoDeviceTypes.Sys6000_Glueing_VASS),
                new DeviceMetadata { RequiresRobotType = true }},

            {(DeviceManufacturer.Promess, PromessDeviceTypes.SpindleUp),
                new DeviceMetadata { RequiresRobotType = false }},
        };

    }


}
