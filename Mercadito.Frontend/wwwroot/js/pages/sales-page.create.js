(function () {
    var customerModule = window.salesPageCreateCustomer;
    var draftModule = window.salesPageCreateDraft;
    if (!customerModule || !draftModule) {
        return;
    }

    function createCreateSaleDraftController(configElement) {
        var createSaleForm = document.getElementById('createSaleForm');
        if (!createSaleForm) {
            return;
        }

        var originalLineCredits = {};
        if (configElement.dataset.originalLineCredits) {
            try {
                originalLineCredits = JSON.parse(configElement.dataset.originalLineCredits);
            } catch (error) {
                originalLineCredits = {};
            }
        }

        var customerSearchInput = document.getElementById('CustomerSearchTerm');
        var customerSearchButton = document.getElementById('searchCustomersButton');
        var customerOptionsContainer = document.getElementById('saleCustomerOptionList');
        var selectedCustomerInput = document.getElementById('SaleDraft_CustomerId');
        var customerDropdownToggle = document.getElementById('saleCustomerDropdownToggle');
        var customerDropdownMenu = document.getElementById('saleCustomerDropdownMenu');
        var newSaleModalElement = document.getElementById('newSaleModal');
        var newCustomerDraftModalElement = document.getElementById('newCustomerDraftModal');
        var selectedCustomerCard = document.getElementById('saleCustomerDropdownToggle');
        var selectedCustomerTitle = document.getElementById('saleSelectedCustomerTitle');
        var selectedCustomerMeta = document.getElementById('saleSelectedCustomerMeta');
        var selectedCustomerWarningIcon = document.getElementById('saleSelectedCustomerWarningIcon');
        var selectedCustomerTriggerIcon = document.getElementById('saleCustomerTriggerIcon');
        var openNewCustomerDraftButton = document.getElementById('openNewCustomerDraftButton');
        var closeNewCustomerDraftModalButton = document.getElementById('closeNewCustomerDraftModalButton');
        var applyNewCustomerDraftButton = document.getElementById('applyNewCustomerDraftButton');
        var backNewCustomerDraftButton = document.getElementById('backNewCustomerDraftButton');
        var discardNewCustomerDraftButton = document.getElementById('discardNewCustomerDraftButton');
        var newCustomerDocumentInput = document.getElementById('SaleDraft_NewCustomer_DocumentNumber');
        var newCustomerNameInput = document.getElementById('SaleDraft_NewCustomer_BusinessName');
        var newCustomerPhoneInput = document.getElementById('SaleDraft_NewCustomer_Phone');
        var newCustomerEmailInput = document.getElementById('SaleDraft_NewCustomer_Email');
        var newCustomerAddressInput = document.getElementById('SaleDraft_NewCustomer_Address');
        var productSearchInput = document.getElementById('ProductSearchTerm');
        var productSearchButton = document.getElementById('searchProductsButton');
        var productOptionsContainer = document.getElementById('saleProductOptionList');
        var selectedProductInput = document.getElementById('ProductToAddId');
        var draftHiddenFields = document.getElementById('saleDraftLineHiddenFields');
        var draftTableBody = document.getElementById('saleDraftLinesTableBody');
        var draftTotalValue = document.getElementById('saleDraftTotalValue');
        var openConfirmSaleSaveButton = document.getElementById('openConfirmSaleSaveButton');
        var confirmSaleSaveModalElement = document.getElementById('confirmSaleSaveModal');
        var confirmSaleSubmitButton = document.getElementById('confirmSaleSubmitButton');
        var closeConfirmSaleSaveModalButton = document.getElementById('closeConfirmSaleSaveModalButton');
        var backToSaleFromConfirmButton = document.getElementById('backToSaleFromConfirmButton');

        if (!customerSearchInput
            || !customerSearchButton
            || !customerOptionsContainer
            || !selectedCustomerInput
            || !customerDropdownToggle
            || !customerDropdownMenu
            || !newSaleModalElement
            || !newCustomerDraftModalElement
            || !selectedCustomerCard
            || !selectedCustomerTitle
            || !selectedCustomerMeta
            || !selectedCustomerWarningIcon
            || !selectedCustomerTriggerIcon
            || !openNewCustomerDraftButton
            || !closeNewCustomerDraftModalButton
            || !applyNewCustomerDraftButton
            || !backNewCustomerDraftButton
            || !discardNewCustomerDraftButton
            || !newCustomerDocumentInput
            || !newCustomerNameInput
            || !newCustomerPhoneInput
            || !newCustomerEmailInput
            || !newCustomerAddressInput
            || !productSearchInput
            || !productSearchButton
            || !productOptionsContainer
            || !selectedProductInput
            || !draftHiddenFields
            || !draftTableBody
            || !draftTotalValue
            || !openConfirmSaleSaveButton
            || !confirmSaleSaveModalElement
            || !confirmSaleSubmitButton
            || !closeConfirmSaleSaveModalButton
            || !backToSaleFromConfirmButton) {
            return;
        }

        var newSaleModal = bootstrap.Modal.getOrCreateInstance(newSaleModalElement);
        var confirmSaleSaveModal = bootstrap.Modal.getOrCreateInstance(confirmSaleSaveModalElement);
        var shouldReturnFromConfirmToSaleModal = false;
        var draftController = null;
        var customerController = null;

        function getCurrentIssueState() {
            if (!draftController) {
                return {
                    hasBlockingErrors: false
                };
            }

            return draftController.getIssueState();
        }

        function getCurrentCustomerIssueState() {
            if (!customerController) {
                return {
                    hasBlockingErrors: false,
                    shouldDisableSave: false
                };
            }

            return customerController.getIssueState();
        }

        function updateSaveButtons() {
            var issueState = getCurrentIssueState();
            var customerIssueState = getCurrentCustomerIssueState();
            var hasBlockingErrors = issueState.hasBlockingErrors || customerIssueState.shouldDisableSave;
            openConfirmSaleSaveButton.disabled = hasBlockingErrors;
            confirmSaleSubmitButton.disabled = hasBlockingErrors;
        }

        customerController = customerModule.createCustomerDraftController({
            createSaleForm: createSaleForm,
            customerSearchInput: customerSearchInput,
            customerSearchButton: customerSearchButton,
            customerOptionsContainer: customerOptionsContainer,
            selectedCustomerInput: selectedCustomerInput,
            customerDropdownToggle: customerDropdownToggle,
            customerDropdownMenu: customerDropdownMenu,
            newSaleModalElement: newSaleModalElement,
            newCustomerDraftModalElement: newCustomerDraftModalElement,
            selectedCustomerCard: selectedCustomerCard,
            selectedCustomerTitle: selectedCustomerTitle,
            selectedCustomerMeta: selectedCustomerMeta,
            selectedCustomerWarningIcon: selectedCustomerWarningIcon,
            selectedCustomerTriggerIcon: selectedCustomerTriggerIcon,
            openNewCustomerDraftButton: openNewCustomerDraftButton,
            closeNewCustomerDraftModalButton: closeNewCustomerDraftModalButton,
            applyNewCustomerDraftButton: applyNewCustomerDraftButton,
            backNewCustomerDraftButton: backNewCustomerDraftButton,
            discardNewCustomerDraftButton: discardNewCustomerDraftButton,
            newCustomerDocumentInput: newCustomerDocumentInput,
            newCustomerNameInput: newCustomerNameInput,
            newCustomerPhoneInput: newCustomerPhoneInput,
            newCustomerEmailInput: newCustomerEmailInput,
            newCustomerAddressInput: newCustomerAddressInput,
            customerSearchUrl: configElement.dataset.customerSearchUrl || '',
            onStateChanged: updateSaveButtons
        });

        draftController = draftModule.createSaleDraftLinesController({
            createSaleForm: createSaleForm,
            productSearchInput: productSearchInput,
            productSearchButton: productSearchButton,
            productOptionsContainer: productOptionsContainer,
            selectedProductInput: selectedProductInput,
            draftHiddenFields: draftHiddenFields,
            draftTableBody: draftTableBody,
            draftTotalValue: draftTotalValue,
            productSearchUrl: configElement.dataset.productSearchUrl || '',
            originalLineCredits: originalLineCredits,
            onStateChanged: updateSaveButtons
        });

        openConfirmSaleSaveButton.addEventListener('click', function () {
            customerController.showValidation();
            customerController.syncState();

            var draftIssueState = getCurrentIssueState();
            var customerIssueState = getCurrentCustomerIssueState();
            updateSaveButtons();

            if (customerController.isDraftModalOpen()) {
                return;
            }

            if (draftIssueState.hasBlockingErrors || customerIssueState.hasBlockingErrors) {
                return;
            }

            shouldReturnFromConfirmToSaleModal = false;

            newSaleModalElement.addEventListener('hidden.bs.modal', function handleSaleHiddenForConfirm() {
                newSaleModalElement.removeEventListener('hidden.bs.modal', handleSaleHiddenForConfirm);
                confirmSaleSaveModal.show();
            });

            newSaleModal.hide();
        });

        confirmSaleSaveModalElement.addEventListener('show.bs.modal', function (event) {
            if (!customerController.isDraftModalOpen()) {
                return;
            }

            event.preventDefault();
        });

        confirmSaleSaveModalElement.addEventListener('hidden.bs.modal', function () {
            if (!shouldReturnFromConfirmToSaleModal) {
                return;
            }

            newSaleModal.show();
        });

        closeConfirmSaleSaveModalButton.addEventListener('click', function () {
            shouldReturnFromConfirmToSaleModal = false;
        });

        backToSaleFromConfirmButton.addEventListener('click', function () {
            shouldReturnFromConfirmToSaleModal = true;
        });

        confirmSaleSubmitButton.addEventListener('click', function () {
            shouldReturnFromConfirmToSaleModal = false;
        });

        customerController.syncState();
        draftController.renderState();
        updateSaveButtons();
    }

    window.salesPageCreate = {
        createCreateSaleDraftController: createCreateSaleDraftController
    };
})();
