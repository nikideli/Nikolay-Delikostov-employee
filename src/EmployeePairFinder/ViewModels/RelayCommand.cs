using System.Windows.Input;

namespace EmployeePairFinder.ViewModels;

// Lightweight ICommand implementation that wraps Action + Predicate delegates.
// Hooks into CommandManager.RequerySuggested so WPF re-evaluates CanExecute
// automatically after UI events without manual RaiseCanExecuteChanged calls.
public sealed class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Predicate<object?>? _canExecute;

    public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
    {
        _execute    = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged
    {
        add    => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
    public void Execute(object? parameter)    => _execute(parameter);

    public static void InvalidateAll() => CommandManager.InvalidateRequerySuggested();
}
