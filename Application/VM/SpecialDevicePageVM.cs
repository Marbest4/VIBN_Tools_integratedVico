using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Windows.Input;
using VIBN_Tools.GlobalClasses;
using VIBN_Tools.SpecialDevices;
using static VIBN_Tools.SpecialDevices.DeviceCatalog;

namespace VIBN_Tools.Application.VM
{
    /// <summary>
    /// Presents the special-device catalog and coordinates adding, deleting
    /// and creating configured device instances in the connected model.
    /// Device-specific behavior belongs to the catalog/factory and device type.
    /// </summary>
    public class SpecialDevicePageVM : MvvmBase
    {

        //===========================================================================================================================
        // P R O P E R T I E S   O F   V I E W - M O D E L
        //===========================================================================================================================




        //===========================================================================================================================
        // B I N D I N G S   -   M A N U A L   A D D   D E V I C E
        //===========================================================================================================================

        public IEnumerable<DeviceManufacturer> Manufacturers => Enum.GetValues(typeof(DeviceManufacturer)).Cast<DeviceManufacturer>();


        private DeviceManufacturer? _selectedManufacturer;
        public DeviceManufacturer? SelectedManufacturer
        {
            get => _selectedManufacturer;
            set
            {
                _selectedManufacturer = value;
                OnPropertyChanged();

                LoadDeviceTypesForManufacturer();
            }
        }

        public ObservableCollection<object> DeviceTypes { get; set; }



        private object _selectedDevice;
        public object SelectedDevice
        {
            get => _selectedDevice;
            set
            {
                _selectedDevice = value;
                OnPropertyChanged();

                if(value is GrobDeviceTypes.SimModeSiemens)
                {
                    IsEnabledDevicePrefix = false;
                    IsEnabledDeviceAddress = false;
                    DevicePrefix = "SimMode";
                }
                else
                {
                    IsEnabledDevicePrefix = true;
                    IsEnabledDeviceAddress = true;
                }


                    UpdateShowRobotType();
            }
        }


        public IEnumerable<RobotType> RobotTypes => Enum.GetValues(typeof(RobotType)).Cast<RobotType>();


        private RobotType? _selectedRobotType;
        public RobotType? SelectedRobotType
        {
            get { return _selectedRobotType; }
            set
            {
                _selectedRobotType = value;
                OnPropertyChanged();
            }
        }

        private bool _showRobotType;
        public bool ShowRobotType
        {
            get => _showRobotType;
            set { _showRobotType = value; OnPropertyChanged(); }
        }





        private string _devicePrefix;
        public string DevicePrefix
        {
            get { return _devicePrefix; }
            set
            {
                _devicePrefix = value;
                OnPropertyChanged();
            }
        }

        private bool _isEnabledDevicePrefix;
        public bool IsEnabledDevicePrefix
        {
            get { return _isEnabledDevicePrefix; }
            set 
            { 
                _isEnabledDevicePrefix = value; 
                OnPropertyChanged();
            }
        }


        private int _deviceAddressInput;
        public int DeviceAddressInput
        {
            get { return _deviceAddressInput; }
            set
            {
                _deviceAddressInput = value;
                OnPropertyChanged();
            }
        }

        private int _deviceAddressOutput;
        public int DeviceAddressOutput
        {
            get { return _deviceAddressOutput; }
            set
            {
                _deviceAddressOutput = value;
                OnPropertyChanged();
            }
        }

        public SpecialDeviceAddresses DeviceAddresses => new SpecialDeviceAddresses(DeviceAddressOutput, DeviceAddressInput);



        private bool _isEnabledDeviceAddress;
        public bool IsEnabledDeviceAddress
        {
            get { return _isEnabledDeviceAddress; }
            set
            {
                _isEnabledDeviceAddress = value;
                OnPropertyChanged();
            }
        }


        public ICommand AddSpecialDevice => GetCommandBinding(Add_SpecialDevice);





        //===========================================================================================================================
        // B I N D I N G S   -   A U T O M A T I C   A D D   D E V I C E
        //===========================================================================================================================

        public ICommand ConnectTia => GetCommandBinding(Connect_Tia);


