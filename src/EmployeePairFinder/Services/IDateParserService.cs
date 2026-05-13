using EmployeePairFinder.Enums;

namespace EmployeePairFinder.Services;

public interface IDateParserService
{
    // Returns null for NULL-equivalent values (caller treats as today).
    // Throws FormatException when the value cannot be matched against any supported format.
    DateTime? Parse(string raw, DateFormatPreference preference, out string? ambiguityWarning);
}
