using System.Windows.Input;
using Tracker.Logging;

namespace Tracker.Command
{
    /// <summary>
    /// An async-aware implementation of ICommand that safely handles async operations.
    /// Prevents the common "async void" pitfall by properly handling exceptions.
    /// </summary>
    public class AsyncCommand : ICommand
    {
        private readonly Func<object?, Task> _execute;
        private readonly Predicate<object?>? _canExecute;
        private readonly ILogger? _logger;
        private readonly string _commandName;
        private bool _isExecuting;

        /// <summary>
        /// Creates a new async command.
        /// </summary>
        /// <param name="execute">The async method to execute.</param>
        /// <param name="canExecute">Optional predicate to determine if command can execute.</param>
        /// <param name="commandName">Name for logging purposes.</param>
        public AsyncCommand(
            Func<object?, Task> execute,
            Predicate<object?>? canExecute = null,
            string? commandName = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
            _commandName = commandName ?? execute.Method.Name;
            _logger = LoggingManager.GetComponentLogger("AsyncCommand");
        }

        /// <summary>
        /// Gets whether the command is currently executing.
        /// </summary>
        public bool IsExecuting => _isExecuting;

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object? parameter)
        {
            // Prevent re-entry while executing
            if (_isExecuting) return false;
            return _canExecute?.Invoke(parameter) ?? true;
        }

        public async void Execute(object? parameter)
        {
            if (!CanExecute(parameter)) return;

            _isExecuting = true;
            RaiseCanExecuteChanged();

            try
            {
                await _execute(parameter).ConfigureAwait(true);
            }
            catch (OperationCanceledException)
            {
                // Cancellation is not an error
                _logger?.Info($"Command '{_commandName}' was cancelled");
            }
            catch (Exception ex)
            {
                _logger?.Exception(ex, $"Error executing command '{_commandName}'");
                
                // Re-throw on UI thread so global exception handler can catch it
                // Or handle gracefully depending on requirements
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    Helpers.MessageBoxHelper.Show(
                        $"An error occurred: {ex.Message}",
                        "Error",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error);
                });
            }
            finally
            {
                _isExecuting = false;
                RaiseCanExecuteChanged();
            }
        }

        public void RaiseCanExecuteChanged() => CommandManager.InvalidateRequerySuggested();
    }

    /// <summary>
    /// An async command that supports cancellation.
    /// </summary>
    public class AsyncCancelableCommand : ICommand, IDisposable
    {
        private readonly Func<object?, CancellationToken, Task> _execute;
        private readonly Predicate<object?>? _canExecute;
        private readonly ILogger? _logger;
        private readonly string _commandName;
        private CancellationTokenSource? _cts;
        private bool _isExecuting;
        private bool _disposed;

        public AsyncCancelableCommand(
            Func<object?, CancellationToken, Task> execute,
            Predicate<object?>? canExecute = null,
            string? commandName = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
            _commandName = commandName ?? execute.Method.Name;
            _logger = LoggingManager.GetComponentLogger("AsyncCommand");
        }

        public bool IsExecuting => _isExecuting;

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object? parameter)
        {
            if (_isExecuting) return false;
            return _canExecute?.Invoke(parameter) ?? true;
        }

        public async void Execute(object? parameter)
        {
            if (!CanExecute(parameter)) return;

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            _isExecuting = true;
            RaiseCanExecuteChanged();

            try
            {
                await _execute(parameter, _cts.Token).ConfigureAwait(true);
            }
            catch (OperationCanceledException)
            {
                _logger?.Info($"Command '{_commandName}' was cancelled");
            }
            catch (Exception ex)
            {
                _logger?.Exception(ex, $"Error executing command '{_commandName}'");
                
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    Helpers.MessageBoxHelper.Show(
                        $"An error occurred: {ex.Message}",
                        "Error",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error);
                });
            }
            finally
            {
                _isExecuting = false;
                RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// Cancels the currently executing command.
        /// </summary>
        public void Cancel()
        {
            _cts?.Cancel();
        }

        public void RaiseCanExecuteChanged() => CommandManager.InvalidateRequerySuggested();

        public void Dispose()
        {
            if (_disposed) return;
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            _disposed = true;
        }
    }
}

