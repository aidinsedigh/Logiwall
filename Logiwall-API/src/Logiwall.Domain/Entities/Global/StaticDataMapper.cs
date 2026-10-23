namespace Logiwall.Domain.Entities.Global;
public class StaticDataMapper
{
    public int Id { get; set; }

    [Required, StringLength(20)]
    public string? Code { get; set; }

    public ProviderType ProviderType { get; set; }

    public int StaticDataId { get; set; }

    public StaticData? StaticData { get; set; }
}
