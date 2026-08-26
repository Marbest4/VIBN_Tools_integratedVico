using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using VIBN_Tools.GlobalClasses;
using VIBN_Tools.Settings;

namespace VIBN_Tools.Application.VM
{
    public class MiniToolsPageVM : MvvmBase
    {

        //===========================================================================================================================
        // B I N D I N G S   -   B U T T O N S
        //===========================================================================================================================

        // Button Create Joints
        public ICommand DeactivateForcing => GetCommandBindingAsync(DeactivateForcingMode);

        private bool _isBusyDeactivateForcing;
        public bool IsBusyDeactivateForcing
        {
            get { return _isBusyDeactivateForcing; }
            set
            {
                _isBusyDeactivateForcing = value;
                OnPropertyChanged();
            }
        }

        public ICommand HideObjects => GetCommandBindingAsync(Hide_Objects);

        private bool _isBusyHideObjects;
        public bool IsBusyHideObjects
        {
            get { return _isBusyHideObjects; }
            set
            {
                _isBusyHideObjects = value;
                OnPropertyChanged();
            }
        }

        public ICommand ToggleInserter => GetCommandBindingAsync(Toggle_Inserter);

        private bool _isBusyToggleInserter;
        public bool IsBusyToggleInserter
        {
            get { return _isBusyToggleInserter; }
            set
            {
                _isBusyToggleInserter = value;
                OnPropertyChanged();
            }
        }

        public ICommand CreateSurface => GetCommandBindingAsync(Create_Surface);

        private bool _isBusyCreateSurface;
        public bool IsBusyCreateSurface
        {
            get { return _isBusyCreateSurface; }
            set
            {
                _isBusyCreateSurface = value;
                OnPropertyChanged();
            }
        }


        //===========================================================================================================================
        // P R O P E R T I E S   O F   V I E W - M O D E L
        //===========================================================================================================================

        //List<FeeJoint> JointsList { get; set; }


        public ProjectSettings ProjectSettings { get; } = new();






        //===========================================================================================================================
        // C O N S T R U C T O R
        //===========================================================================================================================

        //public CadWizardPageVM()
        //{
        //    JointsList = new List<FeeJoint>();
        //}



        //===========================================================================================================================
        // M E T H O D S
        //===========================================================================================================================

        private async Task DeactivateForcingMode(object arg)
        {
            MessageBox.Show("In progress");
            IsBusyDeactivateForcing = true;



            //JointsList.Clear();
            //JointsList = await CadWizardService.SearchCodingsAsync<FeeJoint>(FeeJoint.CadWizardFactory);

            //await Parallel.ForEachAsync(JointsList, async (el, token) =>
            //{
            //    el.Create();
            //    if (await el.SendAndWaitAsync())
            //    {
            //        await el.ReparentCadDecoToJointAsync();

            //        // Create Axis Logic
            //        await CadWizardService.VIBN_Tools.ContainerGenerationAxisLogicAsync(el, _projectSettings.SelectedTemplate);

            //    }
            //});

            //await CadWizardService.ReparentObjectsToBasicFrame(JointsList, "TempGeneratedJoints");

            IsBusyDeactivateForcing = false;

        }
        private async Task Hide_Objects(object arg)
        {
            MessageBox.Show("In progress");
        }

        private async Task Toggle_Inserter(object arg)
        {
            MessageBox.Show("In progress");
        }

        private async Task Create_Surface(object arg)
        {
            MessageBox.Show("In progress");
        }






        //===========================================================================================================================
        // M E T H O D S   ( H E L P E R S )
        //===========================================================================================================================



    }
}
