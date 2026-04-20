using Mercadito.Pages.Infrastructure;
using Mercadito.src.application.sales.models;
using Mercadito.src.application.sales.ports.input;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Mercadito.Pages.Sales
{
    public partial class SalesModel(
        ISalesQueryFacade salesQueryFacade,
        IRegisterSaleFacade registerSaleFacade,
        IUpdateSaleFacade updateSaleFacade,
        ILogger<SalesModel> logger) : AppPageModel
    {
        private readonly ISalesQueryFacade _salesQueryFacade = salesQueryFacade;
        private readonly IRegisterSaleFacade _registerSaleFacade = registerSaleFacade;
        private readonly IUpdateSaleFacade _updateSaleFacade = updateSaleFacade;
        private readonly ILogger<SalesModel> _logger = logger;

        [BindProperty]
        public RegisterSaleDto SaleDraft { get; set; } = CreateDefaultDraft();

        [BindProperty]
        public List<SaleDraftLineViewModel> DraftLineDetails { get; set; } = [];

        [BindProperty]
        public string CustomerSearchTerm { get; set; } = string.Empty;

        [BindProperty]
        public string ProductSearchTerm { get; set; } = string.Empty;

        [BindProperty]
        public long ProductToAddId { get; set; }

        [BindProperty(SupportsGet = true)]
        public long DetailSaleId { get; set; }

        [BindProperty(SupportsGet = true)]
        public long AutoOpenReceiptSaleId { get; set; }

        [BindProperty(SupportsGet = true)]
        public long EditSaleId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SortBy { get; set; } = SalesTableSorting.DefaultSortBy;

        [BindProperty(SupportsGet = true)]
        public string SortDirection { get; set; } = SalesTableSorting.DefaultSortDirection;

        public SalesRegistrationContext RegistrationContext { get; private set; } = new();
        public IReadOnlyList<SaleSummaryItem> RecentSales { get; private set; } = [];
        public IReadOnlyList<SaleDraftLineViewModel> DraftLines { get; private set; } = [];
        public SaleDetailDto? SelectedSaleDetail { get; private set; }
        public bool ShowCreateModal { get; private set; }
        public bool ShowDetailModal { get; private set; }
        public bool ShowNewCustomerPanel { get; private set; }
        public decimal SalesTodayTotal { get; private set; }
        public decimal AverageTicketToday { get; private set; }
        public int SalesTodayCount { get; private set; }
        public decimal DraftTotal { get; private set; }
        public string SelectedCustomerLabel { get; private set; } = "Registrar cliente nuevo";
        public string AutoOpenReceiptUrl { get; private set; } = string.Empty;
        public string OriginalSaleLineCreditsJson { get; private set; } = "{}";
        public bool IsEditingSale => EditSaleId > 0;
        public string DraftModalTitle => IsEditingSale ? "Editar Venta" : "Nueva Venta";
        public string DraftModalActionText => IsEditingSale ? "Actualizar Venta" : "Guardar Venta";
        public string ConfirmDraftCopy => IsEditingSale ? "¿Confirmas la actualización de esta venta?" : "¿Confirmas el registro de esta venta?";
        public string ConfirmDraftActionText => IsEditingSale ? "Actualizar" : "Confirmar";
        public string DraftSuccessActionText => IsEditingSale ? "actualizada" : "registrada";

        public async Task OnGetAsync()
        {
            EnsureDraftDefaults();

            if (EditSaleId > 0)
            {
                await LoadEditDraftAsync(EditSaleId);
            }

            await LoadPageDataAsync();

            if (DetailSaleId > 0)
            {
                await LoadSaleDetailAsync(DetailSaleId);
            }
        }
    }

    public sealed class SaleDraftLineViewModel
    {
        public long ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Batch { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Stock { get; set; }
    }
}
