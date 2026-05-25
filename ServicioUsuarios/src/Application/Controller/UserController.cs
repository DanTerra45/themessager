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

namespace Application.Controller;

[ApiController]
[Route("api/users")]
public class UserController : ControllerBase
{
    private readonly UserService _userService;
    private readonly RegisterUserUseCase _registerUserUseCase;
    private readonly AssignTemporaryPasswordUseCase _assignTemporaryPasswordUseCase;

    public UserController(
        UserService userService,
        RegisterUserUseCase registerUserUseCase,
        AssignTemporaryPasswordUseCase assignTemporaryPasswordUseCase)
    {
        _userService = userService;
        _registerUserUseCase = registerUserUseCase;
        _assignTemporaryPasswordUseCase = assignTemporaryPasswordUseCase;
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

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var options = new UserOptions();
        options.AddFilter(UserFields.Id, FilterOperator.Equals, id);
        options.SelectFields(new[] { UserFields.State });

        var result = await _userService.DeleteAsync(options);
        return this.ToActionResult(result);
    }
}