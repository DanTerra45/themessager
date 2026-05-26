(function () {
    var shared = window.salesPageShared;
    if (!shared) {
        return;
    }

    document.addEventListener('DOMContentLoaded', function () {
        if (typeof bootstrap === 'undefined') {
            return;
        }

        shared.bindCancelModal();

        var configElement = document.getElementById('salesPageConfig');
        if (!configElement) {
            return;
        }

        if (window.salesPageCreate && typeof window.salesPageCreate.createCreateSaleDraftController === 'function') {
            window.salesPageCreate.createCreateSaleDraftController(configElement);
        }

        shared.showModalIfNeeded(configElement.dataset.showCreateModal === 'true', 'newSaleModal');
        shared.showModalIfNeeded(configElement.dataset.showDetailModal === 'true', 'saleDetailModal');
        shared.showModalIfNeeded(configElement.dataset.showCancelModal === 'true', 'cancelSaleModal');

        if (configElement.dataset.showCreateModal === 'true' && configElement.dataset.showNewCustomerModal === 'true') {
            window.setTimeout(function () {
                var openNewCustomerDraftButton = document.getElementById('openNewCustomerDraftButton');
                if (openNewCustomerDraftButton) {
                    openNewCustomerDraftButton.click();
                }
            }, 200);
        }

        shared.autoOpenReceipt(configElement.dataset.autoOpenReceiptUrl || '');
    });
})();
