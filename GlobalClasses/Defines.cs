using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using FS.SDK.Mathematics;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using VIBN_Tools.ContainerToFee;
using VIBN_Tools.GlobalClasses.FeeObjects;
using VIBN_Tools.ModelValidation;
using static VIBN_Tools.GlobalClasses.Interfaces;

namespace VIBN_Tools.GlobalClasses
{
    public class Defines
    {

        public static readonly string GrobGenerationInterfaceProviderGuidString = "a6222164-be37-49de-b760-9b1c97c320bb";
        public static readonly Guid GrobGenerationInterfaceProviderGuid = new Guid(GrobGenerationInterfaceProviderGuidString);

        public static readonly string CadWizardInterfaceGuidString = "e3f7a9c2-4b1d-4f6a-9d8e-2c7f1a6b3e9f";
        public static readonly Guid CadWizardInterfaceGuid = new Guid(CadWizardInterfaceGuidString);


    }





    //===================================================================================================================================
    // E N U M S   &   E N U M - H E L P E R S
    //===================================================================================================================================

    public enum TemplateType
    {
        Siemens,
        Beckhoff_Old,
        Beckhoff_New,
        Rockwell

    }

    public enum RobotType
    {
        ABB,
        Fanuc,
        Kuka,
    }

    public static class RobotTypeHelper
    {
        public static ObservableCollection<string> GetRobotTypes() => new(Enum.GetValues(typeof(RobotType))
                                                                                .Cast<RobotType>()
                                                                                .Select(t => t.DisplayName()));

        private static string GetDisplayName(RobotType type) => type switch
        {
            RobotType.ABB => "ABB",
            RobotType.Fanuc => "Fanuc",
            RobotType.Kuka => "KUKA",
            _ => type.ToString()
        };


        public static string DisplayName(this RobotType type) => GetDisplayName(type);
    }



    // Used for Zuli Converter
    public enum ApplicationType
    {
        FeScreenSim,
        ProcessSimulate
    }

    public enum LanguageType
    {
        TextLanguage1,
        TextLanguage2,
        TextLanguage3,
        TextLanguage4
    }


    public static class LanguageSelectionMap
    {
        public static readonly Dictionary<LanguageType, Func<IZuliToInterface, string>> Mapping =
            new Dictionary<LanguageType, Func<IZuliToInterface, string>>
            {
            { LanguageType.TextLanguage1, l => l.TextLanguage1 },
            { LanguageType.TextLanguage2, l => l.TextLanguage2 },
            { LanguageType.TextLanguage3, l => l.TextLanguage3 },
            { LanguageType.TextLanguage4, l => l.TextLanguage4 }
            };
    }



    public enum InterfaceConnectMode
    {
        [Description("Send & Receive")]
        SendReceive,

        [Description("Send only")]
        SendOnly,

        [Description("Receive only")]
        ReceiveOnly,
    }




    public enum Severity
    {
        Ok,
        Info,
        Warning,
        Error
    }




    public enum ViewElement
    {
        Textblock,
        Textbox,
        Combobox,
        Checkbox,
    }








    //===================================================================================================================================
    // A T T R I B U T E S
    //===================================================================================================================================

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class ZuliDisplayNameAttribute : Attribute
    {
        public string DisplayName { get; }

        public ZuliDisplayNameAttribute(string displayName)
        {
            DisplayName = displayName;
        }
    }


}
