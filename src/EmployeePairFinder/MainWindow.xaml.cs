using System.Windows;
using EmployeePairFinder.ViewModels;

namespace EmployeePairFinder;

/// <summary>
/// Code-behind for MainWindow.xaml.
///
/// Intentionally minimal: the only responsibility is to receive the ViewModel
/// via constructor injection (supplied by the DI container in App.xaml.cs) and
/// assign it as the DataContext. All business logic lives in MainViewModel;
/// all presentation logic lives in the XAML bindings.
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// Initialises the window with the injected ViewModel as its DataContext.
    /// </summary>
    /// <param name="viewModel">
    /// The fully constructed ViewModel provided by the DI container.
    /// </param>
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}