using Logiwall.Application.Abstractions.People;
using Logiwall.Domain.Entities.Identity;
using Logiwall.Shared.Dtos.Global;
using Logiwall.Shared.Dtos.People;
using Microsoft.AspNetCore.RateLimiting;

namespace Logiwall.Server.Controllers.People;

[Route("api/[controller]/[action]")]
[ApiController]
public partial class PersonController : AppControllerBase
{
    [AutoInject] private UserManager<User> userManager = default!;
    [AutoInject] private SignInManager<User> signInManager = default!;
    [AutoInject] private IPersonService personService = default!;
    [AutoInject] private ICustomerService customerService = default!;

    [HttpPost]
    public async Task<ActionResult> Register(RegisterPersonRequestDto requestDto, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var user = await userManager.Users.FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);

        if (user is null)
            throw new ResourceNotFoundException();

        // The person row and the password must be saved together; disposing without a commit rolls back.
        await using var tx = await DbContext.Database.BeginTransactionAsync(cancellationToken);

        var response = await personService.CreatePersonAsync(new CreatePersonRequestDto
        {
            Name = requestDto.Name,
            LastName = requestDto.LastName,
            UserId = userId,
            PhoneNumber = user.UserName!,
            Email = requestDto.Email,
            NationalCode = requestDto.NationalCode
        });

        if (!response.Succuss)
        {
            return BadRequest(response);
        }

        // GetUserProfile reads the e-mail from the Identity user.
        if (string.IsNullOrWhiteSpace(requestDto.Email) is false
            && (await userManager.SetEmailAsync(user, requestDto.Email)).Succeeded is false)
        {
            return BadRequest(new ServiceResponse<bool>
            {
                Succuss = false,
                Message = Localizer[nameof(AppStrings.EmailAddressAttribute_ValidationError), Localizer[nameof(AppStrings.Email)]]
            });
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, token, requestDto.Password!);
        if (!result.Succeeded)
        {
            return BadRequest(new ServiceResponse<bool>
            {
                Message = Localizer[nameof(AppStrings.AnErrorOccurrdWhileRegisteration)]
            });
        }

        await tx.CommitAsync(cancellationToken);
        return Ok(response);
    }

    [HttpGet]
    public async Task<ActionResult> GetUserProfile(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var response = await personService.GetUserProfileAsync(userId);

        if (!response.Succuss)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult> UpdateUserProfile(UserProfileDto requestDto, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (requestDto.UserId is not null && requestDto.UserId != userId)
        {
            return BadRequest(new ServiceResponse<UserProfileDto>
            {
                Data = requestDto,
                Succuss = false,
                Message = Localizer[nameof(AppStrings.InvalidUserId)]
            });
        }
        requestDto.UserId = userId;
        var response = await personService.UpdateUserProfileAsync(requestDto);
        if (!response.Succuss)
        {
            return BadRequest(response);
        }
        return Ok(response);
    }

    [HttpPost, EnableRateLimiting("auth-verify")]
    public async Task<ActionResult> ChangePassword(ChangePasswordRequestDto requestDto, CancellationToken cancellationToken)
    {
        var response = await personService.ChangePasswordAsync(User.GetUserId(), requestDto);
        if (!response.Succuss)
        {
            return BadRequest(response);
        }
        return Ok(response);
    }

    [HttpGet]
    public async Task<ActionResult> GetCustomerContactList(CancellationToken cancellationToken)
    {
        var result = await customerService.GetCustomerContactListAsync(new GetCustomerContactListRequestDto
        {
            UserId = User.GetUserId(),
        });

        if (!result.Succuss)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return BadRequest(ModelState);
        }
        return Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult> GetCustomerContact([FromQuery] int id, CancellationToken cancellationToken)
    {
        var result = await customerService.GetCustomerContactByIdAsync(id, User.GetUserId());

        if (!result.Succuss)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return BadRequest(ModelState);
        }
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult> CreateCustomerContact(CreateCustomerContactRequestDto requestDto, CancellationToken cancellationToken)
    {
        requestDto.UserId = User.GetUserId();
        var result = await customerService.CreateCustomerContactAsync(requestDto);

        if (!result.Succuss)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return BadRequest(ModelState);
        }
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult> UpdateCustomerContact(UpdateCustomerContactRequestDto requestDto, CancellationToken cancellationToken)
    {
        var result = await customerService.UpdateCustomerContactAsync(requestDto, User.GetUserId());

        if (!result.Succuss)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return BadRequest(ModelState);
        }
        return Ok(result);
    }
}
