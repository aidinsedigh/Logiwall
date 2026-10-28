using Logiwall.Shared.Dtos.Global;

namespace Logiwall.Application.Abstractions.Global;
public interface ICommonService
{
    Task<ServiceResponse<List<GetStaticDataResponseDto>>> GetStaticDatasAsync(GetStaticDataRequestDto requestDto);
}
