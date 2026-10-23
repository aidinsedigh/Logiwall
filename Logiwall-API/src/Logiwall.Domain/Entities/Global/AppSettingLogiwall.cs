namespace Logiwall.Domain.Entities.Global;
public class AppSettingLogiwall
{
    public int Id { get; set; }

    [Required]
    public string? KeyName { get; set; }

    public string? Value { get; set; }
}
