using static Logiwall.Shared.Enums.Constants;

namespace Logiwall.Shared.Dtos.Global;
public record GetStaticDataRequestDto
{
    public StaticDataType Type { get; set; }

    public string? SearchBy { get; set; }
}
