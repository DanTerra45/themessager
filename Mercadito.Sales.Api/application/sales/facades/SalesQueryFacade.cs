using System.ComponentModel.DataAnnotations;
using Mercadito.Sales.Api.Application.Sales.Models;
using Mercadito.Sales.Api.Application.Sales.Ports.Input;
using Mercadito.Sales.Api.Application.Sales.Ports.Output;
using Mercadito.Sales.Api.Domain.Shared;
using Mercadito.Sales.Api.Domain.Shared.Exceptions;
using Mercadito.Sales.Api.Domain.Shared.Validation;

namespace Mercadito.Sales.Api.Application.Sales.Facades
{
    public sealed class SalesQueryFacade(ISalesRepository salesRepository) : ISalesQueryFacade
    {
        public async Task<Result<SalesRegistrationContext>> LoadRegistrationContextAsync(string customerSearchTerm = "", string productSearchTerm = "", CancellationToken cancellationToken = default)
        {
            try
            {
                var normalizedCustomerSearchTerm = ValidationText.NormalizeCollapsed(customerSearchTerm);
                var normalizedProductSearchTerm = ValidationText.NormalizeCollapsed(productSearchTerm);
                var nextSaleCode = await salesRepository.GetNextSaleCodeAsync(cancellationToken);
                var customers = await salesRepository.SearchCustomersAsync(normalizedCustomerSearchTerm, cancellationToken);
                var products = await salesRepository.SearchProductsAsync(normalizedProductSearchTerm, cancellationToken);

                return Result.Success(new SalesRegistrationContext
                {
                    NextSaleCode = nextSaleCode,
                    Customers = customers,
                    Products = products
                });
            }
            catch (BusinessValidationException validationException)
            {
                if (validationException.Errors.Count > 0)
                {
                    return Result.Failure<SalesRegistrationContext>(validationException.Errors);
                }

                return Result.Failure<SalesRegistrationContext>(validationException.Message);
            }
            catch (ValidationException validationException)
            {
                return Result.Failure<SalesRegistrationContext>(validationException.Message);
            }
            catch (DataStoreUnavailableException exception)
            {
                return Result.Failure<SalesRegistrationContext>(exception.Message);
            }
        }

        public async Task<Result<IReadOnlyList<CustomerLookupItem>>> SearchCustomersAsync(string customerSearchTerm = "", CancellationToken cancellationToken = default)
        {
            try
            {
                var normalizedCustomerSearchTerm = ValidationText.NormalizeCollapsed(customerSearchTerm);
                var customers = await salesRepository.SearchCustomersAsync(normalizedCustomerSearchTerm, cancellationToken);
                return Result.Success<IReadOnlyList<CustomerLookupItem>>(customers);
            }
            catch (DataStoreUnavailableException exception)
            {
                return Result.Failure<IReadOnlyList<CustomerLookupItem>>(exception.Message);
            }
        }

        public async Task<Result<IReadOnlyList<SaleProductOption>>> SearchProductsAsync(string productSearchTerm = "", CancellationToken cancellationToken = default)
        {
            try
            {
                var normalizedProductSearchTerm = ValidationText.NormalizeCollapsed(productSearchTerm);
                var products = await salesRepository.SearchProductsAsync(normalizedProductSearchTerm, cancellationToken);
                return Result.Success<IReadOnlyList<SaleProductOption>>(products);
            }
            catch (DataStoreUnavailableException exception)
            {
                return Result.Failure<IReadOnlyList<SaleProductOption>>(exception.Message);
            }
        }

