using Logiwall.Shared.Attributes;

namespace Logiwall.UnitTests.Attributes;

public class NationalCodeAttributeTests
{
    private static readonly NationalCodeAttribute Attribute = new();

    [Theory]
    [InlineData("1234567891")] // remainder 1: check digit equals the remainder
    [InlineData("0013542419")] // remainder 2: check digit is 11 - 2
    [InlineData(null)]
    [InlineData("")]
    [InlineData("12345678901")] // an 11-digit legal-entity ID: digits only
    public void Valid_codes_pass(string? code) => Assert.True(Attribute.IsValid(code));

    [Theory]
    [InlineData("1234567890")] // wrong check digit (E2E 2026-11-07)
    [InlineData("1111111111")] // all one digit
    [InlineData("123456789")]
    [InlineData("12345678a1")]
    [InlineData("۱۲۳۴۵۶۷۸۹۱")] // Persian digits: the front converts them before it sends
    [InlineData("1234567890123")]
    public void Invalid_codes_fail(string code) => Assert.False(Attribute.IsValid(code));
}
