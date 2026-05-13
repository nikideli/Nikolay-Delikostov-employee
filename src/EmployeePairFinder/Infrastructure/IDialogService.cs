namespace EmployeePairFinder.Infrastructure;

// Abstraction over the file-open dialog — keeps the ViewModel free of WPF UI dependencies.
public interface IDialogService
{
    // Returns the selected file path, or null if the user cancelled.
    string? OpenFile(string filter = "All Files|*.*");
}
