# Employee Pair Finder

A WPF desktop application built with .NET 8 that identifies the pair of employees who have worked together on common projects for the longest total period of time.

---

## The Problem

Given a CSV file containing employee project assignments in the format:

```
EmpID, ProjectID, DateFrom, DateTo
```

Find the pair of employees who collectively accumulated the most calendar days working simultaneously on shared projects.

**Key rules:**
- `DateTo` may be `NULL`, meaning the assignment is still active (treated as today)
- Days are counted inclusively — both the start and end day count
- Total days are summed across all projects the pair shared

---

## Architecture

The application follows **MVVM** (Model-View-ViewModel) and **SOLID** principles throughout:

```
src/EmployeePairFinder/
├── Enums/
│   └── DateFormatPreference.cs      # European or American date interpretation
├── Models/
│   ├── EmployeeRecord.cs            # One CSV row (EmpID, ProjectID, DateFrom, DateTo)
│   ├── EmployeePairResult.cs        # One result row (EmpID1, EmpID2, ProjectID, DaysWorked)
│   └── CsvParseResult.cs            # Parse output: records + warnings
├── Services/
│   ├── IDateParserService.cs        # Interface for date string → DateTime conversion
│   ├── DateParserService.cs         # Resolves 8 date format families; handles ambiguous dates
│   ├── IFileValidatorService.cs     # Interface for pre-parse file validation
│   ├── FileValidatorService.cs      # Checks existence, readability, header detection
│   ├── ICsvParserService.cs         # Interface for CSV → EmployeeRecord[] conversion
│   ├── CsvParserService.cs          # Row-by-row parser with per-row error handling
│   ├── IEmployeePairService.cs      # Interface for the core pair-finding algorithm
│   └── EmployeePairService.cs       # Core algorithm implementation
├── Infrastructure/
│   ├── IDialogService.cs            # File picker abstraction (keeps ViewModel testable)
│   └── DialogService.cs             # Wraps Microsoft.Win32.OpenFileDialog
├── ViewModels/
│   ├── ViewModelBase.cs             # INotifyPropertyChanged + SetField<T> helper
│   ├── RelayCommand.cs              # ICommand implementation (hooks CommandManager)
│   └── MainViewModel.cs             # All UI state, commands, async load orchestration
├── Converters/
│   └── StringToVisibilityConverter.cs  # Shows/hides panels based on non-empty string
├── App.xaml / App.xaml.cs           # DI container composition root
└── MainWindow.xaml / MainWindow.xaml.cs  # View — pure XAML bindings, no business logic
```

## Running the Application

1. Open Nikolay-Delikostov-employees.sln in Visual Studio.
2. Ensure EmployeePairFinder is set as the "Startup Project" (right-click → Set as Startup Project).
3. Press "F5" or "Ctrl+F5".
4. The application window opens. Click "Browse CSV" to select a file, optionally adjust the date format toggle, then click "Load Data".

---

## Test Data Files

CSV files in the `TestData/` folder cover some scenarios:

| File | Scenario |
|------|---------|
| `01_basic_scenario.csv` | Simple baseline: employees on separate date ranges — no overlap |
| `02_null_dateto.csv` | `NULL` DateTo values — resolved to today |
| `03_multiple_pairs.csv` | Multiple employee pairs across multiple projects; algorithm picks the winner |
| `04_with_headers.csv` | File has a `EmpID, ProjectID, DateFrom, DateTo` header row — auto-detected and skipped |
| `05_mixed_date_formats.csv` | ISO, European dot, and named-month formats in the same file |
| `06_overlapping_periods.csv` | Partial date range overlaps across multiple employees and projects |
| `07_single_employee.csv` | Only one distinct employee in the file — no pair can be found |
| `08_invalid_data.csv` | Bad dates, non-numeric IDs, missing columns — warnings shown, valid rows still processed |
| `09_ambiguous_dates.csv` | Dates like `03/05/2022` — European/American toggle changes the interpretation |
| `10_large_dataset.csv` | 67 rows, 20 employees, 16 projects |
| `11_edge_case_same_day.csv` | `DateFrom` equals `DateTo` — result must show `1` day (inclusive counting) |
| `12_empty_file.csv` | Empty file — red error panel displayed, no crash |
| `13_large_many_pairs.csv` | 786 rows, 60 employees, 30 projects — many result rows in the DataGrid |

---

## Sample Input / Output

**Input (`03_multiple_pairs.csv` excerpt):**
```
10, 100, 2015-01-01, 2015-12-31
20, 100, 2015-03-01, 2015-09-30
10, 300, 2017-01-01, 2018-12-31
20, 300, 2017-01-01, 2018-12-31
```

**Output (DataGrid):**

| Employee ID #1 | Employee ID #2 | Project ID | Days Worked Together |
|----------------|----------------|------------|----------------------|
|       10       |       20       |     100    |          214         |
| 10 | 20 | 300 | 730 |

*Winner: Employees 10 & 20 — 1126 total days across 3 shared projects.*
