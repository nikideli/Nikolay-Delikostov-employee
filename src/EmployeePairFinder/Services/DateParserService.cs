using System.Globalization;
using EmployeePairFinder.Enums;

namespace EmployeePairFinder.Services;

// Parses date strings from CSV cells. Tries formats in priority order:
//   1. NULL keywords  → null (caller treats as today)
//   2. ISO 8601       → yyyy-MM-dd, yyyy/MM/dd
//   3. Named month    → "15 Nov 2023", "November 15 2023", "01-Sep-2020" (en-US / en-GB)
//   4. Separator-based → dot always European; slash/dash resolved via preference
public sealed class DateParserService : IDateParserService
{
    // NULL-equivalent keywords in DateTo mean the assignment is still active.
    private static readonly HashSet<string> NullKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "null", "n/a", "na", "none", "today", "-", ""
    };

    // ISO formats — 4-digit year in an unambiguous position, tried with InvariantCulture.
    private static readonly string[] IsoFormats =
    [
        "yyyy-MM-dd",
        "yyyy/MM/dd"
    ];

    // Named-month formats — unambiguous because the month is a word.
    private static readonly (string Format, CultureInfo Culture)[] NamedMonthFormats =
    [
        ("d MMM yyyy",    CultureInfo.GetCultureInfo("en-GB")),
        ("d MMM yyyy",    CultureInfo.GetCultureInfo("en-US")),
        ("d MMMM yyyy",   CultureInfo.GetCultureInfo("en-GB")),
        ("d MMMM yyyy",   CultureInfo.GetCultureInfo("en-US")),
        ("d-MMM-yyyy",    CultureInfo.GetCultureInfo("en-GB")),
        ("d-MMM-yyyy",    CultureInfo.GetCultureInfo("en-US")),
        ("MMMM d yyyy",   CultureInfo.GetCultureInfo("en-US")),
        ("MMMM d, yyyy",  CultureInfo.GetCultureInfo("en-US")),
    ];

    /// <inheritdoc/>
    public DateTime? Parse(string raw, DateFormatPreference preference, out string? ambiguityWarning)
    {
        ambiguityWarning = null;
        var trimmed = raw.Trim();

        if (NullKeywords.Contains(trimmed))
            return null;

        // ISO formats first — always unambiguous
        foreach (var fmt in IsoFormats)
        {
            if (DateTime.TryParseExact(trimmed, fmt, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var isoDate))
                return isoDate;
        }

        // Named-month formats — unambiguous because the month is a word
        foreach (var (fmt, culture) in NamedMonthFormats)
        {
            if (DateTime.TryParseExact(trimmed, fmt, culture,
                    DateTimeStyles.AllowWhiteSpaces, out var namedDate))
                return namedDate;
        }

        // Separator-based: dot = always European; slash/dash resolved by preference
        return ParseSeparatorDate(trimmed, preference, out ambiguityWarning);
    }

    // Handles dd.MM.yyyy (dot → always European), dd/MM/yyyy vs MM/dd/yyyy,
    // and dd-MM-yyyy vs MM-dd-yyyy. Ambiguous values apply the preference and emit a warning.
    private static DateTime? ParseSeparatorDate(
        string value,
        DateFormatPreference preference,
        out string? ambiguityWarning)
    {
        ambiguityWarning = null;

        char sep = DetectSeparator(value);
        if (sep == '\0')
            throw new FormatException($"Unrecognised date format: \"{value}\".");

        var parts = value.Split(sep);
        if (parts.Length != 3)
            throw new FormatException($"Unrecognised date format: \"{value}\".");

        // Year-first variant: e.g. 2023.11.15
        if (parts[0].Length == 4 && int.TryParse(parts[0], out int yearFirst))
        {
            if (int.TryParse(parts[1], out int mF) && int.TryParse(parts[2], out int dF))
                return BuildDate(yearFirst, mF, dF, value);
            throw new FormatException($"Unrecognised date format: \"{value}\".");
        }

        if (!int.TryParse(parts[0], out int a) || !int.TryParse(parts[1], out int b))
            throw new FormatException($"Unrecognised date format: \"{value}\".");

        if (!int.TryParse(parts[2], out int yearRaw))
            throw new FormatException($"Unrecognised date format: \"{value}\".");

        // Normalise 2-digit years: 00-49 → 2000-2049, 50-99 → 1950-1999
        int year = yearRaw < 100 ? (yearRaw >= 50 ? 1900 + yearRaw : 2000 + yearRaw) : yearRaw;

        // Dot separator is always DD.MM — no ambiguity possible
        if (sep == '.')
            return BuildDate(year, month: b, day: a, raw: value);

        // Unambiguous: one part exceeds 12, forcing the layout
        if (a > 12) return BuildDate(year, month: b, day: a, raw: value);
        if (b > 12) return BuildDate(year, month: a, day: b, raw: value);

        // Genuinely ambiguous — apply preference and warn
        ambiguityWarning =
            $"Ambiguous date \"{value}\": interpreted as " +
            (preference == DateFormatPreference.European
                ? $"DD{sep}MM (day={a}, month={b})"
                : $"MM{sep}DD (month={a}, day={b})") +
            " based on the selected preference.";

        return preference == DateFormatPreference.European
            ? BuildDate(year, month: b, day: a, raw: value)
            : BuildDate(year, month: a, day: b, raw: value);
    }

    // Returns the first separator found (. / -), or '\0' if none.
    private static char DetectSeparator(string value)
    {
        foreach (char c in new[] { '.', '/', '-' })
            if (value.Contains(c)) return c;
        return '\0';
    }

    // Wraps DateTime constructor and converts ArgumentOutOfRangeException to FormatException
    private static DateTime BuildDate(int year, int month, int day, string raw)
    {
        try
        {
            return new DateTime(year, month, day);
        }
        catch (ArgumentOutOfRangeException)
        {
            throw new FormatException(
                $"Date \"{raw}\" resolved to an invalid calendar date " +
                $"(year={year}, month={month}, day={day}). " +
                $"Check the value or switch the date format preference.");
        }
    }
}
