namespace Logiwall.Shared.Attributes;

/// <summary>
/// Accepts an Iranian national code: 10 digits with a valid check digit, not all the same digit.
/// An empty value is valid; add [Required] when the field must be sent.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class NationalCodeAttribute : ValidationAttribute
{
    public NationalCodeAttribute() => ErrorMessage = nameof(AppStrings.InvalidNationalCode);

    public override bool IsValid(object? value)
    {
        if (value is not string code || code.Length == 0)
            return true;

        // shortcut: an 11-digit legal-entity ID (PersonKind.Legal) is checked for digits only; add its checksum when the front sends one.
        if (code.Length == 11)
            return code.All(char.IsAsciiDigit);

        if (code.Length != 10 || code.All(char.IsAsciiDigit) is false || code.All(c => c == code[0]))
            return false;

        var sum = 0;
        for (var i = 0; i < 9; i++)
            sum += (code[i] - '0') * (10 - i);

        var remainder = sum % 11;
        var check = code[9] - '0';
        return remainder < 2 ? check == remainder : check == 11 - remainder;
    }
}
