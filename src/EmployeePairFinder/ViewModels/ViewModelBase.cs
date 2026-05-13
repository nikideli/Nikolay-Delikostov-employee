using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EmployeePairFinder.ViewModels;

// Base class for all ViewModels: INotifyPropertyChanged + SetField<T> helper.
public abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    // Assigns the new value and raises PropertyChanged only when the value actually changed.
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
