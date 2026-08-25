using FS.API;
using FS.SDK;
using System.Windows.Input;
using VIBN_Tools.CAD_Wizard;
using VIBN_Tools.GlobalClasses;
using VIBN_Tools.GlobalClasses.FeeObjects;
using VIBN_Tools.Settings;

namespace VIBN_Tools.Application.VM
{
    /// <summary>
    /// Coordinates CAD-derived FEE helper generation such as joints, sensors,
    /// conveyors and templates. Creation commands remain asynchronous because
    /// they operate on the connected simulation model.
    /// </summary>
    public class CadWizardPageVM : MvvmBase
    {


        //===========================================================================================================================
        // B I N D I N G S   -   B U T T O N S
        //===========================================================================================================================

        // Button Create Joints
        public ICommand CreateJoints => GetCommandBindingAsync(Create_Joints);

        private bool _isBusyCreateJoints;
        public bool IsBusyCreateJoints
        {
            get { return _isBusyCreateJoints; }
            set
            {
                _isBusyCreateJoints = value;
                OnPropertyChanged();
            }
        }

        // Button Create Sensors
        public ICommand CreateSensors => GetCommandBindingAsync(Create_Sensors);

        private bool _isBusyCreateSensors;
        public bool IsBusyCreateSensors
        {
            get { return _isBusyCreateSensors; }
            set
            {
                _isBusyCreateSensors = value;
                OnPropertyChanged();
            }
        }


        // Button Create Conveyors
        public ICommand CreateConveyors => GetCommandBindingAsync(Create_Conveyors);

        private bool _isBusyCreateConveyors;
        public bool IsBusyCreateConveyors
        {
            get { return _isBusyCreateConveyors; }
            set
            {
                _isBusyCreateConveyors = value;
                OnPropertyChanged();
            }
        }

        // Button Create Templates
        public ICommand CreateTemplates => GetCommandBindingAsync(Create_Templates);

        private bool _isBusyCreateTemplates;
        public bool IsBusyCreateTemplates
        {
            get { return _isBusyCreateTemplates; }
            set
            {
                _isBusyCreateTemplates = value;
                OnPropertyChanged();
            }
        }





        // Button Delete Empty Nodes
        public ICommand DeleteEmptyNodes => GetCommandBindingAsync(Delete_EmptyNodes);

        private bool _isBusyDeleteEmptyNodes;
        public bool IsBusyDeleteEmptyNodes
        {
            get { return _isBusyDeleteEmptyNodes; }
            set
            {
                _isBusyDeleteEmptyNodes = value;
                OnPropertyChanged();
            }
        }







        //===========================================================================================================================
        // P R O P E R T I E S   O F   V I E W - M O D E L
        //===========================================================================================================================

        List<FeeJoint> JointsList { get; set; }
        List<FeeSensor> SensorsList { get; set; }
        List<FeeSurface> ConveyorsList { get; set; }
        List<FeeDecoration> TemplatesList { get; set; }



        private readonly ProjectSettings _projectSettings;
        public ProjectSettings ProjectSettings => _projectSettings;






        //===========================================================================================================================
        // C O N S T R U C T O R
        //===========================================================================================================================

        //public CadWizardPageVM()
        //{
        //    JointsList = new List<FeeJoint>();
        //}

        public CadWizardPageVM(ProjectSettings projectSettings)
        {
            _projectSettings = projectSettings;
            JointsList = new List<FeeJoint>();
            SensorsList = new List<FeeSensor>();
            ConveyorsList = new List<FeeSurface>();
            TemplatesList = new List<FeeDecoration>();
        }



        //===========================================================================================================================
        // M E T H O D S
        //===========================================================================================================================

        private async Task Create_Joints(object arg)
        {

            IsBusyCreateJoints = true;

            JointsList.Clear();
            JointsList = await CadWizardService.SearchCodingsAsync<FeeJoint>(FeeJoint.CadWizardFactory);

            await Parallel.ForEachAsync(JointsList, async (el, token) =>
            {
                await el.CreateAsync();
                if (await el.SendAndWaitAsync())
                {
                    await el.ReparentCadDecoToJointAsync();

                    // Create Axis Logic
                    await CadWizardService.CreateAndAssignAxisLogicAsync(el, _projectSettings.SelectedTemplate);

                }
            });

            await CadWizardService.ReparentObjectsToBasicFrame(JointsList, "TempGeneratedJoints");

            IsBusyCreateJoints = false;

        }



