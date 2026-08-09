using FS.SDK.Mathematics;
using System.IO;
using VIBN_Tools.GlobalClasses;
using VIBN_Tools.GlobalClasses.FeeObjects;
using static VIBN_Tools.GlobalClasses.FeeObjects.FeeCabinetElement;
using static VIBN_Tools.GlobalClasses.FeeObjects.FeeLogic;
using static VIBN_Tools.GlobalClasses.Services;
using MvvmBase = VIBN_Tools.GlobalClasses.MvvmBase;

namespace VIBN_Tools.Settings
{
    public class ProjectSettings : NotifyBase
    {       


        private TemplateType _selectedTemplate;
        public TemplateType SelectedTemplate
        {
            get => _selectedTemplate;
            set
            {
                _selectedTemplate = value;
                OnPropertyChanged();
            }
        }






        public static async Task ImportFeeLogicAndCabinetFilesAsync()
        {
            // Base Path to content
            string basePathContent = Path.Combine(AppContext.BaseDirectory, @"Content");

            // Import Standard Logics
            await ApiInstance.Logic.SendLogicDefinitionAsync(basePathContent + LogicsStandard.Grob_AxisBeckhoff.Path);
            await ApiInstance.Logic.SendLogicDefinitionAsync(basePathContent + LogicsStandard.Grob_AxisSiemens.Path);
            await ApiInstance.Logic.SendLogicDefinitionAsync(basePathContent + LogicsStandard.Grob_BeltControl.Path);
            await ApiInstance.Logic.SendLogicDefinitionAsync(basePathContent + LogicsStandard.Grob_Clamping.Path);
            await ApiInstance.Logic.SendLogicDefinitionAsync(basePathContent + LogicsStandard.Grob_Conveyor.Path);
            await ApiInstance.Logic.SendLogicDefinitionAsync(basePathContent + LogicsStandard.Grob_Cylinder.Path);
            await ApiInstance.Logic.SendLogicDefinitionAsync(basePathContent + LogicsStandard.Grob_GripperBasic.Path);
            await ApiInstance.Logic.SendLogicDefinitionAsync(basePathContent + LogicsStandard.Grob_LiftUnit.Path);
            await ApiInstance.Logic.SendLogicDefinitionAsync(basePathContent + LogicsStandard.Grob_PneumaticSupply.Path);
            await ApiInstance.Logic.SendLogicDefinitionAsync(basePathContent + LogicsStandard.Grob_SafetyDoor.Path);
            await ApiInstance.Logic.SendLogicDefinitionAsync(basePathContent + LogicsStandard.Grob_Stop.Path);


            // Import Grob Cabinet Definitions
            await ApiInstance.Interaction.ImportCabinetDefinitionAsync(basePathContent + CabinetElementPaths.TwoPositionSwitch);
            await ApiInstance.Interaction.ImportCabinetDefinitionAsync(basePathContent + CabinetElementPaths.ThreePositionSwitch);
            await ApiInstance.Interaction.ImportCabinetDefinitionAsync(basePathContent + CabinetElementPaths.NotAus);
            await ApiInstance.Interaction.ImportCabinetDefinitionAsync(basePathContent + CabinetElementPaths.Button);

        }


        public async static Task CreateFeeSimulationBaseAsync()
        {
            var station = await CreateFrameAsync("StationName");

            var assemblies = await CreateFrameAsync("Assemblies", station);
            var cad = await CreateFrameAsync("CAD", station);
            var cabinets = await CreateFrameAsync("Cabinets", station);
            var deposits = await CreateFrameAsync("Deposits", station);
            var workpieces = await CreateFrameAsync("Workpieces", station);
            var imported = await CreateFrameAsync("Imported", workpieces);
            var misc = await CreateFrameAsync("Misc", station);
            var temp = await CreateFrameAsync("Temp", station);


            FeeFloor Floor = new FeeFloor()
            {
                Name = "Floor",
                Parent = misc,
                Visible = true,
                Scale = new Vector3(30f, 30f, 0.05f),
                Position = new Vector3(0f, 0f, -0.025f),
                UseCollisionSlot = false,
            };
            await Floor.CreateAsync();
            await Floor.SendAndWaitAsync();
        }

        private static async Task<FeeBasicFrame> CreateFrameAsync(string name, FeeAbstractObject? parent = null, bool visible = false)
        {
            var frame = new FeeBasicFrame
            {
                Name = name,
                Parent = parent,
                Visible = visible
            };

            await frame.CreateAsync();
            await frame.SendAndWaitAsync();
            return frame;
        }








    }
}
