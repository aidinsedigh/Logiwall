using Logiwall.Shared.Dtos.Global;
using Logiwall.Shared.Dtos.Identity;

namespace Logiwall.Application.Abstractions.Identity
{
    public interface IAuthenticationService
    {
        Task<ServiceResponse<bool>> SendOtpCodeAsync(SendOtpRequestDto requestDto);

        Task<ServiceResponse<ConfirmationOtpResponseDto>> ConfirmationOtpCodeAsync(ConfirmaionOtpRequestDto requestDto);

        Task LogUserLoginAsync(LogUserLoginRequestDto requestDto);
    }
}