        public async Task<Result<IReadOnlyList<SaleSummaryItem>>> GetRecentSalesAsync(
            int take = 20,
            string sortBy = "createdat",
            string sortDirection = "desc",
            DateOnly? fromDate = null,
            DateOnly? toDate = null,
            string status = "",
            string paymentMethod = "",
            CancellationToken cancellationToken = default)
        {
            var normalizedTake = 20;
            if (take > 0)
            {
                normalizedTake = Math.Min(take, 500);
            }

            if (fromDate.HasValue && toDate.HasValue && fromDate.Value > toDate.Value)
            {
                return Result.Failure<IReadOnlyList<SaleSummaryItem>>("La fecha inicial no puede ser mayor a la fecha final.");
            }

            try
            {
                var normalizedStatus = NormalizeKnownValue(status, "Registrada", "Anulada");
                var normalizedPaymentMethod = NormalizeKnownValue(paymentMethod, "Efectivo", "Tarjeta", "QR");
                var sales = await salesRepository.GetRecentSalesAsync(
                    normalizedTake,
                    sortBy,
                    sortDirection,
                    fromDate,
                    toDate,
                    normalizedStatus,
                    normalizedPaymentMethod,
                    cancellationToken);

                return Result.Success<IReadOnlyList<SaleSummaryItem>>(sales);
            }
            catch (DataStoreUnavailableException exception)
            {
                return Result.Failure<IReadOnlyList<SaleSummaryItem>>(exception.Message);
            }
        }

        public async Task<Result<SalesOverviewMetrics>> GetOverviewMetricsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var metrics = await salesRepository.GetOverviewMetricsAsync(cancellationToken);
                return Result.Success(metrics);
            }
            catch (DataStoreUnavailableException exception)
            {
                return Result.Failure<SalesOverviewMetrics>(exception.Message);
            }
        }

        public async Task<Result<DailyCashClosingReport>> GetDailyCashClosingReportAsync(DateOnly businessDate, CancellationToken cancellationToken = default)
        {
            if (businessDate == default)
            {
                return Result.Failure<DailyCashClosingReport>("La fecha del cierre de caja es inválida.");
            }

            try
            {
                var report = await salesRepository.GetDailyCashClosingReportAsync(businessDate, cancellationToken);
                return Result.Success(report);
            }
            catch (DataStoreUnavailableException exception)
            {
                return Result.Failure<DailyCashClosingReport>(exception.Message);
            }
        }

        public async Task<Result<SaleDetailDto>> GetSaleDetailAsync(long saleId, CancellationToken cancellationToken = default)
        {
            if (saleId <= 0)
            {
                return Result.Failure<SaleDetailDto>("El identificador de la venta es inválido.");
            }

            try
            {
                var saleDetail = await salesRepository.GetSaleDetailAsync(saleId, cancellationToken);
                if (saleDetail == null)
                {
                    return Result.Failure<SaleDetailDto>("La venta solicitada no existe.");
                }

                return Result.Success(saleDetail);
            }
            catch (DataStoreUnavailableException exception)
            {
                return Result.Failure<SaleDetailDto>(exception.Message);
            }
        }

        public async Task<Result<SaleReceiptDto>> GetSaleReceiptAsync(long saleId, CancellationToken cancellationToken = default)
        {
            if (saleId <= 0)
            {
                return Result.Failure<SaleReceiptDto>("El identificador de la venta es inválido.");
            }

            try
            {
                var receipt = await salesRepository.GetSaleReceiptAsync(saleId, cancellationToken);
                if (receipt == null)
                {
                    return Result.Failure<SaleReceiptDto>("No se encontró el comprobante solicitado.");
                }

                return Result.Success(receipt);
            }
            catch (DataStoreUnavailableException exception)
            {
                return Result.Failure<SaleReceiptDto>(exception.Message);
            }
        }

        private static string NormalizeKnownValue(string? value, params string[] allowedValues)
        {
            var normalizedValue = ValidationText.NormalizeCollapsed(value);
            if (string.IsNullOrWhiteSpace(normalizedValue))
            {
                return string.Empty;
            }

            foreach (var allowedValue in allowedValues)
            {
                if (string.Equals(normalizedValue, allowedValue, StringComparison.OrdinalIgnoreCase))
                {
                    return allowedValue;
                }
            }

            return string.Empty;
        }
    }
}
