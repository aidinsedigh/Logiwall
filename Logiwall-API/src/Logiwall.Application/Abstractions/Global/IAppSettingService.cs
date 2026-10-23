using Logiwall.Shared.Dtos.Global;

namespace Logiwall.Application.Abstractions.Global;
public interface IAppSettingService
{
    Task<AppSettingsDto> GetAllSettingsAsync();
}
