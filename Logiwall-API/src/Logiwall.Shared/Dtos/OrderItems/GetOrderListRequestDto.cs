using Logiwall.Shared.Dtos.Global;

namespace Logiwall.Shared.Dtos.OrderItems;
public class GetOrderListRequestDto
{
    public int UserId { get; set; }

    public GetListQueryParams QueryParams { get; set; }
}
