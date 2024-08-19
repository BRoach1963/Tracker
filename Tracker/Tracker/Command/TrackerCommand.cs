using System.Windows.Input;

namespace Tracker.Command
{
    public class TrackerCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
        : ICommand
    {
        private readonly Action<object?> _execute = execute ?? throw new ArgumentNullException(nameof(execute));

        public event EventHandler? CanExecuteChanged
            {
                add => CommandManager.RequerySuggested += value;
                remove => CommandManager.RequerySuggested -= value;
            }

        public bool CanExecute(object? parameter) => canExecute?.Invoke(parameter) ?? true;

        public void Execute(object? parameter) => _execute?.Invoke(parameter);

        public void RaiseCanExecuteChanged() => CommandManager.InvalidateRequerySuggested();

    }
}
