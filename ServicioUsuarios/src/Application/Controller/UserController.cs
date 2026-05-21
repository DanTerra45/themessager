using Application.Options;
using Domain.Common;
using Domain.Database;
using Domain.Dto.Register;
using Domain.Entities.Enums;
using Infrastructure.Database;
using Infrastructure.Repository;
using Infrastructure.Service;
using Microsoft.AspNetCore.Mvc;
using Application.Common;

namespace Application.Controller;

[ApiController]
[Route("api/users/test")]
public class UserController : ControllerBase
{
	private readonly UserRepository _userRepository;
	private readonly UserService _userService;

	public UserController(UserRepository userRepository, UserService userService)
	{
		_userRepository = userRepository;
		_userService = userService;
	}

	[HttpGet("all")]
	public async Task<IActionResult> GetAll(
		[FromQuery] bool preview = true,
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
							UserFields.State
						]);
		if (preview)
			return Ok(BuildGetAllPreview(options));
		var result = await _userService.GetAllAsync(options);
		return this.ToActionResult(result);
	}

	[HttpGet("{id:int}")]
	public async Task<IActionResult> GetById(
		int id,
		[FromQuery] bool preview = true,
		[FromQuery] UserFields[]? fields = null)
	{
		var options = CreateOptions(null, null, null, false, fields);

		if (preview)
			return Ok(BuildGetByIdPreview(id, options));

		return Ok(await _userRepository.GetByIdAsync(id, options));
	}

	[HttpGet("by-username/{username}")]
	public async Task<IActionResult> GetByUsername(string username, [FromQuery] bool preview = true)
	{
		if (preview)
			return Ok(BuildSimplePreview("GetByUsername", "username", username, UserFields.Username));

		return StatusCode(StatusCodes.Status501NotImplemented, "GetByUsername todavía no está implementado en UserRepository.");
	}

	[HttpGet("by-email/{email}")]
	public async Task<IActionResult> GetByEmail(string email, [FromQuery] bool preview = true)
	{
		if (preview)
			return Ok(BuildSimplePreview("GetByEmail", "email", email, UserFields.Email));

		return StatusCode(StatusCodes.Status501NotImplemented, "GetByEmail todavía no está implementado en UserRepository.");
	}

	[HttpPost]
	public async Task<IActionResult> Create([FromBody] CreateUserDto request, [FromQuery] bool preview = true)
	{
		if (preview)
			return Ok(BuildCreatePreview(request));

		return Ok(await _userRepository.CreateAsync(request, null));
	}

	// [HttpPatch("{id:int}/state")]
	// public async Task<IActionResult> ChangeState(
	// 	int id,
	// 	[FromQuery] UserState newState,
	// 	[FromQuery] bool preview = true)
	// {
	// 	if (preview)
	// 		return Ok(BuildChangeStatePreview(id, newState));

	// 	return Ok(await _userRepository.ChangeStateAsync(id, newState));
	// }

	private static UserOptions CreateOptions(
		int? limit,
		int? offset,
		UserFields? orderBy,
		bool orderDescending,
		UserFields[]? fields)
	{
		var options = new UserOptions();

		if (limit is not null)
			options.SetPagination(limit.Value, offset ?? 0);

		if (orderBy is not null)
			options.SetOrdering(orderBy.Value, orderDescending);

		if (fields is not null && fields.Length > 0)
			options.SelectedFields = fields;

		return options;
	}

	private static object BuildGetAllPreview(UserOptions options)
	{
		var effective = ApplySoftDeleteFilter(options);
		var builder = new QueryBuilder<UserOptions, UserFields>("users", new UserSchema());
		builder.BuildFromOptions(effective);
		var (sql, parameters) = builder.Build();

		return new SqlPreview(
			"GetAll",
			sql,
			ExtractParameters(parameters));
	}

	private static object BuildGetByIdPreview(int id, UserOptions options)
	{
		var effective = ApplySoftDeleteFilter(options);
		effective.AddFilter(UserFields.Id, FilterOperator.Equals, id);

		var builder = new QueryBuilder<UserOptions, UserFields>("users", new UserSchema());
		builder.BuildFromOptions(effective);
		var (sql, parameters) = builder.Build();

		return new SqlPreview(
			"GetById",
			sql,
			ExtractParameters(parameters));
	}

	private static object BuildSimplePreview(string operation, string fieldName, object value, UserFields field)
	{
		var options = ApplySoftDeleteFilter(new UserOptions());
		options.AddFilter(field, FilterOperator.Equals, value);

		var builder = new QueryBuilder<UserOptions, UserFields>("users", new UserSchema());
		builder.BuildFromOptions(options);
		var (sql, parameters) = builder.Build();

		return new SqlPreview(
			operation,
			sql,
			ExtractParameters(parameters));
	}

	private static object BuildRolesPreview(IEnumerable<UserRole> roles, UserOptions options)
	{
		var effective = ApplySoftDeleteFilter(options);
		effective.AddFilter(UserFields.Role, FilterOperator.Contains, roles.ToList());

		var builder = new QueryBuilder<UserOptions, UserFields>("users", new UserSchema());
		builder.BuildFromOptions(effective);
		var (sql, parameters) = builder.Build();

		return new SqlPreview(
			"GetByRoles",
			sql,
			ExtractParameters(parameters));
	}

	private static object BuildCreatePreview(CreateUserDto request)
	{
		var props = typeof(CreateUserDto).GetProperties();
		var columns = props.Select(p => p.Name.ToLowerInvariant()).ToList();
		var parameters = props.Select((_, index) => $"@ins{index}").ToList();

		var sql = $"INSERT INTO users ({string.Join(", ", columns)}) VALUES ({string.Join(", ", parameters)})";
		var paramValues = props.Zip(parameters, (prop, name) => new SqlParameter(name, prop.GetValue(request))).ToList();

		return new SqlPreview("Create", sql, paramValues);
	}

	private static object BuildChangeStatePreview(int id, UserState newState)
	{
		var sql = "UPDATE users SET state = @state WHERE id = @id AND state <> @deleted";

		return new SqlPreview(
			"ChangeState",
			sql,
			new[]
			{
				new SqlParameter("@state", newState),
				new SqlParameter("@id", id),
				new SqlParameter("@deleted", UserState.Deleted)
			});
	}

	private static UserOptions ApplySoftDeleteFilter(UserOptions options)
	{
		options.AddFilter(UserFields.State, FilterOperator.NotEquals, UserState.Deleted);
		return options;
	}

	private static IReadOnlyCollection<SqlParameter> ExtractParameters(Dapper.DynamicParameters parameters) =>
		parameters.ParameterNames
			.Select(name => new SqlParameter(name, parameters.Get<object>(name)!))
			.ToArray();

}



public sealed record SqlPreview(string Operation, string Sql, IReadOnlyCollection<SqlParameter> Parameters);

public sealed record SqlParameter(string Name, object? Value);