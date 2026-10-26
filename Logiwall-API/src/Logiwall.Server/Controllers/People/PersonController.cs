using Logiwall.Application.Abstractions.People;
using Logiwall.Domain.Entities.Identity;
using Logiwall.Shared.Dtos.Global;
using Logiwall.Shared.Dtos.People;

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

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, token, requestDto.Password!);
        if (!result.Succeeded)
        {
            return BadRequest(new ServiceResponse<bool>
            {
                Message = AppStrings.AnErrorOccurrdWhileRegisteration
            });
        }

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
                Message = "UserId is not correct"
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
        var result = await customerService.GetCustomerContactByIdAsync(id);

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
        var result = await customerService.UpdateCustomerContactAsync(requestDto);

        if (!result.Succuss)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return BadRequest(ModelState);
        }
        return Ok(result);
    }
}
