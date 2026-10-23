using Logiwall.Shared.Dtos.Global;

namespace Logiwall.Shared.Dtos.Providers.Sep724;
public class GetPaymentInfoRequestDto : IHttpRequest
{
    [JsonPropertyName("requestId")]
    public long RequestId { get; set; }

    [JsonPropertyName("referenceCode")]
    public Guid ReferenceCode { get; set; }
}