        private bool _isBusyConnectTia;
        public bool IsBusyConnectTia
        {
            get { return _isBusyConnectTia; }
            set
            {
                _isBusyConnectTia = value;
                OnPropertyChanged();
            }
        }




        //===========================================================================================================================
        // B I N D I N G S   -   C R E A T E   D E V I C E S
        //===========================================================================================================================


        public ObservableCollection<SpecialDevice> ListOfSepcialDevices { get; set; } = new ObservableCollection<SpecialDevice>();

        private int _selectedDeviceIndex;
        public int SelectedDeviceIndex
        {
            get { return _selectedDeviceIndex; }
            set { _selectedDeviceIndex = value; }
        }



        public ICommand DeleteSelectedDevices => GetCommandBinding(Delete_SelectedDevices);
        public ICommand DeleteAllDevices => GetCommandBinding(Delete_AllDevices);
        public ICommand CreateSpecialDevices => GetCommandBinding(Create_SpecialDevicesAsync);



        private bool _isBusyCreateDevices;
        public bool IsBusyCreateDevices
        {
            get { return _isBusyCreateDevices; }
            set
            {
                _isBusyCreateDevices = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsEnabledDeleteSelectedDevice));
                OnPropertyChanged(nameof(IsEnabledDeleteAllDevices));
            }
        }

        public bool IsEnabledDeleteSelectedDevice => !IsBusyCreateDevices;

        public bool IsEnabledDeleteAllDevices => !IsBusyCreateDevices;







        //===========================================================================================================================
        // C O N S T R U C T O R
        //===========================================================================================================================

        public SpecialDevicePageVM()
        {
            DeviceTypes = new ObservableCollection<object>();

            IsBusyCreateDevices = false;

        }



        //===========================================================================================================================
        // M E T H O D S
        //===========================================================================================================================


        private void Add_SpecialDevice(object parameter)
        {
            if (SelectedManufacturer is null || SelectedDevice is null)
                return;

            var device = DeviceFactory.Create(SelectedManufacturer.Value, (Enum)SelectedDevice, DevicePrefix, DeviceAddresses, SelectedRobotType);
            ListOfSepcialDevices.Add(device);
        }


        private async void Create_SpecialDevicesAsync(object parameter)
        {
            IsBusyCreateDevices = true;

            var devicesToRemove = new ConcurrentBag<SpecialDevice>();

            await Parallel.ForEachAsync(ListOfSepcialDevices, async (el, cancellationToken) =>
            {
                if (await el.CreateAsync())
                    devicesToRemove.Add(el);
            });


            foreach (var device in devicesToRemove)
            {
                ListOfSepcialDevices.Remove(device);
            }

            IsBusyCreateDevices = false;
        }



        private void Delete_SelectedDevices(object parameter)
        {
            if (SelectedDeviceIndex >= 0)
            {
                ListOfSepcialDevices.RemoveAt(SelectedDeviceIndex);
            }
        }

        // Delete all Devices from List
        private void Delete_AllDevices(object parameter)
        {
            ListOfSepcialDevices.Clear();
        }



        private async void Connect_Tia(object parameter)
        {
            IsBusyConnectTia = true;

            var devices = await TiaService.TiaDeviceService.GetTiaHardwareDevices();

            foreach (var device in devices)
            {
                ListOfSepcialDevices.Add(device);
            }

            IsBusyConnectTia = false;
        }



        //===========================================================================================================================
        // M E T H O D S   ( H E L P E R S )
        //===========================================================================================================================

        private void LoadDeviceTypesForManufacturer()
        {
            DeviceTypes.Clear();

            if (SelectedManufacturer is null)
                return;

            var enumType = DeviceCatalog.DeviceTypeEnums[SelectedManufacturer.Value];

            foreach (var t in Enum.GetValues(enumType))
            {
                DeviceTypes.Add(t);
            }
        }


        private void UpdateShowRobotType()
        {
            ShowRobotType = SelectedDevice != null && RobotTypeDevices.Contains(SelectedDevice);
        }



        private static readonly HashSet<object> RobotTypeDevices = new HashSet<object>()
        {
            AtlasCopcoDeviceTypes.Sys6000_Glueing_BMW,
            AtlasCopcoDeviceTypes.Sys6000_Glueing_VASS
        };





    }
}
