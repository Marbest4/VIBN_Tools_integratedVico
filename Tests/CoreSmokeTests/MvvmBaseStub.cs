using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace VIBN_Tools.GlobalClasses;

public class MvvmBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public ICommand GetCommandBinding(Action executeAction) =>
        new CommandHandler(_ => executeAction());

    public ICommand GetCommandBindingAsync(Func<Task> executeAsync) =>
        new CommandHandler(async _ => await executeAsync());

    private sealed class CommandHandler : ICommand
    {
        private readonly Action<object?> _execute;

        public CommandHandler(Action<object?> execute)
        {
            _execute = execute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => _execute(parameter);
    }
}
