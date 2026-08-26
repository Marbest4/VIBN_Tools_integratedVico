using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FS.SDK.Mathematics;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using VIBN_Tools.ContainerToFee;
using VIBN_Tools.ContainerToFee.GrobStandard;
using VIBN_Tools.GlobalClasses.FeeObjects;
using VIBN_Tools.ModelValidation;

namespace VIBN_Tools.GlobalClasses
{
    public class Interfaces
    {


        //===================================================================================================================================
        // I N T E R F A C E S   F E E   O B J E C T S
        //===================================================================================================================================

        public interface IFeeObjectService
        {
            IReadOnlyList<FeeAbstractObject> AllFeeObjects { get; }
            Task UpdateFeeDataAsync();
        }







        //===================================================================================================================================
        // I N T E R F A C E S   C O N T A I N E R   G E N E R A T I O N
        //===================================================================================================================================

        public interface IAssignableSimObject
        {
            ISimObjectFindOrSelect AssignedContainer { get; set; }
        }

        public interface ISimObjectFindOrSelect
        {
            // Search for a SimObject inside a collection
            void FindSimObjects(ObservableCollection<FeeAbstractObject> mappableSimObjects);

            // Get SimObjects for each collection
            IEnumerable<SimObjectTarget> GetSimObjectTargets();
        }

        public interface IContainerFactory
        {
            Task CreateContainerAsync(ContainerBaseClass container, FeeInterface targetInterface, FeeAbstractObject parentObject);
        }

        // Container type interfaces
        public interface ICreatableContainer
        {
            bool IsCreationRequested { get; set; }
        }

        public interface ILogicSimObjectOwner : ICreatableContainer
        {
            Task<FeeLogic> CreateLogicAsync(FeeAbstractObject parentObject);
            Task AssignSignalsAsync(FeeInterface targetInterface);
            Task CreateSimObjectsAsync();
            Task AssignSimObjectsAsync();
        }

        public interface ILogicOwner
        {
            Task<FeeLogic> CreateLogicAsync(FeeAbstractObject parentObject);
            Task AssignSignalsAsync(FeeInterface targetInterface);
        }

        public interface ISimObjectOwner : ICreatableContainer
        {
            Task CreateSimObjectsAsync(FeeAbstractObject parentObject);
            Task AssignSignalsAsync(FeeInterface targetInterface);
        }

        public interface ICabinetElementOwner : ISimObjectOwner
        {
            string CabinetName { get; }
            System.Numerics.Vector2 ElementPosition { get; set; }
        }


        public interface IAddonContainer
        {
            Task ConnectToParentAsync();
            GrobGripperBasic_Container ParentContainer { get; set; }
        }
        public interface IAddonContainer<TParent> : IAddonContainer
            where TParent : ContainerBaseClass
        {
            new TParent ParentContainer { get; set; }
        }





        //===================================================================================================================================
        // I N T E R F A C E S   Z U L I - C O N V E R T E R
        //===================================================================================================================================

        public interface IZuliTypeDefinitionBase
        {
            string TypeName { get; }
            bool Matches(IWorkbook workbook);

            int SheetIndex { get; }
            int HeaderRow { get; }
            int FirstDataRow { get; }

            IZuliToInterface ParseRowGeneric(IRow row, DataFormatter formatter, IFormulaEvaluator evaluator);

            bool VerifyLine(IZuliToInterface line);
        }

        public interface IZuliTypeDefinition<T> : IZuliTypeDefinitionBase
        {
            T ParseRow(IRow row, DataFormatter formatter, IFormulaEvaluator evaluator);
        }

        public interface IZuliToInterface
        {
            string Symbolic { get; }
            string DataType { get; }

            string TextLanguage1 { get; }
            string TextLanguage2 { get; }
            string TextLanguage3 { get; }
            string TextLanguage4 { get; }

        }

        public interface IZuliExportStrategyBase
        {
            string ApplicationName { get; }
            string RobotType { get; }
            XSSFWorkbook CreateWorkbook();
            void WriteLineToExcel(IRow row, IZuliToInterface line, LanguageType selectedLanguage);
        }

        public interface IZuliExportStrategy<TLine> : IZuliExportStrategyBase where TLine : IZuliToInterface
        {
            void WriteLineToExcel(IRow row, TLine line, LanguageType selectedLanguage);
        }





        //===================================================================================================================================
        // I N T E R F A C E S   C A D - W I Z A R D
        //===================================================================================================================================

        public interface IAxisLogicStrategy
        {
            Task<bool> CreateAndAssignAxisLogicAsync(FeeJoint joint);
        }

        public interface ICadWizardCreatable<T> where T : ICadWizardCreatable<T>
        {
            static abstract T? CadWizardFactory(string name, Vector3 position, Vector3 rotation, Guid cadDecoGuid);
        }





        //===================================================================================================================================
        // I N T E R F A C E S   M O D E L - V A L I D A T I O N
        //===================================================================================================================================

        public interface IPlausibilityCheck
        {
            Task CheckObjectIssuesAsync(IEnumerable<FeeAbstractObject> newObjects);
        }

        public interface IPlausibilityCheck<TParam> : IPlausibilityCheck
        {
            Task CheckObjectIssuesAsync(TParam parameter);
        }


        public interface ILogicValidator
        {
            Task<IEnumerable<PlausibilityIssue>> ValidateAsync(FeeLogic logicObject);
        }

        public interface IChangeHandler
        {
            Type TargetType { get; }
            Task HandleChangeAsync(FeeAbstractObject obj, string propertyName);




        }
    }
}
