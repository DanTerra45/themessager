namespace ServicioFrontend.Dtos.Sales;

public sealed record CancelSaleRequestDto(
    long SaleId,
    string Reason);
