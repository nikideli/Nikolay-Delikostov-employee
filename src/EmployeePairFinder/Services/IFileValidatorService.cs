namespace EmployeePairFinder.Services;

// Carries the outcome of file validation: pass/fail, an optional error message,
// and whether the first row is a column-header that should be skipped.
public sealed class FileValidationResult
{
    public bool IsValid { get; init; }
    public string? ErrorMessage { get; init; }
    public bool HasHeader { get; init; }

    public static FileValidationResult Ok(bool hasHeader = false)
        => new() { IsValid = true, HasHeader = hasHeader };

    public static FileValidationResult Fail(string error)
        => new() { IsValid = false, ErrorMessage = error };
}

public interface IFileValidatorService
{
    // Validates existence, readability, content, and basic column structure.
    FileValidationResult Validate(string filePath);
}
