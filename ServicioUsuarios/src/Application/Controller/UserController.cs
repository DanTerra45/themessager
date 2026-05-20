using Application.Options;
using Domain.Database;
using Domain.Dto.Register;
using Domain.Entities.Enums;
using Infrastructure.Database;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Application.Controller;

[ApiController]
[Route("api/users/test")]
public class UserController : ControllerBase
{
	private readonly UserRepository _userRepository;

	public UserController(UserRepository userRepository)
	{
		_userRepository = userRepository;
	}

	[HttpGet("all")]
	public async Task<IActionResult> GetAll(
		[FromQuery] bool preview = true,
		[FromQuery] int? limit = null,
		[FromQuery] int? offset = null,
		[FromQuery] UserFields? orderBy = null,
		[FromQuery] bool orderDescending = false,
		[FromQuery] UserFields[]? fields = null)
	{
		var options = CreateOptions(limit, offset, orderBy, orderDescending, fields);

		if (preview)
			return Ok(BuildGetAllPreview(options));

		return Ok(await _userRepository.GetAllAsync(options));
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

		return Ok(await _userRepository.GetByUsernameAsync(username));
	}

	[HttpGet("by-email/{email}")]
	public async Task<IActionResult> GetByEmail(string email, [FromQuery] bool preview = true)
	{
		if (preview)
			return Ok(BuildSimplePreview("GetByEmail", "email", email, UserFields.Email));

		return Ok(await _userRepository.GetByEmailAsync(email));
	}

	[HttpGet("by-roles")]
	public async Task<IActionResult> GetByRoles(
		[FromQuery] UserRole[] roles,
		[FromQuery] bool preview = true,
		[FromQuery] int? limit = null,
		[FromQuery] int? offset = null,
		[FromQuery] UserFields? orderBy = null,
		[FromQuery] bool orderDescending = false,
		[FromQuery] UserFields[]? fields = null)
	{
		var options = CreateOptions(limit, offset, orderBy, orderDescending, fields);

		if (preview)
			return Ok(BuildRolesPreview(roles, options));

		return Ok(await _userRepository.GetByRolesAsync(roles, options));
	}

	[HttpPost]
	public async Task<IActionResult> Create([FromBody] CreateUserDto request, [FromQuery] bool preview = true)
	{
		if (preview)
			return Ok(BuildCreatePreview(request));

		return Ok(await _userRepository.CreateAsync(request, null));
	}

	[HttpPatch("{id:int}/state")]
	public async Task<IActionResult> ChangeState(
		int id,
		[FromQuery] UserState newState,
		[FromQuery] bool preview = true)
	{
		if (preview)
			return Ok(BuildChangeStatePreview(id, newState));

		return Ok(await _userRepository.ChangeStateAsync(id, newState));
	}

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
		var builder = new QueryBuilder<UserFields>();

		builder.Select(ResolveFields(effective), "users");
		builder.Where(effective, "users");
		ApplyOrderAndPaging(builder, effective);

		return new SqlPreview(
			"GetAll",
			builder.Sql.ToString(),
			ExtractParameters(builder));
	}

	private static object BuildGetByIdPreview(int id, UserOptions options)
	{
		var effective = ApplySoftDeleteFilter(options);
		effective.AddFilter(UserFields.Id, FilterOperator.Equals, id);

		var builder = new QueryBuilder<UserFields>();
		builder.Select(ResolveFields(effective), "users");
		builder.Where(effective, "users");

		return new SqlPreview(
			"GetById",
			builder.Sql.ToString(),
			ExtractParameters(builder));
	}

	private static object BuildSimplePreview(string operation, string fieldName, object value, UserFields field)
	{
		var options = ApplySoftDeleteFilter(new UserOptions());
		options.AddFilter(field, FilterOperator.Equals, value);

		var builder = new QueryBuilder<UserFields>();
		builder.Select(Enum.GetValues(typeof(UserFields)).Cast<UserFields>(), "users");
		builder.Where(options, "users");

		return new SqlPreview(
			operation,
			builder.Sql.ToString(),
			ExtractParameters(builder));
	}

	private static object BuildRolesPreview(IEnumerable<UserRole> roles, UserOptions options)
	{
		var effective = ApplySoftDeleteFilter(options);
		effective.AddFilter(UserFields.Role, FilterOperator.Contains, roles.ToList());

		var builder = new QueryBuilder<UserFields>();
		builder.Select(ResolveFields(effective), "users");
		builder.Where(effective, "users");
		ApplyOrderAndPaging(builder, effective);

		return new SqlPreview(
			"GetByRoles",
			builder.Sql.ToString(),
			ExtractParameters(builder));
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

	private static IEnumerable<UserFields> ResolveFields(UserOptions options) =>
		options.SelectedFields.Any()
			? options.SelectedFields
			: Enum.GetValues(typeof(UserFields)).Cast<UserFields>();

	private static void ApplyOrderAndPaging(QueryBuilder<UserFields> builder, UserOptions options)
	{
		if (!EqualityComparer<UserFields>.Default.Equals(options.OrderBy, default))
		{
			var column = Enum.GetName(typeof(UserFields), options.OrderBy)!.ToLower();
			builder.Sql.Append($" ORDER BY {column} {(options.OrderDescending ? "DESC" : "ASC")}");
		}

		if (options.Limit is not null)
			builder.Sql.Append($" LIMIT {options.Limit} OFFSET {options.Offset ?? 0}");
	}

	private static IReadOnlyCollection<SqlParameter> ExtractParameters(QueryBuilder<UserFields> builder) =>
		builder.Parameters.ParameterNames
			.Select(name => new SqlParameter(name, builder.Parameters.Get<object>(name)!))
			.ToArray();
}

public sealed record SqlPreview(string Operation, string Sql, IReadOnlyCollection<SqlParameter> Parameters);

public sealed record SqlParameter(string Name, object? Value);