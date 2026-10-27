namespace Logiwall.Application.Abstractions.Tools;
public interface ISMSService
{
    /// <returns>"OK" on success, otherwise a provider error description.</returns>
    Task<string> SendSmsAsync(string message, string destination, CancellationToken cancellationToken = default);
}
