using static Logiwall.Shared.Enums.Constants;

namespace Logiwall.Shared.Dtos.Global;
public class GetStaticDataRequestDto
{
    public StaticDataType Type { get; set; }

    public string? SearchBy { get; set; }
}
