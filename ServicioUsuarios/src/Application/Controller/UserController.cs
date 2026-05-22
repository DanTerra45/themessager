using Application.Options;
using Domain.Dto.Register;
using Infrastructure.Repository;
using Application.Service;
using Microsoft.AspNetCore.Mvc;
using Application.Common;
using Application.UseCases;
using Microsoft.AspNetCore.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Domain.Common;

namespace Application.Controller;

[ApiController]
[Route("api/users")]
public class UserController : ControllerBase
{
	private readonly UserRepository _userRepository;
	private readonly UserService _userService;
	private readonly RegisterUserUseCase _registerUserUseCase;

	public UserController(UserRepository userRepository, UserService userService, RegisterUserUseCase registerUserUseCase)
	{
		_userRepository = userRepository;
		_userService = userService;
		_registerUserUseCase = registerUserUseCase;	
	}

	[HttpGet("all")]
	public async Task<IActionResult> GetAll(
		[FromQuery] int? limit = null,
		[FromQuery] int? offset = null,
		[FromQuery] UserFields? orderBy = null,
		[FromQuery] bool orderDescending = false)
	{
		var options = new UserOptions()
						.SelectFields([
							UserFields.Id,
							UserFields.Username,
							UserFields.Email,
							UserFields.NeedPasswordChange,
							UserFields.Role,
							UserFields.State])
						.SetPagination(limit ?? 10, offset ?? 0)
						.SetOrdering(orderBy ?? UserFields.Id, orderDescending);
		var result = await _userService.GetAllAsync(options);
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
		var result = await _registerUserUseCase.Execute(dto, int.Parse(sub));
		return this.ToActionResult(result, StatusCodes.Status201Created);
	}
}
