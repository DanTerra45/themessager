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

            if (currentOption.getAttribute(attributeName) !== String(selectedValue)) {
                return;
            }

            currentOption.classList.add('is-selected');
            currentOption.setAttribute('aria-pressed', 'true');
        });
    }

    function bindCustomerSelection(container, onCustomerSelected) {
        container.addEventListener('click', function (event) {
            var option = event.target.closest('[data-sale-customer-option="true"]');
            if (!option) {
                return;
            }

            var selectedCustomerId = option.getAttribute('data-customer-id');
            if (!selectedCustomerId) {
                return;
            }

            onCustomerSelected({
                customerId: parseInteger(selectedCustomerId, 0),
                documentNumber: option.getAttribute('data-customer-document') || '',
                businessName: option.getAttribute('data-customer-name') || ''
            });
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
            container.innerHTML = '<article class="sale-product-option-empty">No hay clientes que coincidan con la busqueda actual.</article>';
            return;
        }

        var markup = '';
        for (var index = 0; index < customers.length; index += 1) {
            var customer = customers[index];
            var optionClasses = 'sale-customer-option';
            var ariaPressed = 'false';

            if (String(customer.id) === String(selectedCustomerInput.value)) {
                optionClasses += ' is-selected';
                ariaPressed = 'true';
            }

            markup += '<button type="button" class="' + optionClasses + '" data-sale-customer-option="true" data-customer-id="' + escapeHtml(customer.id) + '" data-customer-document="' + escapeHtml(customer.documentNumber) + '" data-customer-name="' + escapeHtml(customer.businessName) + '" aria-pressed="' + ariaPressed + '">';
            markup += '<span class="sale-customer-option-name">' + escapeHtml(customer.businessName) + '</span>';
            markup += '<span class="sale-customer-option-meta">' + escapeHtml(customer.documentNumber) + '</span>';
            markup += '</button>';
        }

        container.innerHTML = markup;
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

            if (currentOption.getAttribute('data-customer-id') !== String(selectedCustomerId)) {
                return;
            }

            option = currentOption;
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

        var message = validationSpan.getAttribute('data-error-message');
        if (!message || message.trim().length === 0) {
            message = validationSpan.textContent;
        }

        if (!message || message.trim().length === 0) {
            return 'Valor invalido.';
        }

        return message.trim();
    }

    function createCustomerDraftController(options) {
        var createSaleForm = options.createSaleForm;
        var customerSearchInput = options.customerSearchInput;
        var customerSearchButton = options.customerSearchButton;
        var customerOptionsContainer = options.customerOptionsContainer;
        var selectedCustomerInput = options.selectedCustomerInput;
        var customerDropdownToggle = options.customerDropdownToggle;
        var customerDropdownMenu = options.customerDropdownMenu;
        var newSaleModalElement = options.newSaleModalElement;
        var newCustomerDraftModalElement = options.newCustomerDraftModalElement;
        var selectedCustomerCard = options.selectedCustomerCard;
        var selectedCustomerTitle = options.selectedCustomerTitle;
        var selectedCustomerMeta = options.selectedCustomerMeta;
        var selectedCustomerWarningIcon = options.selectedCustomerWarningIcon;
        var selectedCustomerTriggerIcon = options.selectedCustomerTriggerIcon;
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
        var onStateChanged = options.onStateChanged;

        if (typeof onStateChanged !== 'function') {
            onStateChanged = function () { };
        }

        if (!customerDropdownMenu) {
            return null;
        }

        var newSaleModal = bootstrap.Modal.getOrCreateInstance(newSaleModalElement);
        var newCustomerDraftModal = bootstrap.Modal.getOrCreateInstance(newCustomerDraftModalElement);
        var customerDropdown = bootstrap.Dropdown.getOrCreateInstance(customerDropdownToggle);
        var shouldReturnToSaleModal = false;
        var customerValidationVisible = false;
        var selectedCustomerSnapshot = null;
        var defaultCustomerSnapshot = null;
        var customerContextBeforeDraftModal = null;
        var draftModalCloseAction = 'restore';

        [newCustomerDocumentInput, newCustomerNameInput, newCustomerPhoneInput, newCustomerEmailInput, newCustomerAddressInput].forEach(function (field) {
            field.removeAttribute('required');
            field.removeAttribute('pattern');
            field.removeAttribute('minlength');
        });

        function notifyStateChanged() {
            onStateChanged();
        }

        function clearSelectedCustomerValidation() {
            clearFieldValidationState(createSaleForm, selectedCustomerInput);
        }

        function clearNewCustomerDraftValidation() {
            clearFieldValidationState(createSaleForm, newCustomerDocumentInput);
            clearFieldValidationState(createSaleForm, newCustomerNameInput);
            clearFieldValidationState(createSaleForm, newCustomerPhoneInput);
            clearFieldValidationState(createSaleForm, newCustomerEmailInput);
            clearFieldValidationState(createSaleForm, newCustomerAddressInput);
        }

        function clearNewCustomerDraftValues() {
            newCustomerDocumentInput.value = '';
            newCustomerNameInput.value = '';
            newCustomerPhoneInput.value = '';
            newCustomerEmailInput.value = '';
            newCustomerAddressInput.value = '';
        }

        function readDraftCustomerValues() {
            return {
                documentNumber: newCustomerDocumentInput.value || '',
                businessName: newCustomerNameInput.value || '',
                phone: newCustomerPhoneInput.value || '',
                email: newCustomerEmailInput.value || '',
                address: newCustomerAddressInput.value || ''
            };
        }

        function restoreDraftCustomerValues(values) {
            var normalizedValues = values;
            if (!normalizedValues) {
                normalizedValues = {};
            }

            newCustomerDocumentInput.value = normalizedValues.documentNumber || '';
            newCustomerNameInput.value = normalizedValues.businessName || '';
            newCustomerPhoneInput.value = normalizedValues.phone || '';
            newCustomerEmailInput.value = normalizedValues.email || '';
            newCustomerAddressInput.value = normalizedValues.address || '';
        }

        function hasDraftCustomerData() {
            return getNormalizedFieldValueByMode(newCustomerDocumentInput).length > 0
                || getNormalizedFieldValueByMode(newCustomerNameInput).length > 0
                || getNormalizedFieldValueByMode(newCustomerPhoneInput).length > 0
                || getNormalizedFieldValueByMode(newCustomerEmailInput).length > 0
                || getNormalizedFieldValueByMode(newCustomerAddressInput).length > 0;
        }

        function hasSelectedCustomer() {
            return parseInteger(selectedCustomerInput.value, 0) > 0;
        }

        function buildCustomerSnapshot(customer) {
            if (!customer) {
                return null;
            }

            var customerId = 0;
            if (customer.customerId !== undefined && customer.customerId !== null) {
                customerId = parseInteger(customer.customerId, 0);
            }

            if (customerId <= 0 && customer.id !== undefined && customer.id !== null) {
                customerId = parseInteger(customer.id, 0);
            }

            if (customerId <= 0) {
                return null;
            }

            var documentNumber = '';
            if (customer.documentNumber) {
                documentNumber = String(customer.documentNumber);
            }

            var businessName = '';
            if (customer.businessName) {
                businessName = String(customer.businessName);
            }

            return {
                customerId: customerId,
                documentNumber: documentNumber,
                businessName: businessName
            };
        }

        function cloneCustomerSnapshot(snapshot) {
            if (!snapshot) {
                return null;
            }

            return {
                customerId: snapshot.customerId,
                documentNumber: snapshot.documentNumber,
                businessName: snapshot.businessName
            };
        }

        function rememberSelectedCustomerSnapshot(customer) {
            var snapshot = buildCustomerSnapshot(customer);
            if (!snapshot) {
                return;
            }

            selectedCustomerSnapshot = snapshot;
        }

        function clearSelectedCustomerSnapshot() {
            selectedCustomerSnapshot = null;
        }

        function rememberDefaultCustomerSnapshot(customer) {
            var snapshot = buildCustomerSnapshot(customer);
            if (!snapshot) {
                return;
            }

            if (snapshot.documentNumber !== '0') {
                return;
            }

            defaultCustomerSnapshot = snapshot;
        }

        function getSelectedCustomerSnapshot() {
            var selectedCustomerId = parseInteger(selectedCustomerInput.value, 0);
            if (selectedCustomerId <= 0) {
                return null;
            }

            var selectedCustomer = getSelectedCustomerOption(customerOptionsContainer, selectedCustomerId);
            if (selectedCustomer) {
                rememberSelectedCustomerSnapshot(selectedCustomer);
                rememberDefaultCustomerSnapshot(selectedCustomer);
                return cloneCustomerSnapshot(selectedCustomerSnapshot);
            }

            if (selectedCustomerSnapshot && selectedCustomerSnapshot.customerId === selectedCustomerId) {
                return cloneCustomerSnapshot(selectedCustomerSnapshot);
            }

            return null;
        }

        function getDefaultCustomerOption() {
            var customerOptions = customerOptionsContainer.querySelectorAll('[data-sale-customer-option="true"]');
            for (var index = 0; index < customerOptions.length; index += 1) {
                var currentOption = customerOptions[index];
                if ((currentOption.getAttribute('data-customer-document') || '') !== '0') {
                    continue;
                }

                var defaultCustomer = {
                    customerId: parseInteger(currentOption.getAttribute('data-customer-id'), 0),
                    documentNumber: currentOption.getAttribute('data-customer-document') || '',
                    businessName: currentOption.getAttribute('data-customer-name') || ''
                };

                rememberDefaultCustomerSnapshot(defaultCustomer);
                return defaultCustomer;
            }

            if (defaultCustomerSnapshot) {
                return cloneCustomerSnapshot(defaultCustomerSnapshot);
            }

            return null;
        }

        function captureCustomerContext() {
            var selectedCustomer = getSelectedCustomerSnapshot();
            return {
                selectedCustomerId: selectedCustomerInput.value || '0',
                selectedCustomerSnapshot: cloneCustomerSnapshot(selectedCustomer),
                draftValues: readDraftCustomerValues()
            };
        }

        function restoreCustomerContext(context) {
            var draftValues = null;
            if (context) {
                draftValues = context.draftValues;
            }

            restoreDraftCustomerValues(draftValues);

            selectedCustomerInput.value = '0';
            clearSelectedCustomerSnapshot();

            if (context && context.selectedCustomerSnapshot) {
                selectedCustomerSnapshot = cloneCustomerSnapshot(context.selectedCustomerSnapshot);
            }

            if (context && context.selectedCustomerId) {
                selectedCustomerInput.value = String(context.selectedCustomerId);
            }

            markSelectedOption(customerOptionsContainer, '[data-sale-customer-option="true"]', 'data-customer-id', selectedCustomerInput.value);

            if (!hasSelectedCustomer() && !hasDraftCustomerData()) {
                applyDefaultCustomerSelection();
            }

            clearSelectedCustomerValidation();
            clearNewCustomerDraftValidation();
        }

        function restoreSelectionAfterDraftDiscard() {
            clearNewCustomerDraftValues();
            clearNewCustomerDraftValidation();
            clearSelectedCustomerValidation();

            selectedCustomerInput.value = '0';
            clearSelectedCustomerSnapshot();

            if (customerContextBeforeDraftModal && customerContextBeforeDraftModal.selectedCustomerSnapshot) {
                selectedCustomerSnapshot = cloneCustomerSnapshot(customerContextBeforeDraftModal.selectedCustomerSnapshot);
            }

            if (customerContextBeforeDraftModal && customerContextBeforeDraftModal.selectedCustomerId) {
                selectedCustomerInput.value = String(customerContextBeforeDraftModal.selectedCustomerId);
            }

            markSelectedOption(customerOptionsContainer, '[data-sale-customer-option="true"]', 'data-customer-id', selectedCustomerInput.value);

            if (!hasSelectedCustomer()) {
                applyDefaultCustomerSelection();
            }
        }

        function applyExistingCustomerSelection(customer) {
            var customerSnapshot = buildCustomerSnapshot(customer);
            if (!customerSnapshot) {
                return false;
            }

            selectedCustomerInput.value = String(customerSnapshot.customerId);
            rememberSelectedCustomerSnapshot(customerSnapshot);
            rememberDefaultCustomerSnapshot(customerSnapshot);
            clearSelectedCustomerValidation();
            clearNewCustomerDraftValidation();
            markSelectedOption(customerOptionsContainer, '[data-sale-customer-option="true"]', 'data-customer-id', customerSnapshot.customerId);
            customerDropdown.hide();
            return true;
        }

        function applyDefaultCustomerSelection() {
            if (hasDraftCustomerData()) {
                return false;
            }

            if (hasSelectedCustomer()) {
                return false;
            }

            var defaultCustomer = getDefaultCustomerOption();
            if (!defaultCustomer) {
                return false;
            }

            return applyExistingCustomerSelection(defaultCustomer);
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
                setFieldValidationState(createSaleForm, newCustomerDocumentInput, 'El CI/NIT debe ser 0 o tener entre 5 y 20 caracteres validos.');
                hasErrors = true;
            }

            if (businessName.length === 0) {
                setFieldValidationState(createSaleForm, newCustomerNameInput, 'La razon social es obligatoria.');
                hasErrors = true;
            } else if (businessName.length > 150) {
                setFieldValidationState(createSaleForm, newCustomerNameInput, 'La razon social no puede exceder 150 caracteres.');
                hasErrors = true;
            }

            if (phone.length > 20) {
                setFieldValidationState(createSaleForm, newCustomerPhoneInput, 'El telefono no puede exceder 20 caracteres.');
                hasErrors = true;
            }

            if (email.length > 100) {
                setFieldValidationState(createSaleForm, newCustomerEmailInput, 'El correo no puede exceder 100 caracteres.');
                hasErrors = true;
            } else if (email.length > 0 && !emailPattern.test(email)) {
                setFieldValidationState(createSaleForm, newCustomerEmailInput, 'El correo no tiene un formato valido.');
                hasErrors = true;
            }

            if (address.length > 150) {
                setFieldValidationState(createSaleForm, newCustomerAddressInput, 'La direccion no puede exceder 150 caracteres.');
                hasErrors = true;
            }

            return !hasErrors;
        }

        function createCustomerDraftIssueState() {
            if (hasSelectedCustomer()) {
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
                var serverWarningMessage = customerIdError;
                if (serverWarningMessage.length === 0) {
                    serverWarningMessage = documentError;
                }

                if (serverWarningMessage.length === 0) {
                    serverWarningMessage = businessNameError;
                }

                if (serverWarningMessage.length === 0) {
                    serverWarningMessage = phoneError;
                }

                if (serverWarningMessage.length === 0) {
                    serverWarningMessage = emailError;
                }

                if (serverWarningMessage.length === 0) {
                    serverWarningMessage = addressError;
                }

                if (serverWarningMessage.length === 0) {
                    serverWarningMessage = 'Se detectaron datos incorrectos en el cliente en borrador. Revisalos antes de guardar la venta.';
                }

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
                    warningMessage: 'Se detectaron datos incorrectos en el cliente en borrador. Revisalos antes de guardar la venta.',
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
            var tooltip = bootstrap.Tooltip.getInstance(selectedCustomerWarningIcon);
            if (!customerIssueState.shouldDisplayWarning) {
                selectedCustomerCard.classList.remove('is-warning');
                selectedCustomerWarningIcon.classList.add('d-none');
                selectedCustomerWarningIcon.setAttribute('data-bs-title', '');

                if (tooltip) {
                    tooltip.dispose();
                }

                return;
            }

            selectedCustomerCard.classList.add('is-warning');
            selectedCustomerWarningIcon.classList.remove('d-none');
            selectedCustomerWarningIcon.setAttribute('data-bs-title', customerIssueState.warningMessage);

            if (tooltip) {
                tooltip.dispose();
            }

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
            var resolvedCustomerIssueState = customerIssueState;
            if (!resolvedCustomerIssueState) {
                resolvedCustomerIssueState = createCustomerDraftIssueState();
            }

            var selectedCustomer = getSelectedCustomerSnapshot();
            if (selectedCustomer) {
                selectedCustomerCard.classList.remove('is-draft');
                selectedCustomerTitle.textContent = selectedCustomer.businessName;
                selectedCustomerMeta.textContent = selectedCustomer.documentNumber;
                selectedCustomerTriggerIcon.className = 'bi bi-pencil-square';
                updateSelectedCustomerWarning(resolvedCustomerIssueState);
                return;
            }

            var draftDocument = newCustomerDocumentInput.value.trim();
            var draftName = newCustomerNameInput.value.trim();
            if (draftDocument.length > 0 || draftName.length > 0) {
                selectedCustomerCard.classList.add('is-draft');

                if (draftName.length > 0) {
                    selectedCustomerTitle.textContent = draftName;
                } else {
                    selectedCustomerTitle.textContent = 'Cliente nuevo en borrador';
                }

                if (draftDocument.length > 0) {
                    selectedCustomerMeta.textContent = draftDocument;
                } else {
                    selectedCustomerMeta.textContent = 'Se registrara junto con la venta.';
                }

                selectedCustomerTriggerIcon.className = 'bi bi-pencil-square';
                updateSelectedCustomerWarning(resolvedCustomerIssueState);
                return;
            }

            selectedCustomerCard.classList.remove('is-draft');
            selectedCustomerTitle.textContent = 'Esperando cliente...';
            selectedCustomerMeta.textContent = 'Selecciona un cliente para la venta.';
            selectedCustomerTriggerIcon.className = 'bi bi-person-plus';
            updateSelectedCustomerWarning(resolvedCustomerIssueState);
        }

        function syncCustomerDraftState() {
            clearEmptyOptionalCustomerErrors();
            applyDefaultCustomerSelection();
            markSelectedOption(customerOptionsContainer, '[data-sale-customer-option="true"]', 'data-customer-id', selectedCustomerInput.value);

            var customerIssueState = createCustomerDraftIssueState();
            updateSelectedCustomerSummary(customerIssueState);
            notifyStateChanged();
        }

        function openCustomerDraftModal() {
            customerContextBeforeDraftModal = captureCustomerContext();
            draftModalCloseAction = 'restore';
            shouldReturnToSaleModal = true;
            customerDropdown.hide();

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

        async function executeCustomerSearch(term) {
            try {
                await runCustomerSearch(customerSearchUrl, term, customerOptionsContainer, selectedCustomerInput, syncCustomerDraftState);
            } catch (error) {
                return;
            }
        }

        bindCustomerSelection(customerOptionsContainer, function (selectedCustomer) {
            applyExistingCustomerSelection(selectedCustomer);
            syncCustomerDraftState();
        });

        customerSearchButton.addEventListener('click', function () {
            executeCustomerSearch(customerSearchInput.value);
        });

        customerSearchInput.addEventListener('keydown', function (event) {
            if (event.key !== 'Enter') {
                return;
            }

            event.preventDefault();
            executeCustomerSearch(customerSearchInput.value);
        });

        bindResetToAllOnEmpty(customerSearchInput, function () {
            executeCustomerSearch('');
        });

        customerDropdownToggle.addEventListener('shown.bs.dropdown', function () {
            window.setTimeout(function () {
                customerSearchInput.focus();
                customerSearchInput.select();
            }, 0);
        });

        openNewCustomerDraftButton.addEventListener('click', function () {
            openCustomerDraftModal();
        });

        applyNewCustomerDraftButton.addEventListener('click', function () {
            if (!validateNewCustomerDraft()) {
                return;
            }

            draftModalCloseAction = 'apply';
            selectedCustomerInput.value = '0';
            clearSelectedCustomerSnapshot();
            clearSelectedCustomerValidation();
            markSelectedOption(customerOptionsContainer, '[data-sale-customer-option="true"]', 'data-customer-id', '');
            syncCustomerDraftState();
            closeCustomerDraftModal(true);
        });

        backNewCustomerDraftButton.addEventListener('click', function () {
            draftModalCloseAction = 'restore';
            closeCustomerDraftModal(true);
        });

        closeNewCustomerDraftModalButton.addEventListener('click', function () {
            draftModalCloseAction = 'restore';
            closeCustomerDraftModal(true);
        });

        discardNewCustomerDraftButton.addEventListener('click', function () {
            draftModalCloseAction = 'discard';
            restoreSelectionAfterDraftDiscard();
            syncCustomerDraftState();
            closeCustomerDraftModal(true);
        });

        newCustomerDocumentInput.addEventListener('input', function () {
            clearFieldValidationState(createSaleForm, newCustomerDocumentInput);
        });

        newCustomerNameInput.addEventListener('input', function () {
            clearFieldValidationState(createSaleForm, newCustomerNameInput);
        });

        newCustomerPhoneInput.addEventListener('input', function () {
            clearFieldValidationState(createSaleForm, newCustomerPhoneInput);
        });

        newCustomerEmailInput.addEventListener('input', function () {
            clearFieldValidationState(createSaleForm, newCustomerEmailInput);
        });

        newCustomerAddressInput.addEventListener('input', function () {
            clearFieldValidationState(createSaleForm, newCustomerAddressInput);
        });

        newCustomerDraftModalElement.addEventListener('hide.bs.modal', function () {
            if (draftModalCloseAction !== 'restore') {
                return;
            }

            clearNewCustomerDraftValidation();
            restoreCustomerContext(customerContextBeforeDraftModal);
            syncCustomerDraftState();
        });

        newCustomerDraftModalElement.addEventListener('hidden.bs.modal', function () {
            if (shouldReturnToSaleModal) {
                newSaleModal.show();
            }

            customerContextBeforeDraftModal = null;
            draftModalCloseAction = 'restore';
        });

        syncCustomerDraftState();

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
