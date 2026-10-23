using static Logiwall.Shared.Enums.Constants;

namespace Logiwall.Domain.Entities.Global;
public class StaticData
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string? Title { get; set; }

    public StaticDataType Type { get; set; }
}
