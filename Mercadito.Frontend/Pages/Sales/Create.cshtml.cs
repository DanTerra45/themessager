using System.Text.RegularExpressions;
using Mercadito.Frontend.Adapters.Sales;
using Mercadito.Frontend.Dtos.Common;
using Mercadito.Frontend.Dtos.Sales;
using Mercadito.Frontend.Pages.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Mercadito.Frontend.Pages.Sales;

public sealed class CreateModel(ISalesApiAdapter salesApiAdapter, ILogger<CreateModel> logger) : FrontendPageModel
{
    private static readonly Regex CustomerDocumentPattern = new(
        "^([0-9A-Za-z-]{5,20}|0)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public CreateSaleInitialState InitialState { get; private set; } = CreateSaleInitialState.Empty;

    public IReadOnlyList<string> Errors { get; private set; } = [];

    public async Task OnGetAsync()
    {
        await LoadInitialStateAsync();
    }

    public async Task<IActionResult> OnGetCustomersAsync(string searchTerm = "")
    {
        var result = await salesApiAdapter.SearchCustomersAsync(searchTerm, HttpContext.RequestAborted);
        return new JsonResult(result)
        {
            StatusCode = result.Success ? StatusCodes.Status200OK : StatusCodes.Status400BadRequest
        };
    }

    public async Task<IActionResult> OnGetProductsAsync(string searchTerm = "")
    {
        var result = await salesApiAdapter.SearchProductsAsync(searchTerm, HttpContext.RequestAborted);
        return new JsonResult(result)
        {
            StatusCode = result.Success ? StatusCodes.Status200OK : StatusCodes.Status400BadRequest
        };
    }

    public async Task<IActionResult> OnPostRegisterAsync([FromBody] RegisterSaleRequestDto? request)
    {
        var validationErrors = ValidateRegisterRequest(request);
        if (validationErrors.Count > 0)
        {
            return ToRegisterFailure(validationErrors);
        }

        var normalizedRequest = NormalizeRegisterRequest(request!);
        var result = await salesApiAdapter.RegisterSaleAsync(
            normalizedRequest,
            BuildActorContext(),
            HttpContext.RequestAborted);
        if (!result.Success)
        {
            logger.LogWarning(
                "No se pudo registrar la venta desde el frontend: {Errors}",
                string.Join(" | ", result.Errors));
        }

        return new JsonResult(result)
        {
            StatusCode = result.Success ? StatusCodes.Status200OK : StatusCodes.Status400BadRequest
        };
    }

    private async Task LoadInitialStateAsync()
    {
        var result = await salesApiAdapter.GetRegistrationContextAsync(cancellationToken: HttpContext.RequestAborted);
        if (result.Success && result.Data != null)
        {
            InitialState = new CreateSaleInitialState(
                result.Data.NextSaleCode,
                result.Data.Customers,
                result.Data.Products);
            return;
        }

        Errors = result.Errors
            .Where(error => !string.IsNullOrWhiteSpace(error))
            .DefaultIfEmpty("No se pudo cargar el contexto inicial de ventas.")
            .ToList();

        logger.LogWarning(
            "No se pudo cargar el contexto inicial de nueva venta: {Errors}",
            string.Join(" | ", Errors));
    }

    private static JsonResult ToRegisterFailure(IReadOnlyList<string> errors)
    {
        return new JsonResult(new ApiResponseDto<SaleReceiptDto>(false, null, errors))
        {
            StatusCode = StatusCodes.Status400BadRequest
        };
    }

    private static RegisterSaleRequestDto NormalizeRegisterRequest(RegisterSaleRequestDto request)
    {
        var newCustomer = request.NewCustomer == null
            ? null
            : new CreateSaleCustomerRequestDto(
                NormalizeRequired(request.NewCustomer.CiNit),
                NormalizeRequired(request.NewCustomer.BusinessName),
                NormalizeOptional(request.NewCustomer.Phone),
                NormalizeOptional(request.NewCustomer.Email),
                NormalizeOptional(request.NewCustomer.Address));

        var lines = (request.Lines ?? [])
            .Where(line => line.ProductId > 0 && line.Quantity > 0)
            .Select(line => new RegisterSaleLineRequestDto(
                line.ProductId,
                NormalizeOptional(line.LotCode) ?? string.Empty,
                line.Quantity))
            .ToList();

        return new RegisterSaleRequestDto(
            request.CustomerId.GetValueOrDefault() > 0 ? request.CustomerId : null,
            newCustomer,
            NormalizeRequired(request.Channel),
            NormalizeRequired(request.PaymentMethod),
            lines);
    }

    private static IReadOnlyList<string> ValidateRegisterRequest(RegisterSaleRequestDto? request)
    {
        if (request == null)
        {
            return ["La venta es obligatoria."];
        }

        var errors = new List<string>();
        AddRequiredTextError(errors, request.Channel, "El canal es obligatorio.");
        AddLengthError(errors, request.Channel, 30, "El canal no puede exceder 30 caracteres.");
        AddRequiredTextError(errors, request.PaymentMethod, "El método de pago es obligatorio.");
        AddLengthError(errors, request.PaymentMethod, 30, "El método de pago no puede exceder 30 caracteres.");

        if (request.CustomerId.GetValueOrDefault() <= 0)
        {
            ValidateNewCustomer(request.NewCustomer, errors);
        }

        ValidateLines(request.Lines, errors);
        return errors;
    }

    private static void ValidateNewCustomer(CreateSaleCustomerRequestDto? customer, List<string> errors)
    {
        if (customer == null)
        {
            errors.Add("Debes seleccionar un cliente o registrar uno nuevo.");
            return;
        }

        AddRequiredTextError(errors, customer.CiNit, "El CI/NIT es obligatorio.");
        AddLengthError(errors, customer.CiNit, 20, "El CI/NIT no puede exceder 20 caracteres.");

        var documentNumber = NormalizeRequired(customer.CiNit);
        if (!string.IsNullOrWhiteSpace(documentNumber) && !CustomerDocumentPattern.IsMatch(documentNumber))
        {
            errors.Add("El CI/NIT debe ser 0 o tener entre 5 y 20 caracteres válidos.");
        }

        AddRequiredTextError(errors, customer.BusinessName, "La razón social es obligatoria.");
        AddLengthError(errors, customer.BusinessName, 150, "La razón social no puede exceder 150 caracteres.");
        AddLengthError(errors, customer.Phone, 20, "El teléfono no puede exceder 20 caracteres.");
        AddLengthError(errors, customer.Email, 100, "El correo no puede exceder 100 caracteres.");
        AddLengthError(errors, customer.Address, 150, "La dirección no puede exceder 150 caracteres.");
    }

    private static void ValidateLines(IReadOnlyList<RegisterSaleLineRequestDto>? lines, List<string> errors)
    {
        if (lines == null || lines.Count == 0)
        {
            errors.Add("Debes agregar al menos un producto a la venta.");
            return;
        }

        foreach (var line in lines)
        {
            if (line.ProductId <= 0)
            {
                errors.Add("Hay un producto inválido en el detalle de la venta.");
            }

            if (line.Quantity <= 0)
            {
                errors.Add("La cantidad de cada producto debe ser mayor que cero.");
            }
        }
    }

    private static void AddRequiredTextError(List<string> errors, string? value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(message);
        }
    }

    private static void AddLengthError(List<string> errors, string? value, int maximumLength, string message)
    {
        if (!string.IsNullOrWhiteSpace(value) && value.Trim().Length > maximumLength)
        {
            errors.Add(message);
        }
    }

    private static string NormalizeRequired(string? value)
    {
        return string.Join(' ', (value ?? string.Empty).Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = NormalizeRequired(value);
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}

public sealed record CreateSaleInitialState(
    string NextSaleCode,
    IReadOnlyList<CustomerOptionDto> Customers,
    IReadOnlyList<SaleProductOptionDto> Products)
{
    public static CreateSaleInitialState Empty { get; } = new(string.Empty, [], []);
}
