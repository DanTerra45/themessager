using Application.Options;
using Domain.Dto.Register;
using Application.Service;
using Microsoft.AspNetCore.Mvc;
using Application.Common;
using Application.UseCases;
using Microsoft.AspNetCore.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Domain.Common;
using Domain.Database;
using Domain.Entities.Enums;
using Domain.Database.Fields;
using Domain.Dto.Auth;

namespace Application.Controller;

[ApiController]
[Route("api/users")]
public class UserController : ControllerBase
{
    private readonly UserService _userService;
    private readonly RegisterUserUseCase _registerUserUseCase;
    private readonly AssignTemporaryPasswordUseCase _assignTemporaryPasswordUseCase;
    private readonly DisableUserUseCase _disableUserUseCase;
    private readonly ForgotPasswordUseCase _forgotPasswordUseCase;

    public UserController(
        UserService userService,
        RegisterUserUseCase registerUserUseCase,
        AssignTemporaryPasswordUseCase assignTemporaryPasswordUseCase,
        DisableUserUseCase disableUserUseCase,
        ForgotPasswordUseCase forgotPasswordUseCase)
    {
        _userService = userService;
        _registerUserUseCase = registerUserUseCase;
        _assignTemporaryPasswordUseCase = assignTemporaryPasswordUseCase;
        _disableUserUseCase = disableUserUseCase;
        _forgotPasswordUseCase = forgotPasswordUseCase;
    }

    [HttpGet("all")]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? limit = null,
        [FromQuery] int? offset = null,
        [FromQuery] UserFields? orderBy = null,
        [FromQuery] bool availableOnly = true,
        [FromQuery] bool orderDescending = false)
    {
        var options = new UserOptions();
		options.SelectFields(new[]
		{
			UserFields.Id,
			UserFields.Username,
			UserFields.Email,
			UserFields.NeedPasswordChange,
			UserFields.Role,
			UserFields.State,
			UserFields.LastLogin,
			UserFields.CreatedAt
		});
		options.SetPagination(limit ?? 10, offset ?? 0);
		options.SetOrdering(orderBy ?? UserFields.Id, orderDescending);

        if (availableOnly)
        {
            options.AddFilter(UserFields.State, FilterOperator.Equals, UserState.Active);
        }

        var result = await _userService.GetAllAsync(options);
        return this.ToActionResult(result);
    }

    [HttpPatch("assign/temporary-password/{id}")]
    public async Task<IActionResult> AssignTemporaryPassword(int id)
    {
        var result = await _assignTemporaryPasswordUseCase.Execute(id);
        return this.ToActionResult(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateUserDto dto)
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!int.TryParse(sub, out var creatorId))
        {
            return this.ToActionResult(Result<int>.Failure(new AppError("400", "Invalid sub claim", ErrorType.Conflict)));
        }

        var result = await _registerUserUseCase.Execute(dto, creatorId);
        return this.ToActionResult(result, StatusCodes.Status201Created);
    }

    [AllowAnonymous]
    [HttpPut("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var result = await _forgotPasswordUseCase.Execute(request);
        return this.ToActionResult(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id,
	[FromBody] DisableUserDto disableUserDto)
    {
        
        var result = await _disableUserUseCase.ExecuteAsync(
                RegisterUserStoryDto.ToDisable(
                    id,
                    int.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)),
                    disableUserDto.Reason
                )
        );
        return this.ToActionResult(result);
    }
}