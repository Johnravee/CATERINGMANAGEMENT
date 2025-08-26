using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CATERINGMANAGEMENT.Helpers
{
    // ✅ Non-generic RelayCommand (sync + async)
    public class RelayCommand : ICommand
    {
        private readonly Action? _execute;
        private readonly Func<Task>? _executeAsync;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public RelayCommand(Func<Task> executeAsync, Func<bool>? canExecute = null)
        {
            _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) =>
            _canExecute == null || _canExecute();

        public async void Execute(object? parameter)
        {
            if (_execute != null)
                _execute();
            else if (_executeAsync != null)
                await _executeAsync();
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value!;
            remove => CommandManager.RequerySuggested -= value!;
        }
    }

    // ✅ Generic RelayCommand<T> (sync + async)
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T>? _execute;
        private readonly Func<T, Task>? _executeAsync;
        private readonly Predicate<T>? _canExecute;

        public RelayCommand(Action<T> execute, Predicate<T>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public RelayCommand(Func<T, Task> executeAsync, Predicate<T>? canExecute = null)
        {
            _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            if (parameter == null && typeof(T).IsValueType) return _canExecute == null;
            return _canExecute == null || _canExecute((T)parameter!);
        }

        public async void Execute(object? parameter)
        {
            if (parameter == null && typeof(T).IsValueType) return;

            if (_execute != null)
                _execute((T)parameter!);
            else if (_executeAsync != null)
                await _executeAsync((T)parameter!);
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value!;
            remove => CommandManager.RequerySuggested -= value!;
        }
    }
}
