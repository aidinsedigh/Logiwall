using System.Net;

namespace Logiwall.Shared.Exceptions;

public sealed class CarrierRequestException(string message, int status) : RestException(message)
{
    public override HttpStatusCode StatusCode => (HttpStatusCode)status;
}
