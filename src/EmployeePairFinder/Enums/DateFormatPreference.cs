namespace EmployeePairFinder.Enums;

// Controls how ambiguous dates (both parts ≤12) are resolved.
// Dot-separated dates are always European regardless of this setting.
public enum DateFormatPreference
{
    European,   // DD/MM — default; day-first, standard outside North America
    American    // MM/DD — month-first, standard in North America
}