        private async Task Create_Sensors(object arg)
        {
            IsBusyCreateSensors = true;

            SensorsList.Clear();
            SensorsList = await CadWizardService.SearchCodingsAsync<FeeSensor>(FeeSensor.CadWizardFactory);

            await Parallel.ForEachAsync(SensorsList, async (el, token) =>
            {
                await el.CadWizardCreateAndSendAsync();
            });

            await CadWizardService.ReparentObjectsToBasicFrame(SensorsList, "TempGeneratedSensors");

            IsBusyCreateSensors = false;
        }



        private async Task Create_Conveyors(object arg)
        {

            IsBusyCreateConveyors = true;

            ConveyorsList.Clear();
            ConveyorsList = await CadWizardService.SearchCodingsAsync<FeeSurface>(FeeSurface.CadWizardFactory);

            await Parallel.ForEachAsync(ConveyorsList, async (el, token) =>
            {
                await el.CreateAsync();
                await el.SendAndWaitAsync();
            });

            await CadWizardService.ReparentObjectsToBasicFrame(ConveyorsList, "TempGeneratedConveyors");

            IsBusyCreateConveyors = false;

        }


        private async Task Create_Templates(object arg)
        {

            IsBusyCreateTemplates = true;

            TemplatesList.Clear();
            TemplatesList = await CadWizardService.SearchCodingsAsync<FeeDecoration>(FeeDecoration.CadWizardFactory);

            await Parallel.ForEachAsync(TemplatesList, async (el, token) =>
            {

                if (await el.CadWizardCreateAndSendAsync())
                {
                    // Compound
                    var childrenTemp = (await Services.ApiInstance.Object.GetAllChildrenFromSceneObjectAsync(el.Guid.ToString())).ToList();
                    if (childrenTemp.Count() > 0 && !ApiEnums.IsErrorCode(childrenTemp[0]))
                    {
                        await Services.ApiInstance.Object.CompoundObjectsAsync(el.Guid.ToString(), true, childrenTemp.ToArray());
                    }
                }
            });

            await CadWizardService.ReparentObjectsToBasicFrame(TemplatesList, "TempGeneratedTemplates");

            IsBusyCreateTemplates = false;

        }




        private async Task Delete_EmptyNodes(object arg)
        {
            IsBusyDeleteEmptyNodes = true;
            bool deleteSomething;

            do
            {
                deleteSomething = false;

                var guids = await Services.ApiInstance.Object.GetSceneObjectGuidsOfTypeAsync(nameof(CADAssembly));

                await Parallel.ForEachAsync(guids, async (el, token) =>
                {
                    var children = (await Services.ApiInstance.Object.GetAllChildrenFromSceneObjectAsync(el)).ToList();

                    if (children.Count() < 2 && ApiEnums.IsErrorCode(children[0]))
                    {
                        Services.ApiInstance.Object.DeleteObject(el);
                        deleteSomething = true;
                    }
                });
            }
            while (deleteSomething);

            IsBusyDeleteEmptyNodes = false;

        }






        //===========================================================================================================================
        // M E T H O D S   ( H E L P E R S )
        //===========================================================================================================================



















        //===========================================================================================================================
        // M I N I - T O O L S   ( T O   M  O V E )
        //===========================================================================================================================

        public ICommand WriteMarksToNames => GetCommandBindingAsync(WriteMarks_ToNames);
        private bool _isBusyWritingMarks;

        public bool IsBusyWritingMarks
        {
            get { return _isBusyWritingMarks; }
            set 
            { 
                _isBusyWritingMarks = value;
                OnPropertyChanged();
            }
        }



        private async Task WriteMarks_ToNames(object parameter)
        {
            IsBusyWritingMarks = true;

            var allObjectGuids = await Services.ApiInstance.Object.GetSceneObjectGuidsAsync();

            foreach (var guidString in allObjectGuids)
            {
                Guid guid = Guid.Parse(guidString);

                var name = Services.ApiInstance.XmlHelper.ConvertToString( await Services.ApiInstance.Object.GetPropertyAsync(guid, nameof(SceneObject.Name)));
                var mark = Services.ApiInstance.XmlHelper.ConvertToString(await Services.ApiInstance.Object.GetPropertyAsync(guid, nameof(SceneObject.MarkComponent.Mark), "MarkComponent"));

                var type = await Services.ApiInstance.Object.GetPropertyAsync(guid, nameof(SceneObject.Type));

                Services.ApiInstance.Object.CreateObject(type, guid);

                if (!string.IsNullOrEmpty(mark))

                {
                    await Services.ApiInstance.Object.SetPropertyAsync(guid, nameof(SceneObject.Name), $"{name} [{mark}]");

                    await Services.ApiInstance.Object.SendAndWait(guid);
                }

            }

            IsBusyWritingMarks = false;
        }


    }
}
