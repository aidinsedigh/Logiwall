namespace Logiwall.Application.Abstractions.Tools;
public interface ISMSService
{
    string SendSMS(string Message, string Destination);
}

