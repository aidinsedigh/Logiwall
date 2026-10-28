namespace Logiwall.Shared.Dtos.Providers.Sep724;
public record PaymentTokenRequestDto
{
    public string? OrderNumber { get; set; }

    public decimal Amount { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Channel { get; set; }

    public string? CallbackUrl { get; set; }

}
