using System.Text.RegularExpressions;

namespace CarMarketplace.Application.Security;

/// <summary>
/// Central password policy used by registration, reset, and authenticated changes.
/// </summary>
public static class PasswordPolicy
{
    public const int MinLength = 12;

    public const string ComplexityMessage =
        "Password must include at least one uppercase letter, one lowercase letter, one number, and one special character.";

    private const string ComplexityPattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).+$";

    public static bool MeetsComplexity(string? password)
    {
        return !string.IsNullOrWhiteSpace(password) && Regex.IsMatch(password, ComplexityPattern);
    }

    public static IReadOnlyList<string> Validate(string? password)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(password))
        {
            errors.Add("Password is required.");
            return errors;
        }

        if (password.Length < MinLength)
        {
            errors.Add($"Password must be at least {MinLength} characters.");
        }

        if (!MeetsComplexity(password))
        {
            errors.Add(ComplexityMessage);
        }

        return errors;
    }
}
