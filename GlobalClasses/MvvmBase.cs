using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FS.SDK.Extensibility.Contracts;
using VIBN_Tools.GlobalClasses.FeeObjects;



namespace VIBN_Tools.GlobalClasses
{
    public class MvvmBase : INotifyPropertyChanged
    {

        public class CommandHandler : ICommand
        {
            private Action<object> _execute;
            private Func<object, bool> _canExecute;
            public CommandHandler(Action<object> execute, Func<object, bool> canExecute = null)
            {
                _execute = execute;
                _canExecute = canExecute ?? (_ => true);
            }

            public bool CanExecute(object parameter) => _canExecute(parameter);
            public void Execute(object parameter) => _execute(parameter);

            public event EventHandler CanExecuteChanged;

            public void RaiseCanExecuteChanged()
            {
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            }
        }


        public class AsyncCommandHandler : ICommand
        {
            private readonly Func<object, Task> _executeAsync;
            private readonly Func<object, bool> _canExecute;

            public AsyncCommandHandler(Func<object, Task> executeAsync, Func<object, bool> canExecute = null)
            {
                _executeAsync = executeAsync;
                _canExecute = canExecute ?? (_ => true);
            }

            public bool CanExecute(object parameter) => _canExecute(parameter);

            public async void Execute(object parameter)
            {
                try
                {
                    await _executeAsync(parameter);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }// => await _executeAsync(parameter);

            public event EventHandler CanExecuteChanged;

            public void RaiseCanExecuteChanged()
            {
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            }
        }



        public event PropertyChangedEventHandler PropertyChanged;

        public virtual void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));






        // Overload for using with command parameter
        public ICommand GetCommandBinding(Action<object> executeAction)
        {
            return new CommandHandler(executeAction);
        }

        // Overload for using without any parameter
        public ICommand GetCommandBinding(Action executeAction)
        {
            return new CommandHandler(_ => executeAction());
        }



        // Overload for using with command parameter

        public ICommand GetCommandBindingAsync(Func<object, Task> executeAsync)
        {
            return new AsyncCommandHandler(executeAsync);
        }

        // Overload for using without any parameter
        public ICommand GetCommandBindingAsync(Func<Task> executeAsync)
        {
            return new AsyncCommandHandler(_ => executeAsync());
        }



    }



    public abstract class NotifyBase : INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler PropertyChanged;

        protected bool SetPropertyChange<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }

        public virtual void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    }



    public abstract class OptionsViewModelBase : MvvmBase
    {
        public string Label { get; set; }
        public ViewElement ViewElement { get; set; }


        private bool _viewElementEnabled;
        public bool ViewElementEnabled
        {
            get => _viewElementEnabled;
            set
            {
                _viewElementEnabled = value;
                OnPropertyChanged();
            }
        }

        public abstract object ValueObject { get; set; }
    }


    public class OptionsViewModel<T> : OptionsViewModelBase where T : struct
    {

        private T? _value;
        public T? Value
        {
            get => _value;
            set
            { _value = value; OnPropertyChanged(); }
        }

        public override object ValueObject
        {
            get => Value;
            set => Value = value as T?;
        }


        private ObservableCollection<T> _items;
        public ObservableCollection<T> Items
        {
            get => _items;
            set
            { _items = value; OnPropertyChanged(); }
        }
    }


    public class StatusViewModel : OptionsViewModelBase
    {
        private object _value;
        public override object ValueObject
        {
            get => _value;
            set { _value = value; OnPropertyChanged(); }
        }
    }



    public class AxisSelectionViewModel : NotifyBase
    {

        private readonly ObservableCollection<FeeJoint> _allJoints;
        private readonly Func<AxisSelectionViewModel, FeeJoint, bool> _isJointAvailable;

        public int AxisIndex { get; }
        public string DisplayName => $"Axis {AxisIndex}";


        private FeeJoint _selectedJoint;
        public FeeJoint SelectedJoint
        {
            get => _selectedJoint;
            set
            {
                if(SetPropertyChange(ref _selectedJoint, value))
                {
                    SelectionChanged?.Invoke(this, EventArgs.Empty);
                }
            }                
        }

        public event EventHandler SelectionChanged;

        public ObservableCollection<FeeJoint> Options { get; } = new ObservableCollection<FeeJoint>();    //=> _allJoints.Where(j => _isJointAvailable(this, j) || j == SelectedJoint);

        public AxisSelectionViewModel(int axisIndex, ObservableCollection<FeeJoint> allJoints, Func<AxisSelectionViewModel, FeeJoint, bool> isJointAvailable)
        {
            AxisIndex = axisIndex;
            _allJoints = allJoints;
            _isJointAvailable = isJointAvailable;

            _allJoints.CollectionChanged += (s, e) =>
            {
                RefreshOptions();
            };
        }


        private void RefreshOptions()
        {
            Options.Clear();
            foreach(var j in _allJoints.Where(j => _isJointAvailable(this, j) || j == SelectedJoint))
            {
                Options.Add(j);
            }
        }
    }






    public class InterfaceConnectViewModel : NotifyBase
    {



        private FeeInterface _selectedInterface;
        public FeeInterface SelectedInterface
        {
            get => _selectedInterface;
            set
            {
                if (SetPropertyChange(ref _selectedInterface, value))
                {
                    OnPropertyChanged(nameof(CanUseDbAdressing));
                }
            }
        }


        private int _startByte;
        public int StartByte
        {
            get => _startByte;
            set => SetPropertyChange(ref _startByte, value);
        }

        private int _byteCount;
        public int ByteCount
        {
            get => _byteCount;
            set => SetPropertyChange(ref _byteCount, value);
        }



        public string AddressPrefix {  get; set; }


        private bool _useDbAddressing;
        public bool UseDbAddressing
        {
            get => _useDbAddressing;
            set => SetPropertyChange(ref _useDbAddressing, value);
        }

        private int _dbOutNumber;
        public int DbOutNumber
        {
            get => _dbOutNumber;
            set => SetPropertyChange(ref _dbOutNumber, value);
        }

        private int _dbInNumber;
        public int DbInNumber
        {
            get => _dbInNumber;
            set => SetPropertyChange(ref _dbInNumber, value);
        }



        private InterfaceConnectMode _selectedMode;
        public InterfaceConnectMode SelectedMode
        {
            get => _selectedMode;
            set => SetPropertyChange(ref _selectedMode, value);
        }



        private static readonly HashSet<Guid> _dbPossibleInterfaceProviders = new HashSet<Guid>()
        {
            PluginGuids.SiemensPLCSIMAdvanced,
            PluginGuids.SiemensPLCSIMAdvancedNetwork,
            PluginGuids.SiemensS7Online,
            PluginGuids.SiemensSinumerikOne,
            PluginGuids.SiemensSinumerikOneNetwork,
        };


        public bool CanUseDbAdressing => SelectedInterface != null && _dbPossibleInterfaceProviders.Contains(SelectedInterface.ProviderGuid);



    }







    public static class VisualTreeHelpers
    {
        public static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T match)
                    return match;

                current = VisualTreeHelper.GetParent(current);
            }

            return null;
        }



        public static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T t)
                    return t;

                var result = FindVisualChild<T>(child);
                if (result != null)
                    return result;
            }
            return null;
        }



        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null)
                yield break;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T typedChild)
                    yield return typedChild;

                foreach (var descendant in FindVisualChildren<T>(child))
                    yield return descendant;
            }
        }


        public static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            while (child != null)
            {
                if (child is T parent)
                    return parent;

                child = VisualTreeHelper.GetParent(child);
            }
            return null;
        }
    }





}
