using System.Windows.Input;

namespace Tracker.Command
{
    public class TrackerCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Predicate<object?>? _canExecute;

        public TrackerCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
        {
            _canExecute = canExecute;
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        }

        public event EventHandler? CanExecuteChanged
            {
                add => CommandManager.RequerySuggested += value;
                remove => CommandManager.RequerySuggested -= value;
            }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

        public void Execute(object? parameter) => _execute?.Invoke(parameter);

        public void RaiseCanExecuteChanged() => CommandManager.InvalidateRequerySuggested();

    }
}
