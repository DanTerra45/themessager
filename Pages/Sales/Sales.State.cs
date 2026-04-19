using Mercadito.src.application.sales.models;
using Microsoft.AspNetCore.Mvc;

namespace Mercadito.Pages.Sales
{
    public partial class SalesModel
    {
        private void BuildDraftPresentation()
        {
            var synchronizedDetails = new List<SaleDraftLineViewModel>(SaleDraft.Lines.Count);
            DraftTotal = 0m;

            foreach (var line in SaleDraft.Lines)
            {
                var detail = FindDraftLineDetail(line.ProductId);
                if (detail == null)
                {
                    detail = BuildDraftLineDetailFromContext(line.ProductId);
                }

                if (detail == null)
                {
                    detail = new SaleDraftLineViewModel
                    {
                        ProductId = line.ProductId,
                        ProductName = $"Producto #{line.ProductId}",
                        Batch = string.Empty,
                        UnitPrice = 0m,
                        Stock = 0
                    };
                }

                synchronizedDetails.Add(detail);
                DraftTotal += decimal.Round(detail.UnitPrice * line.Quantity, 2, MidpointRounding.AwayFromZero);
            }

            DraftLineDetails = synchronizedDetails;
            DraftLines = synchronizedDetails;
            SelectedCustomerLabel = ResolveSelectedCustomerLabel();
        }

        private SaleDraftLineViewModel? FindDraftLineDetail(long productId)
        {
            foreach (var detail in DraftLineDetails)
            {
                if (detail.ProductId == productId)
                {
                    return detail;
                }
            }

            return null;
        }

        private SaleDraftLineViewModel? BuildDraftLineDetailFromContext(long productId)
        {
            foreach (var product in RegistrationContext.Products)
            {
                if (product.Id == productId)
                {
                    return new SaleDraftLineViewModel
                    {
                        ProductId = product.Id,
                        ProductName = product.Name,
                        Batch = product.Batch,
                        UnitPrice = product.Price,
                        Stock = product.Stock
                    };
                }
            }

            return null;
        }

        public string GetSortIcon(string columnName)
        {
            return SalesTableSorting.GetSortIcon(SortBy, SortDirection, NormalizeSortBy(columnName));
        }

        public string GetNextSortDirection(string columnName)
        {
            return SalesTableSorting.GetNextSortDirection(SortBy, SortDirection, NormalizeSortBy(columnName));
        }

        private static string NormalizeSortBy(string? value)
        {
            return SalesTableSorting.NormalizeSortBy(
                value,
                "code",
                "createdat",
                "customer",
                "channel",
                "paymentmethod",
                "total",
                "status");
        }

        private string ResolveSelectedCustomerLabel()
        {
            if (SaleDraft.CustomerId <= 0)
            {
                return "Registrar cliente nuevo";
            }

            foreach (var customer in RegistrationContext.Customers)
            {
                if (customer.Id == SaleDraft.CustomerId)
                {
                    return $"{customer.DocumentNumber} - {customer.BusinessName}";
                }
            }

            return $"Cliente seleccionado #{SaleDraft.CustomerId}";
        }

        private void EnsureDraftDefaults()
        {
            if (SaleDraft == null)
            {
                SaleDraft = CreateDefaultDraft();
            }

            if (SaleDraft.NewCustomer == null)
            {
                SaleDraft.NewCustomer = new CreateCustomerDto();
            }

            if (SaleDraft.Lines == null)
            {
                SaleDraft.Lines = [];
            }

            if (string.IsNullOrWhiteSpace(SaleDraft.Channel))
            {
                SaleDraft.Channel = "Mostrador";
            }

            if (string.IsNullOrWhiteSpace(SaleDraft.PaymentMethod))
            {
                SaleDraft.PaymentMethod = "Efectivo";
            }

            if (DraftLineDetails == null)
            {
                DraftLineDetails = [];
            }
        }

        private static RegisterSaleDto CreateDefaultDraft()
        {
            return new RegisterSaleDto
            {
                Channel = "Mostrador",
                PaymentMethod = "Efectivo",
                NewCustomer = new CreateCustomerDto(),
                Lines = []
            };
        }

        private bool ShouldShowNewCustomerPanel()
        {
            if (SaleDraft.CustomerId > 0)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(SaleDraft.NewCustomer.DocumentNumber)
                || !string.IsNullOrWhiteSpace(SaleDraft.NewCustomer.BusinessName)
                || !string.IsNullOrWhiteSpace(SaleDraft.NewCustomer.Phone)
                || !string.IsNullOrWhiteSpace(SaleDraft.NewCustomer.Email)
                || !string.IsNullOrWhiteSpace(SaleDraft.NewCustomer.Address))
            {
                return true;
            }

            return ModelState.ContainsKey($"{nameof(SaleDraft)}.{nameof(RegisterSaleDto.NewCustomer)}.{nameof(CreateCustomerDto.DocumentNumber)}")
                || ModelState.ContainsKey($"{nameof(SaleDraft)}.{nameof(RegisterSaleDto.NewCustomer)}.{nameof(CreateCustomerDto.BusinessName)}")
                || ModelState.ContainsKey($"{nameof(SaleDraft)}.{nameof(RegisterSaleDto.NewCustomer)}.{nameof(CreateCustomerDto.Phone)}")
                || ModelState.ContainsKey($"{nameof(SaleDraft)}.{nameof(RegisterSaleDto.NewCustomer)}.{nameof(CreateCustomerDto.Email)}")
                || ModelState.ContainsKey($"{nameof(SaleDraft)}.{nameof(RegisterSaleDto.NewCustomer)}.{nameof(CreateCustomerDto.Address)}");
        }

        private string BuildReceiptUrl(long saleId)
        {
            if (saleId <= 0)
            {
                return string.Empty;
            }

            var pageUrl = Url.Page("/Sales/Receipt", values: new { saleId });
            if (string.IsNullOrWhiteSpace(pageUrl))
            {
                return string.Empty;
            }

            return pageUrl ?? string.Empty;
        }
    }
}
