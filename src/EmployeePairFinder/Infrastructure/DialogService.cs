using Microsoft.Win32;

namespace EmployeePairFinder.Infrastructure;

// Production implementation: wraps Microsoft.Win32.OpenFileDialog.
// Lives in the infrastructure layer so the ViewModel stays free of WPF UI types.
public sealed class DialogService : IDialogService
{
    public string? OpenFile(string filter = "All Files|*.*")
    {
        var dialog = new OpenFileDialog { Filter = filter, Title = "Select Employee CSV File" };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
