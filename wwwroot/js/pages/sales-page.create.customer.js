(function () {
    var shared = window.salesPageShared;
    if (!shared) {
        return;
    }

    var escapeHtml = shared.escapeHtml;
    var parseInteger = shared.parseInteger;
    var getNormalizedFieldValueByMode = shared.getNormalizedFieldValueByMode;
    var normalizeFieldValueByMode = shared.normalizeFieldValueByMode;
    var findValidationSpan = shared.findValidationSpan;
    var setFieldValidationState = shared.setFieldValidationState;
    var clearFieldValidationState = shared.clearFieldValidationState;

    function bindResetToAllOnEmpty(searchInput, reloadAction) {
        searchInput.addEventListener('input', function () {
            if (searchInput.value.trim().length !== 0) {
                return;
            }

            reloadAction();
        });
    }

    function markSelectedOption(container, selector, attributeName, selectedValue) {
        var options = container.querySelectorAll(selector);
        options.forEach(function (currentOption) {
            currentOption.classList.remove('is-selected');
            currentOption.setAttribute('aria-pressed', 'false');

            if (currentOption.getAttribute(attributeName) === String(selectedValue)) {
                currentOption.classList.add('is-selected');
                currentOption.setAttribute('aria-pressed', 'true');
            }
        });
    }

    function bindCustomerSelection(container, selectedCustomerInput, onCustomerSelected) {
        container.addEventListener('click', function (event) {
            var option = event.target.closest('[data-sale-customer-option="true"]');
            if (!option) {
                return;
            }

            var selectedCustomerId = option.getAttribute('data-customer-id');
            if (!selectedCustomerId) {
                return;
            }

            selectedCustomerInput.value = selectedCustomerId;
            markSelectedOption(container, '[data-sale-customer-option="true"]', 'data-customer-id', selectedCustomerId);
            onCustomerSelected();
        });
    }

    async function runCustomerSearch(searchUrl, term, customerOptionsContainer, selectedCustomerInput, syncCustomerDraftState) {
        if (!searchUrl) {
            return;
        }

        var requestUrl = new URL(searchUrl, window.location.origin);
        requestUrl.searchParams.set('customerSearchTerm', term);

        var response = await fetch(requestUrl.toString(), {
            headers: {
                'X-Requested-With': 'XMLHttpRequest'
            }
        });

        if (!response.ok) {
            return;
        }

        var customers = await response.json();
        renderCustomerOptions(customerOptionsContainer, customers, selectedCustomerInput);
        syncCustomerDraftState();
    }

    function renderCustomerOptions(container, customers, selectedCustomerInput) {
        if (customers.length === 0) {
            container.innerHTML = '<article class="sale-product-option-empty">No hay clientes que coincidan con la búsqueda actual.</article>';
            return;
        }

        var selectedCustomerStillExists = false;
        var markup = '';
        for (var index = 0; index < customers.length; index += 1) {
            var customer = customers[index];
            var optionClasses = 'sale-customer-option';
            var ariaPressed = 'false';

            if (String(customer.id) === String(selectedCustomerInput.value)) {
                optionClasses += ' is-selected';
                ariaPressed = 'true';
                selectedCustomerStillExists = true;
            }

            markup += '<button type="button" class="' + optionClasses + '" data-sale-customer-option="true" data-customer-id="' + escapeHtml(customer.id) + '" data-customer-document="' + escapeHtml(customer.documentNumber) + '" data-customer-name="' + escapeHtml(customer.businessName) + '" aria-pressed="' + ariaPressed + '">';
            markup += '<span class="sale-customer-option-name">' + escapeHtml(customer.businessName) + '</span>';
            markup += '<span class="sale-customer-option-meta">' + escapeHtml(customer.documentNumber) + '</span>';
            markup += '</button>';
        }

        container.innerHTML = markup;

        if (selectedCustomerStillExists) {
            return;
        }

        if (selectedCustomerInput.value === '0' || selectedCustomerInput.value === '') {
            return;
        }

        selectedCustomerInput.value = '0';
    }

    function getSelectedCustomerOption(container, selectedCustomerId) {
        if (!selectedCustomerId) {
            return null;
        }

        var option = null;
        var customerOptions = container.querySelectorAll('[data-sale-customer-option="true"]');
        customerOptions.forEach(function (currentOption) {
            if (option) {
                return;
            }

            if (currentOption.getAttribute('data-customer-id') === String(selectedCustomerId)) {
                option = currentOption;
            }
        });

        if (!option) {
            return null;
        }

        return {
            customerId: parseInteger(option.getAttribute('data-customer-id'), 0),
            documentNumber: option.getAttribute('data-customer-document') || '',
            businessName: option.getAttribute('data-customer-name') || ''
        };
    }

    function getValidationMessage(root, fieldName) {
        var validationSpan = findValidationSpan(root, fieldName);
        if (!validationSpan || !validationSpan.classList.contains('field-validation-error')) {
            return '';
        }

        var message = (validationSpan.getAttribute('data-error-message') || validationSpan.textContent || '').trim();
        if (message.length === 0) {
            return 'Valor inválido.';
        }

        return message;
    }

    function createCustomerDraftController(options) {
        var createSaleForm = options.createSaleForm;
        var customerSearchInput = options.customerSearchInput;
        var customerSearchButton = options.customerSearchButton;
        var customerOptionsContainer = options.customerOptionsContainer;
        var selectedCustomerInput = options.selectedCustomerInput;
        var newSaleModalElement = options.newSaleModalElement;
        var newCustomerDraftModalElement = options.newCustomerDraftModalElement;
        var selectedCustomerCard = options.selectedCustomerCard;
        var selectedCustomerTitle = options.selectedCustomerTitle;
        var selectedCustomerMeta = options.selectedCustomerMeta;
        var selectedCustomerWarningIcon = options.selectedCustomerWarningIcon;
        var openNewCustomerDraftButton = options.openNewCustomerDraftButton;
        var closeNewCustomerDraftModalButton = options.closeNewCustomerDraftModalButton;
        var applyNewCustomerDraftButton = options.applyNewCustomerDraftButton;
        var backNewCustomerDraftButton = options.backNewCustomerDraftButton;
        var discardNewCustomerDraftButton = options.discardNewCustomerDraftButton;
        var newCustomerDocumentInput = options.newCustomerDocumentInput;
        var newCustomerNameInput = options.newCustomerNameInput;
        var newCustomerPhoneInput = options.newCustomerPhoneInput;
        var newCustomerEmailInput = options.newCustomerEmailInput;
        var newCustomerAddressInput = options.newCustomerAddressInput;
        var customerSearchUrl = options.customerSearchUrl || '';
        var onStateChanged = typeof options.onStateChanged === 'function' ? options.onStateChanged : function () { };

        var newSaleModal = bootstrap.Modal.getOrCreateInstance(newSaleModalElement);
        var newCustomerDraftModal = bootstrap.Modal.getOrCreateInstance(newCustomerDraftModalElement);
        var shouldReturnToSaleModal = false;
        var customerValidationVisible = false;

        [newCustomerDocumentInput, newCustomerNameInput, newCustomerPhoneInput, newCustomerEmailInput, newCustomerAddressInput].forEach(function (field) {
            field.removeAttribute('required');
            field.removeAttribute('pattern');
            field.removeAttribute('minlength');
        });

        function notifyStateChanged() {
            onStateChanged();
        }

        function clearNewCustomerDraftValidation() {
            clearFieldValidationState(createSaleForm, newCustomerDocumentInput);
            clearFieldValidationState(createSaleForm, newCustomerNameInput);
            clearFieldValidationState(createSaleForm, newCustomerPhoneInput);
            clearFieldValidationState(createSaleForm, newCustomerEmailInput);
            clearFieldValidationState(createSaleForm, newCustomerAddressInput);
        }

        function validateNewCustomerDraft() {
            var hasErrors = false;
            var documentNumber = normalizeFieldValueByMode(newCustomerDocumentInput);
            var businessName = normalizeFieldValueByMode(newCustomerNameInput);
            var phone = normalizeFieldValueByMode(newCustomerPhoneInput);
            var email = normalizeFieldValueByMode(newCustomerEmailInput);
            var address = normalizeFieldValueByMode(newCustomerAddressInput);
            var emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

            clearNewCustomerDraftValidation();

            if (documentNumber.length === 0) {
                setFieldValidationState(createSaleForm, newCustomerDocumentInput, 'El CI/NIT es obligatorio.');
                hasErrors = true;
            } else if (documentNumber.length > 20) {
                setFieldValidationState(createSaleForm, newCustomerDocumentInput, 'El CI/NIT debe tener entre 1 y 20 caracteres.');
                hasErrors = true;
            } else if (!/^([0-9A-Za-z-]{5,20}|0)$/.test(documentNumber)) {
                setFieldValidationState(createSaleForm, newCustomerDocumentInput, 'El CI/NIT debe ser 0 o tener entre 5 y 20 caracteres válidos.');
                hasErrors = true;
            }

            if (businessName.length === 0) {
                setFieldValidationState(createSaleForm, newCustomerNameInput, 'La razón social es obligatoria.');
                hasErrors = true;
            } else if (businessName.length > 150) {
                setFieldValidationState(createSaleForm, newCustomerNameInput, 'La razón social no puede exceder 150 caracteres.');
                hasErrors = true;
            }

            if (phone.length > 20) {
                setFieldValidationState(createSaleForm, newCustomerPhoneInput, 'El teléfono no puede exceder 20 caracteres.');
                hasErrors = true;
            }

            if (email.length > 100) {
                setFieldValidationState(createSaleForm, newCustomerEmailInput, 'El correo no puede exceder 100 caracteres.');
                hasErrors = true;
            } else if (email.length > 0 && !emailPattern.test(email)) {
                setFieldValidationState(createSaleForm, newCustomerEmailInput, 'El correo no tiene un formato válido.');
                hasErrors = true;
            }

            if (address.length > 150) {
                setFieldValidationState(createSaleForm, newCustomerAddressInput, 'La dirección no puede exceder 150 caracteres.');
                hasErrors = true;
            }

            return !hasErrors;
        }

        function createCustomerDraftIssueState() {
            var hasSelectedCustomer = parseInteger(selectedCustomerInput.value, 0) > 0;
            if (hasSelectedCustomer) {
                return {
                    hasBlockingErrors: false,
                    warningMessage: '',
                    shouldDisplayWarning: false,
                    shouldDisableSave: false
                };
            }

            var documentNumber = getNormalizedFieldValueByMode(newCustomerDocumentInput);
            var businessName = getNormalizedFieldValueByMode(newCustomerNameInput);
            var phone = getNormalizedFieldValueByMode(newCustomerPhoneInput);
            var email = getNormalizedFieldValueByMode(newCustomerEmailInput);
            var address = getNormalizedFieldValueByMode(newCustomerAddressInput);
            var hasDraftData = documentNumber.length > 0
                || businessName.length > 0
                || phone.length > 0
                || email.length > 0
                || address.length > 0;

            var customerIdError = getValidationMessage(createSaleForm, selectedCustomerInput.name);
            var documentError = getValidationMessage(createSaleForm, newCustomerDocumentInput.name);
            var businessNameError = getValidationMessage(createSaleForm, newCustomerNameInput.name);
            var phoneError = getValidationMessage(createSaleForm, newCustomerPhoneInput.name);
            var emailError = getValidationMessage(createSaleForm, newCustomerEmailInput.name);
            var addressError = getValidationMessage(createSaleForm, newCustomerAddressInput.name);
            var hasServerErrors = customerIdError.length > 0
                || documentError.length > 0
                || businessNameError.length > 0
                || phoneError.length > 0
                || emailError.length > 0
                || addressError.length > 0;

            if (hasServerErrors) {
                var serverWarningMessage = customerIdError
                    || documentError
                    || businessNameError
                    || phoneError
                    || emailError
                    || addressError
                    || 'Se detectaron datos incorrectos en el cliente en borrador. Revísalos antes de guardar la venta.';

                return {
                    hasBlockingErrors: true,
                    warningMessage: serverWarningMessage,
                    shouldDisplayWarning: true,
                    shouldDisableSave: true
                };
            }

            if (!hasDraftData) {
                return {
                    hasBlockingErrors: true,
                    warningMessage: 'Debes seleccionar o registrar un cliente antes de guardar la venta.',
                    shouldDisplayWarning: customerValidationVisible,
                    shouldDisableSave: customerValidationVisible
                };
            }

            var emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
            if (documentNumber.length === 0
                || documentNumber.length > 20
                || !/^([0-9A-Za-z-]{5,20}|0)$/.test(documentNumber)
                || businessName.length === 0
                || businessName.length > 150
                || phone.length > 20
                || email.length > 100
                || (email.length > 0 && !emailPattern.test(email))
                || address.length > 150) {
                return {
                    hasBlockingErrors: true,
                    warningMessage: 'Se detectaron datos incorrectos en el cliente en borrador. Revísalos antes de guardar la venta.',
                    shouldDisplayWarning: customerValidationVisible,
                    shouldDisableSave: customerValidationVisible
                };
            }

            return {
                hasBlockingErrors: false,
                warningMessage: '',
                shouldDisplayWarning: false,
                shouldDisableSave: false
            };
        }

        function updateSelectedCustomerWarning(customerIssueState) {
            if (!customerIssueState.shouldDisplayWarning) {
                selectedCustomerCard.classList.remove('is-warning');
                selectedCustomerWarningIcon.classList.add('d-none');
                selectedCustomerWarningIcon.setAttribute('data-bs-title', '');
                var hiddenTooltip = bootstrap.Tooltip.getInstance(selectedCustomerWarningIcon);
                if (hiddenTooltip) {
                    hiddenTooltip.hide();
                }

                return;
            }

            selectedCustomerCard.classList.add('is-warning');
            selectedCustomerWarningIcon.classList.remove('d-none');
            selectedCustomerWarningIcon.setAttribute('data-bs-title', customerIssueState.warningMessage);
            bootstrap.Tooltip.getOrCreateInstance(selectedCustomerWarningIcon, {
                container: 'body',
                trigger: 'hover focus'
            });
        }

        function clearEmptyOptionalCustomerErrors() {
            if (getNormalizedFieldValueByMode(newCustomerPhoneInput).length === 0) {
                clearFieldValidationState(createSaleForm, newCustomerPhoneInput);
            }

            if (getNormalizedFieldValueByMode(newCustomerEmailInput).length === 0) {
                clearFieldValidationState(createSaleForm, newCustomerEmailInput);
            }

            if (getNormalizedFieldValueByMode(newCustomerAddressInput).length === 0) {
                clearFieldValidationState(createSaleForm, newCustomerAddressInput);
            }
        }

        function updateSelectedCustomerSummary(customerIssueState) {
            var resolvedCustomerIssueState = customerIssueState || createCustomerDraftIssueState();
            var selectedCustomer = getSelectedCustomerOption(customerOptionsContainer, selectedCustomerInput.value);
            if (selectedCustomer) {
                selectedCustomerCard.classList.remove('is-draft');
                selectedCustomerTitle.textContent = selectedCustomer.businessName;
                selectedCustomerMeta.textContent = selectedCustomer.documentNumber;
                updateSelectedCustomerWarning(resolvedCustomerIssueState);
                return;
            }

            var draftDocument = newCustomerDocumentInput.value.trim();
            var draftName = newCustomerNameInput.value.trim();
            if (draftDocument || draftName) {
                selectedCustomerCard.classList.add('is-draft');
                selectedCustomerTitle.textContent = draftName || 'Cliente nuevo en borrador';

                if (draftDocument) {
                    selectedCustomerMeta.textContent = draftDocument;
                    updateSelectedCustomerWarning(resolvedCustomerIssueState);
                    return;
                }

                selectedCustomerMeta.textContent = 'Se registrará junto con la venta.';
                updateSelectedCustomerWarning(resolvedCustomerIssueState);
                return;
            }

            selectedCustomerCard.classList.remove('is-draft');
            selectedCustomerTitle.textContent = 'Esperando cliente...';
            selectedCustomerMeta.textContent = 'Selecciona uno de la lista o usa el botón + para registrar uno nuevo.';
            updateSelectedCustomerWarning(resolvedCustomerIssueState);
        }

        function syncCustomerDraftState() {
            clearEmptyOptionalCustomerErrors();
            updateSelectedCustomerSummary(createCustomerDraftIssueState());
            notifyStateChanged();
        }

        function openCustomerDraftModal() {
            shouldReturnToSaleModal = true;

            newSaleModalElement.addEventListener('hidden.bs.modal', function handleSaleHidden() {
                newSaleModalElement.removeEventListener('hidden.bs.modal', handleSaleHidden);
                newCustomerDraftModal.show();
            });

            newSaleModal.hide();
        }

        function closeCustomerDraftModal(returnToSaleModal) {
            shouldReturnToSaleModal = returnToSaleModal === true;
            newCustomerDraftModal.hide();
        }

        bindCustomerSelection(customerOptionsContainer, selectedCustomerInput, function () {
            syncCustomerDraftState();
        });

        customerSearchButton.addEventListener('click', function () {
            runCustomerSearch(customerSearchUrl, customerSearchInput.value, customerOptionsContainer, selectedCustomerInput, syncCustomerDraftState);
        });

        customerSearchInput.addEventListener('keydown', function (event) {
            if (event.key !== 'Enter') {
                return;
            }

            event.preventDefault();
            runCustomerSearch(customerSearchUrl, customerSearchInput.value, customerOptionsContainer, selectedCustomerInput, syncCustomerDraftState);
        });

        bindResetToAllOnEmpty(customerSearchInput, function () {
            runCustomerSearch(customerSearchUrl, '', customerOptionsContainer, selectedCustomerInput, syncCustomerDraftState);
        });

        openNewCustomerDraftButton.addEventListener('click', function () {
            selectedCustomerInput.value = '0';
            markSelectedOption(customerOptionsContainer, '[data-sale-customer-option="true"]', 'data-customer-id', '');
            syncCustomerDraftState();
            openCustomerDraftModal();
        });

        applyNewCustomerDraftButton.addEventListener('click', function () {
            if (!validateNewCustomerDraft()) {
                return;
            }

            selectedCustomerInput.value = '0';
            markSelectedOption(customerOptionsContainer, '[data-sale-customer-option="true"]', 'data-customer-id', '');
            syncCustomerDraftState();
            closeCustomerDraftModal(true);
        });

        backNewCustomerDraftButton.addEventListener('click', function () {
            closeCustomerDraftModal(true);
        });

        discardNewCustomerDraftButton.addEventListener('click', function () {
            selectedCustomerInput.value = '0';
            newCustomerDocumentInput.value = '';
            newCustomerNameInput.value = '';
            newCustomerPhoneInput.value = '';
            newCustomerEmailInput.value = '';
            newCustomerAddressInput.value = '';
            clearNewCustomerDraftValidation();
            markSelectedOption(customerOptionsContainer, '[data-sale-customer-option="true"]', 'data-customer-id', '');
            syncCustomerDraftState();
            closeCustomerDraftModal(true);
        });

        closeNewCustomerDraftModalButton.addEventListener('click', function () {
            clearNewCustomerDraftValidation();
            closeCustomerDraftModal(false);
        });

        newCustomerDocumentInput.addEventListener('input', function () {
            clearFieldValidationState(createSaleForm, newCustomerDocumentInput);
            syncCustomerDraftState();
        });
        newCustomerNameInput.addEventListener('input', function () {
            clearFieldValidationState(createSaleForm, newCustomerNameInput);
            syncCustomerDraftState();
        });
        newCustomerPhoneInput.addEventListener('input', function () {
            clearFieldValidationState(createSaleForm, newCustomerPhoneInput);
            syncCustomerDraftState();
        });
        newCustomerEmailInput.addEventListener('input', function () {
            clearFieldValidationState(createSaleForm, newCustomerEmailInput);
            syncCustomerDraftState();
        });
        newCustomerAddressInput.addEventListener('input', function () {
            clearFieldValidationState(createSaleForm, newCustomerAddressInput);
            syncCustomerDraftState();
        });

        newCustomerDraftModalElement.addEventListener('hidden.bs.modal', function () {
            if (!shouldReturnToSaleModal) {
                return;
            }

            newSaleModal.show();
        });

        return {
            getIssueState: createCustomerDraftIssueState,
            isDraftModalOpen: function () {
                return newCustomerDraftModalElement.classList.contains('show');
            },
            showValidation: function () {
                customerValidationVisible = true;
            },
            syncState: syncCustomerDraftState
        };
    }

    window.salesPageCreateCustomer = {
        createCustomerDraftController: createCustomerDraftController
    };
})();
