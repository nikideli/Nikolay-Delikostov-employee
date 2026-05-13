using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using EmployeePairFinder.Enums;
using EmployeePairFinder.Infrastructure;
using EmployeePairFinder.Models;
using EmployeePairFinder.Services;

namespace EmployeePairFinder.ViewModels;

// All UI state, commands, and async orchestration for the main window.
public sealed class MainViewModel : ViewModelBase
{
    // ── Injected services (dependency-inverted via interfaces) ────────────────
    private readonly IDialogService      _dialogService;
    private readonly ICsvParserService   _csvParser;
    private readonly IEmployeePairService _pairService;

    // ── Backing fields ────────────────────────────────────────────────────────
    private string _filePath        = string.Empty;
    private string _errorMessage    = string.Empty;
    private string _resultSummary   = string.Empty;
    private bool   _isLoading;
    private bool   _isEuropeanFormat = true; // European is the sensible default

    // ── Constructor ───────────────────────────────────────────────────────────

    public MainViewModel(
        IDialogService       dialogService,
        ICsvParserService    csvParser,
        IEmployeePairService pairService)
    {
        _dialogService = dialogService;
        _csvParser     = csvParser;
        _pairService   = pairService;

        // Wire up commands. CanExecute predicates are re-evaluated automatically
        // by WPF's CommandManager on every UI interaction.
        BrowseCommand = new RelayCommand(_ => ExecuteBrowse(), _ => !IsLoading);
        LoadCommand   = new RelayCommand(
            async _ => await ExecuteLoadAsync(),
            _        => !string.IsNullOrWhiteSpace(FilePath) && !IsLoading);
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    // Opens the file-picker dialog and sets FilePath.
    public ICommand BrowseCommand { get; }

    // Validates, parses and analyses the selected CSV file on a background thread.
    public ICommand LoadCommand { get; }

    // ── Bindable properties ───────────────────────────────────────────────────

    // Path to the selected CSV file.
    public string FilePath
    {
        get => _filePath;
        private set => SetField(ref _filePath, value);
    }

    // When non-empty, the red error panel becomes visible.
    public string ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            SetField(ref _errorMessage, value);
            OnPropertyChanged(nameof(HasError));
        }
    }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    // Non-fatal per-row issues (bad dates, skipped rows). Bound to the amber panel.
    public ObservableCollection<string> Warnings { get; } = [];

    public bool HasWarnings => Warnings.Count > 0;

    // Compact label shown above the Expander when warnings are present.
    public string WarningsSummary
        => Warnings.Count == 1
            ? "⚠  1 warning — click to expand"
            : $"⚠  {Warnings.Count} warnings — click to expand";

    // Human-readable sentence describing the winning pair(s), shown above the DataGrid.
    public string ResultSummary
    {
        get => _resultSummary;
        private set
        {
            SetField(ref _resultSummary, value);
            OnPropertyChanged(nameof(HasResults));
            OnPropertyChanged(nameof(ShowEmptyState));
        }
    }

    public bool HasResults => PairResults.Count > 0;

    // Drives the "Load a file to see results" placeholder text.
    // Hidden once a summary is set (even "no pair found") or results are present.
    public bool ShowEmptyState => !HasResults && !IsLoading && string.IsNullOrEmpty(ResultSummary);

    public ObservableCollection<EmployeePairResult> PairResults { get; } = [];

    // True while background I/O runs — disables buttons and shows the progress bar.
    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            SetField(ref _isLoading, value);
            OnPropertyChanged(nameof(ShowEmptyState));
        }
    }

    // European = DD/MM (default), American = MM/DD. Controls ambiguous-date resolution.
    public bool IsEuropeanFormat
    {
        get => _isEuropeanFormat;
        set
        {
            if (SetField(ref _isEuropeanFormat, value))
                // Keep the American RadioButton in sync since it is the logical inverse
                OnPropertyChanged(nameof(IsAmericanFormat));
        }
    }

    // Logical inverse of IsEuropeanFormat; the RadioButtons are kept in sync via this property.
    public bool IsAmericanFormat
    {
        get => !_isEuropeanFormat;
        set => IsEuropeanFormat = !value;
    }

    // ── Command handlers ──────────────────────────────────────────────────────

    private void ExecuteBrowse()
    {
        var path = _dialogService.OpenFile("CSV Files|*.csv|Text Files|*.txt|All Files|*.*");
        if (path is null) return;

        FilePath = path;

        // Clear previous results when a new file is chosen so stale data
        // is never shown alongside a different file's name
        ClearResults();

        Console.WriteLine($"[INFO] File selected: {path}");
    }

    // Async load handler — heavy work runs on Task.Run, then result populates UI-thread properties.
    private async Task ExecuteLoadAsync()
    {
        IsLoading = true;
        ClearResults();

        Console.WriteLine($"[INFO] Loading file: {FilePath}");
        Console.WriteLine($"[INFO] Date preference: {(IsEuropeanFormat ? "European (DD/MM)" : "American (MM/DD)")}");

        try
        {
            var preference = IsEuropeanFormat
                ? DateFormatPreference.European
                : DateFormatPreference.American;

            // Run CPU/IO-bound work off the UI thread
            var (parseResult, pairResults, summary, fatalError) = await Task.Run(() =>
            {
                // ── Parse CSV ─────────────────────────────────────────────────
                var result = _csvParser.Parse(FilePath, preference);

                // A file-level error is embedded as the first warning by CsvParserService
                // when validation fails; we surface it as a fatal error here.
                if (result.Records.Count == 0 && result.Warnings.Count > 0
                    && result.Warnings[0].StartsWith("[FILE ERROR]"))
                {
                    var errorText = result.Warnings[0].Replace("[FILE ERROR] ", string.Empty);
                    return (result, (IReadOnlyList<EmployeePairResult>)[], string.Empty, errorText);
                }

                // ── Find longest pair ─────────────────────────────────────────
                var pairs = _pairService.FindLongestCollaboratingPair(result.Records);

                // ── Build summary text ────────────────────────────────────────
                string summaryText = BuildSummary(result.Records.Count, pairs);

                return (result, pairs, summaryText, (string?)null);
            });

            // ── Back on UI thread: update all bound properties ────────────────

            if (fatalError is not null)
            {
                ErrorMessage = fatalError;
                Console.WriteLine($"[ERROR] {fatalError}");
                return;
            }

            // Populate warnings panel
            foreach (var warning in parseResult.Warnings)
                Warnings.Add(warning);

            OnPropertyChanged(nameof(HasWarnings));
            OnPropertyChanged(nameof(WarningsSummary));

            if (Warnings.Count > 0)
                Console.WriteLine($"[INFO] {Warnings.Count} warning(s) encountered during parse.");

            // Populate DataGrid
            foreach (var row in pairResults)
                PairResults.Add(row);

            ResultSummary = summary;
            OnPropertyChanged(nameof(HasResults));
            OnPropertyChanged(nameof(ShowEmptyState));

            Console.WriteLine($"[INFO] Load complete. {parseResult.Records.Count} records, " +
                              $"{pairResults.Count} result rows.");
        }
        catch (Exception ex)
        {
            // Catch-all for unexpected failures — displayed in the red error panel
            // and written to the Debug / Console output for developer inspection.
            ErrorMessage = $"Unexpected error: {ex.Message}";
            Console.WriteLine($"[ERROR] Unhandled exception during load: {ex}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    // Resets all result-related state before a fresh load.
    private void ClearResults()
    {
        ErrorMessage = string.Empty;
        Warnings.Clear();
        PairResults.Clear();
        ResultSummary = string.Empty;
        OnPropertyChanged(nameof(HasWarnings));
        OnPropertyChanged(nameof(WarningsSummary));
        OnPropertyChanged(nameof(HasResults));
        OnPropertyChanged(nameof(ShowEmptyState));
    }

    // Builds the human-readable result summary (no pair / single pair / tie).
    private static string BuildSummary(int totalRecords, IReadOnlyList<EmployeePairResult> pairs)
    {
        if (pairs.Count == 0)
            return totalRecords < 2
                ? "No employee pairs found — the file contains fewer than two distinct employees."
                : "No overlapping work periods found between any employee pair.";

        // All rows share the same pair(s) — group by pair to build the summary
        var groupedByPair = pairs
            .GroupBy(r => (r.EmpId1, r.EmpId2))
            .ToList();

        int totalDays        = pairs.GroupBy(r => (r.EmpId1, r.EmpId2))
                                    .First().Sum(r => r.DaysWorked);
        int sharedProjects   = pairs.GroupBy(r => (r.EmpId1, r.EmpId2))
                                    .First().Count();
        bool multipleTied    = groupedByPair.Count > 1;

        if (!multipleTied)
        {
            var p = groupedByPair[0].Key;
            string projects = sharedProjects == 1 ? "1 shared project" : $"{sharedProjects} shared projects";
            return $"Longest collaboration: Employee {p.EmpId1} & Employee {p.EmpId2} " +
                   $"— {totalDays} total day{(totalDays == 1 ? "" : "s")} across {projects}.";
        }
        else
        {
            // Multiple pairs tied for the maximum
            var pairDescriptions = string.Join(", ",
                groupedByPair.Select(g => $"({g.Key.EmpId1} & {g.Key.EmpId2})"));
            return $"Tied result: {groupedByPair.Count} pairs share the maximum of " +
                   $"{totalDays} total day{(totalDays == 1 ? "" : "s")} — {pairDescriptions}.";
        }
    }
}
