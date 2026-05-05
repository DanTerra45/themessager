(function () {
    'use strict';

    var stockLimitMessage = 'No puedes agregar más unidades de ese producto porque alcanzaste el stock disponible.';

    function ready(callback) {
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', callback);
            return;
        }

        callback();
    }

    function parseInteger(value, fallbackValue) {
        var parsedValue = Number.parseInt(value, 10);
        if (Number.isNaN(parsedValue)) {
            return fallbackValue;
        }

        return parsedValue;
    }

    function parseDecimal(value, fallbackValue) {
        var parsedValue = Number.parseFloat(value);
        if (Number.isNaN(parsedValue)) {
            return fallbackValue;
        }

        return parsedValue;
    }

    function normalizeText(value) {
        return String(value || '').trim().replace(/\s+/g, ' ');
    }

    function escapeHtml(value) {
        return String(value == null ? '' : value)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#039;');
    }

    function formatMoney(value) {
        return Number(value || 0).toLocaleString('es-BO', {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2
        });
    }

    function ensureToastContainer() {
        var existingContainer = document.querySelector('.app-toast-container');
        if (existingContainer) {
            return existingContainer;
        }

        var container = document.createElement('section');
        container.className = 'toast-container position-fixed bottom-0 end-0 p-3 app-toast-container';
        container.setAttribute('aria-live', 'polite');
        container.setAttribute('aria-atomic', 'true');
        document.body.appendChild(container);
        return container;
    }

    function showToastMessages(messages, isError) {
        var normalizedMessages = (messages || [])
            .map(normalizeText)
            .filter(function (message) { return message.length > 0; });

        if (normalizedMessages.length === 0) {
            return;
        }

        var container = ensureToastContainer();
        var toast = document.createElement('section');
        var iconClass = isError ? 'bi-exclamation-lg' : 'bi-check-lg';
        var iconToneClass = isError ? 'app-toast-icon-error' : 'app-toast-icon-success';
        var title = isError ? 'Error:' : 'Aviso:';
        var body = '<strong class="me-2">' + title + '</strong>';

        if (normalizedMessages.length === 1) {
            body += escapeHtml(normalizedMessages[0]);
        } else {
            body += '<span>Revisa los datos indicados.</span><ul class="mb-0 mt-2 ps-3">';
            normalizedMessages.forEach(function (message) {
                body += '<li>' + escapeHtml(message) + '</li>';
            });
            body += '</ul>';
        }

        toast.className = 'toast align-items-center border-0 app-toast';
        toast.setAttribute('role', 'alert');
        toast.setAttribute('aria-live', 'assertive');
        toast.setAttribute('aria-atomic', 'true');
        toast.setAttribute('data-bs-delay', isError ? '6500' : '4000');
        toast.innerHTML =
            '<section class="d-flex position-relative">' +
            '<section class="toast-icon d-flex align-items-center justify-content-center app-toast-icon ' + iconToneClass + '">' +
            '<i class="bi ' + iconClass + '"></i>' +
            '</section>' +
            '<section class="toast-body app-toast-body">' + body + '</section>' +
            '<button type="button" class="btn-close me-2 m-auto position-absolute top-0 end-0 mt-2 app-toast-close" data-bs-dismiss="toast" aria-label="Cerrar"></button>' +
            '</section>';

        container.appendChild(toast);

        if (typeof bootstrap === 'undefined' || typeof bootstrap.Toast === 'undefined') {
            return;
        }

        var toastInstance = bootstrap.Toast.getOrCreateInstance(toast);
        toast.addEventListener('hidden.bs.toast', function () {
            toast.remove();
        });
        toastInstance.show();
    }

    function readInitialState() {
        var stateElement = document.getElementById('salesCreateInitialState');
        if (!stateElement) {
            return { customers: [], products: [] };
        }

        try {
            return JSON.parse(stateElement.textContent || '{}');
        } catch {
            return { customers: [], products: [] };
        }
    }

    function normalizeCustomer(customer) {
        return {
            id: parseInteger(customer.id, 0),
            ciNit: customer.ciNit || customer.documentNumber || '',
            businessName: customer.businessName || ''
        };
    }

    function normalizeProduct(product) {
        return {
            productId: parseInteger(product.productId || product.id, 0),
            productName: product.productName || product.name || '',
            lotCode: product.lotCode || product.batch || '',
            availableStock: parseInteger(product.availableStock || product.stock, 0),
            unitPrice: parseDecimal(product.unitPrice || product.price, 0)
        };
    }

    function createRequestUrl(baseUrl, searchTerm) {
        var requestUrl = new URL(baseUrl, window.location.origin);
        requestUrl.searchParams.set('searchTerm', searchTerm || '');
        return requestUrl.toString();
    }

    async function readApiResponse(response) {
        var payload = null;
        try {
            payload = await response.json();
        } catch {
            payload = null;
        }

        if (payload) {
            return payload;
        }

        return {
            success: false,
            data: null,
            errors: ['El servicio no devolvió una respuesta válida.']
        };
    }

    function getFirstCustomer(customers) {
        var finalConsumer = customers.find(function (customer) {
            return customer.ciNit === '0';
        });

        if (finalConsumer) {
            return finalConsumer;
        }

        return customers.length > 0 ? customers[0] : null;
    }

    function createIssueState(lines) {
        var issuesByProductId = {};
        var firstIssueProductId = 0;

        lines.forEach(function (line) {
            var message = '';
            if (line.quantity <= 0) {
                message = 'La cantidad debe ser mayor que cero.';
            } else if (line.availableStock > 0 && line.quantity > line.availableStock) {
                message = stockLimitMessage;
            }

            if (message.length === 0) {
                return;
            }

            issuesByProductId[String(line.productId)] = message;
            if (firstIssueProductId <= 0) {
                firstIssueProductId = line.productId;
            }
        });

        return {
            issuesByProductId: issuesByProductId,
            firstIssueProductId: firstIssueProductId,
            hasIssues: Object.keys(issuesByProductId).length > 0
        };
    }

    function initializeWarningTooltips(container) {
        if (typeof bootstrap === 'undefined' || typeof bootstrap.Tooltip === 'undefined') {
            return;
        }

        container.querySelectorAll('.sale-warning-tooltip[data-bs-toggle="tooltip"]').forEach(function (tooltipElement) {
            bootstrap.Tooltip.getOrCreateInstance(tooltipElement, {
                container: 'body',
                trigger: 'hover focus'
            });
        });
    }

    ready(function () {
        var root = document.getElementById('salesCreateRoot');
        if (!root) {
            return;
        }

        document.body.classList.add('sale-create-modal-open');

        var initialState = readInitialState();
        var elements = {
            form: document.getElementById('salesCreateForm'),
            customerSearchInput: document.getElementById('saleCustomerSearchInput'),
            customerSearchButton: document.getElementById('searchCustomersButton'),
            customerResultsList: document.getElementById('customerResultsList'),
            selectedCustomerCard: document.getElementById('selectedCustomerCard'),
            selectedCustomerTitle: document.getElementById('selectedCustomerTitle'),
            selectedCustomerMeta: document.getElementById('selectedCustomerMeta'),
            customerToolsPanel: document.getElementById('customerToolsPanel'),
            toggleCustomerSearchButton: document.getElementById('toggleCustomerSearchButton'),
            openNewCustomerPanelButton: document.getElementById('openNewCustomerPanelButton'),
            newCustomerPanel: document.getElementById('newCustomerPanel'),
            hideNewCustomerPanelButton: document.getElementById('hideNewCustomerPanelButton'),
            discardNewCustomerButton: document.getElementById('discardNewCustomerButton'),
            useNewCustomerButton: document.getElementById('useNewCustomerButton'),
            newCustomerCiNit: document.getElementById('newCustomerCiNit'),
            newCustomerBusinessName: document.getElementById('newCustomerBusinessName'),
            newCustomerPhone: document.getElementById('newCustomerPhone'),
            newCustomerEmail: document.getElementById('newCustomerEmail'),
            newCustomerAddress: document.getElementById('newCustomerAddress'),
            channelInput: document.getElementById('saleChannelInput'),
            paymentMethodInput: document.getElementById('salePaymentMethodInput'),
            paymentMethodLabel: document.getElementById('salePaymentMethodLabel'),
            paymentMethodOptions: document.querySelectorAll('[data-payment-method]'),
            productSearchInput: document.getElementById('saleProductSearchInput'),
            productSearchButton: document.getElementById('searchProductsButton'),
            productOptionsList: document.getElementById('productOptionsList'),
            linesTableBody: document.getElementById('saleDraftLinesTableBody'),
            total: document.getElementById('saleDraftTotal'),
            openConfirmSaleButton: document.getElementById('openConfirmSaleButton'),
            confirmRegisterSaleButton: document.getElementById('confirmRegisterSaleButton'),
            closeConfirmSaleButton: document.getElementById('closeConfirmSaleButton'),
            returnToSaleDraftButton: document.getElementById('returnToSaleDraftButton'),
            closeLinks: root.querySelectorAll('[data-sale-modal-close]')
        };

        var confirmSaleModalElement = document.getElementById('confirmSaleModal');
        var confirmSaleModal = typeof bootstrap !== 'undefined' && bootstrap.Modal
            ? bootstrap.Modal.getOrCreateInstance(confirmSaleModalElement, { backdrop: false, keyboard: false })
            : null;

        var customers = (initialState.customers || []).map(normalizeCustomer);
        var products = (initialState.products || []).map(normalizeProduct);
        var defaultCustomer = getFirstCustomer(customers);
        var state = {
            customers: customers,
            products: products,
            selectedCustomerId: defaultCustomer ? defaultCustomer.id : 0,
            selectedCustomer: defaultCustomer,
            newCustomer: null,
            lines: [],
            submitAttempted: false
        };

        function showToastError(messages) {
            showToastMessages(messages, true);
        }

        function clearToastState() {
            return;
        }

        function closeSaleModal(targetUrl) {
            if (root.classList.contains('is-closing')) {
                return;
            }

            if (confirmSaleModal) {
                confirmSaleModal.hide();
            }

            root.classList.remove('is-confirming');
            root.classList.add('is-closing');
            document.body.classList.remove('sale-create-modal-open');
            window.setTimeout(function () {
                window.location.href = targetUrl || root.dataset.salesIndexUrl || '/Sales';
            }, 160);
        }

        function showSaleConfirmation() {
            root.classList.add('is-confirming');

            if (!confirmSaleModal) {
                registerSale();
                return;
            }

            window.setTimeout(function () {
                confirmSaleModal.show();
            }, 140);
        }

        function returnToSaleDraft() {
            if (confirmSaleModal) {
                confirmSaleModal.hide();
            }

            root.classList.remove('is-confirming');
        }

        function getSelectedCustomer() {
            if (state.newCustomer) {
                return {
                    title: state.newCustomer.businessName,
                    meta: state.newCustomer.ciNit,
                    isDraft: true
                };
            }

            var customer = state.selectedCustomer || state.customers.find(function (currentCustomer) {
                return currentCustomer.id === state.selectedCustomerId;
            });

            if (!customer) {
                return null;
            }

            return {
                title: customer.businessName,
                meta: customer.ciNit,
                isDraft: false
            };
        }

        function renderSelectedCustomer() {
            var selectedCustomer = getSelectedCustomer();
            elements.selectedCustomerCard.classList.remove('is-draft', 'is-warning');

            if (selectedCustomer) {
                elements.selectedCustomerTitle.textContent = selectedCustomer.title;
                elements.selectedCustomerMeta.textContent = selectedCustomer.meta;
                if (selectedCustomer.isDraft) {
                    elements.selectedCustomerCard.classList.add('is-draft');
                }
                return;
            }

            elements.selectedCustomerTitle.textContent = 'Esperando cliente...';
            elements.selectedCustomerMeta.textContent = 'Selecciona uno de la lista o usa el botón + para registrar uno nuevo.';
            if (state.submitAttempted) {
                elements.selectedCustomerCard.classList.add('is-warning');
            }
        }

        function renderCustomers() {
            if (state.customers.length === 0) {
                elements.customerResultsList.innerHTML = '<article class="sale-product-option-empty">No hay clientes que coincidan con la búsqueda actual.</article>';
                return;
            }

            var markup = '';
            state.customers.forEach(function (customer) {
            var optionClasses = 'sale-customer-option';
                if (!state.newCustomer && customer.id === state.selectedCustomerId) {
                    optionClasses += ' is-selected';
                }

                markup += '<button type="button" class="' + optionClasses + '" data-customer-id="' + escapeHtml(customer.id) + '">';
                markup += '<span class="sale-customer-option-name">' + escapeHtml(customer.businessName) + '</span>';
                markup += '<span class="sale-customer-option-meta">' + escapeHtml(customer.ciNit) + '</span>';
                markup += '</button>';
            });

            elements.customerResultsList.innerHTML = markup;
        }

        function renderProducts() {
            var issueState = createIssueState(state.lines);

            if (state.products.length === 0) {
                elements.productOptionsList.innerHTML = '<article class="sale-product-option-empty">No hay productos que coincidan con la búsqueda actual.</article>';
                return;
            }

            var markup = '';
            state.products.forEach(function (product, index) {
                var optionClasses = 'sale-product-option';
                var productIssue = issueState.issuesByProductId[String(product.productId)] || '';
                if (productIssue.length > 0) {
                    optionClasses += ' sale-product-option-warning';
                }

                markup += '<button type="button" class="' + optionClasses + '" data-product-id="' + escapeHtml(product.productId) + '" data-product-index="' + escapeHtml(index) + '">';
                markup += '<span class="sale-product-option-grid">';
                markup += '<span class="sale-product-option-name">' + escapeHtml(product.productName) + '</span>';
                markup += '<span class="sale-product-option-lot">' + escapeHtml(product.lotCode) + '</span>';
                markup += '<span class="sale-product-option-stock"><mark class="status-badge" data-status="' + (product.availableStock <= 5 ? 'danger' : 'good') + '">' + escapeHtml(product.availableStock) + '</mark>';
                if (productIssue.length > 0) {
                    markup += ' <span class="info-tooltip sale-warning-tooltip" data-bs-toggle="tooltip" data-bs-custom-class="sale-stock-tooltip" data-bs-title="' + escapeHtml(productIssue) + '" tabindex="0"><i class="bi bi-exclamation-triangle-fill"></i></span>';
                }
                markup += '</span>';
                markup += '<span class="sale-product-option-price">' + escapeHtml(formatMoney(product.unitPrice)) + ' Bs.</span>';
                markup += '</span>';
                markup += '</button>';
            });

            elements.productOptionsList.innerHTML = markup;
            initializeWarningTooltips(elements.productOptionsList);
        }

        function renderLines() {
            var issueState = createIssueState(state.lines);

            if (state.lines.length === 0) {
                elements.linesTableBody.innerHTML = '<tr><td colspan="6" class="text-center text-muted py-4">Aún no agregaste productos a la venta.</td></tr>';
                elements.total.textContent = 'Total: 0,00 Bs.';
                return;
            }

            var total = 0;
            var markup = '';
            state.lines.forEach(function (line, index) {
                var amount = line.quantity > 0 ? line.quantity * line.unitPrice : 0;
                var inputClasses = 'form-control text-center sale-line-quantity-input';
                var lineIssue = issueState.issuesByProductId[String(line.productId)] || '';
                total += amount;

                if (lineIssue.length > 0) {
                    inputClasses += ' sale-line-quantity-input-invalid';
                }

                markup += '<tr>';
                markup += '<td class="sale-draft-product-cell">' + escapeHtml(line.productName) + '</td>';
                markup += '<td class="sale-draft-batch-cell">' + escapeHtml(line.lotCode) + '</td>';
                markup += '<td class="sale-draft-quantity-cell">';
                markup += '<input type="number" min="1" max="' + escapeHtml(line.availableStock) + '" value="' + escapeHtml(line.quantity) + '" class="' + inputClasses + '" data-line-index="' + escapeHtml(index) + '" title="' + escapeHtml(lineIssue) + '" />';
                markup += '</td>';
                markup += '<td class="sale-draft-price-cell">' + escapeHtml(formatMoney(line.unitPrice)) + ' Bs.</td>';
                markup += '<td class="sale-draft-price-cell fw-bold">' + escapeHtml(formatMoney(amount)) + ' Bs.</td>';
                markup += '<td class="sale-draft-action-cell">';
                markup += '<button type="button" class="b-btn b-btn-danger sale-remove-line-button" data-remove-line-index="' + escapeHtml(index) + '" title="Quitar producto">';
                markup += '<i class="bi bi-dash-lg"></i>';
                markup += '</button>';
                markup += '</td>';
                markup += '</tr>';
            });

            elements.linesTableBody.innerHTML = markup;
            elements.total.textContent = 'Total: ' + formatMoney(total) + ' Bs.';
        }

        function renderAll() {
            renderSelectedCustomer();
            renderCustomers();
            renderProducts();
            renderLines();
        }

        function clearFieldError(fieldName) {
            var fieldError = elements.newCustomerPanel.querySelector('[data-field-error-for="' + fieldName + '"]');
            if (!fieldError) {
                return;
            }

            fieldError.classList.add('d-none');
            fieldError.classList.remove('field-validation-error');
            fieldError.removeAttribute('data-error-message');
        }

        function setFieldError(fieldName, message) {
            var fieldError = elements.newCustomerPanel.querySelector('[data-field-error-for="' + fieldName + '"]');
            if (!fieldError) {
                return;
            }

            fieldError.classList.remove('d-none');
            fieldError.classList.add('field-validation-error');
            fieldError.setAttribute('data-error-message', message);
        }

        function clearNewCustomerErrors() {
            ['newCustomerCiNit', 'newCustomerBusinessName', 'newCustomerPhone', 'newCustomerEmail', 'newCustomerAddress'].forEach(clearFieldError);
        }

        function validateNewCustomer() {
            clearNewCustomerErrors();

            var customer = {
                ciNit: normalizeText(elements.newCustomerCiNit.value),
                businessName: normalizeText(elements.newCustomerBusinessName.value),
                phone: normalizeText(elements.newCustomerPhone.value),
                email: normalizeText(elements.newCustomerEmail.value),
                address: normalizeText(elements.newCustomerAddress.value)
            };
            var errors = [];

            if (!customer.ciNit) {
                errors.push('El CI/NIT es obligatorio.');
                setFieldError('newCustomerCiNit', 'El CI/NIT es obligatorio.');
            } else if (!/^([0-9A-Za-z-]{5,20}|0)$/.test(customer.ciNit)) {
                errors.push('El CI/NIT debe ser 0 o tener entre 5 y 20 caracteres válidos.');
                setFieldError('newCustomerCiNit', 'El CI/NIT debe ser 0 o tener entre 5 y 20 caracteres válidos.');
            }

            if (!customer.businessName) {
                errors.push('La razón social es obligatoria.');
                setFieldError('newCustomerBusinessName', 'La razón social es obligatoria.');
            }

            if (customer.email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(customer.email)) {
                errors.push('El correo no tiene un formato válido.');
                setFieldError('newCustomerEmail', 'El correo no tiene un formato válido.');
            }

            return {
                customer: customer,
                errors: errors
            };
        }

        function addProduct(product) {
            var existingLine = state.lines.find(function (line) {
                return line.productId === product.productId;
            });

            if (existingLine) {
                if (existingLine.availableStock > 0 && existingLine.quantity >= existingLine.availableStock) {
                    return;
                }

                existingLine.quantity += 1;
                renderAll();
                return;
            }

            state.lines.push({
                productId: product.productId,
                productName: product.productName,
                lotCode: product.lotCode,
                availableStock: product.availableStock,
                unitPrice: product.unitPrice,
                quantity: 1
            });
            renderAll();
        }

        function scrollProductIssueIntoView() {
            var issueState = createIssueState(state.lines);
            if (!issueState.firstIssueProductId) {
                return;
            }

            var option = elements.productOptionsList.querySelector('[data-product-id="' + issueState.firstIssueProductId + '"]');
            if (!option) {
                return;
            }

            option.scrollIntoView({
                behavior: 'smooth',
                block: 'nearest',
                inline: 'nearest'
            });
        }

        function validateSale() {
            var errors = [];
            var channel = normalizeText(elements.channelInput.value);
            var paymentMethod = normalizeText(elements.paymentMethodInput.value);
            var issueState = createIssueState(state.lines);

            if (!channel) {
                errors.push('El canal es obligatorio.');
            }

            if (!paymentMethod) {
                errors.push('El método de pago es obligatorio.');
            }

            if (!state.newCustomer && state.selectedCustomerId <= 0) {
                errors.push('Debes seleccionar un cliente o registrar uno nuevo.');
            }

            if (state.lines.length === 0) {
                errors.push('Debes agregar al menos un producto a la venta.');
            }

            Object.keys(issueState.issuesByProductId).forEach(function (productId) {
                errors.push(issueState.issuesByProductId[productId]);
            });

            return errors.filter(function (message, index, source) {
                return source.indexOf(message) === index;
            });
        }

        function buildRegisterRequest() {
            return {
                customerId: state.newCustomer ? null : state.selectedCustomerId,
                newCustomer: state.newCustomer,
                channel: normalizeText(elements.channelInput.value),
                paymentMethod: normalizeText(elements.paymentMethodInput.value),
                lines: state.lines.map(function (line) {
                    return {
                        productId: line.productId,
                        lotCode: line.lotCode,
                        quantity: line.quantity
                    };
                })
            };
        }

        async function runCustomerSearch() {
            var response = await fetch(createRequestUrl(root.dataset.customersUrl, elements.customerSearchInput.value), {
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            });
            var payload = await readApiResponse(response);
            if (!payload.success) {
                showToastError(payload.errors || ['No se pudo buscar clientes.']);
                return;
            }

            state.customers = (payload.data || []).map(normalizeCustomer);
            if (!state.newCustomer && state.selectedCustomerId <= 0) {
                var nextCustomer = getFirstCustomer(state.customers);
                state.selectedCustomerId = nextCustomer ? nextCustomer.id : 0;
                state.selectedCustomer = nextCustomer;
            }

            renderAll();
        }

        async function runProductSearch() {
            var response = await fetch(createRequestUrl(root.dataset.productsUrl, elements.productSearchInput.value), {
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            });
            var payload = await readApiResponse(response);
            if (!payload.success) {
                showToastError(payload.errors || ['No se pudo buscar productos.']);
                return;
            }

            state.products = (payload.data || []).map(normalizeProduct);
            renderAll();
            scrollProductIssueIntoView();
        }

        async function registerSale() {
            elements.confirmRegisterSaleButton.disabled = true;
            elements.confirmRegisterSaleButton.textContent = 'Guardando...';

            var tokenField = elements.form.querySelector('input[name="__RequestVerificationToken"]');
            var headers = {
                'Content-Type': 'application/json',
                'X-Requested-With': 'XMLHttpRequest'
            };

            if (tokenField) {
                headers.RequestVerificationToken = tokenField.value;
            }

            var response = await fetch(root.dataset.registerUrl, {
                method: 'POST',
                headers: headers,
                body: JSON.stringify(buildRegisterRequest())
            });
            var payload = await readApiResponse(response);

            elements.confirmRegisterSaleButton.disabled = false;
            elements.confirmRegisterSaleButton.textContent = 'Confirmar';

            if (!payload.success || !payload.data) {
                returnToSaleDraft();
                showToastError(payload.errors || ['No se pudo registrar la venta.']);
                return;
            }

            window.location.href = root.dataset.receiptBaseUrl + '/' + payload.data.id;
        }

        elements.customerResultsList.addEventListener('click', function (event) {
            var option = event.target.closest('[data-customer-id]');
            if (!option) {
                return;
            }

            state.selectedCustomerId = parseInteger(option.getAttribute('data-customer-id'), 0);
            state.selectedCustomer = state.customers.find(function (customer) {
                return customer.id === state.selectedCustomerId;
            }) || null;
            state.newCustomer = null;
            elements.newCustomerPanel.classList.add('d-none');
            if (elements.customerToolsPanel) {
                elements.customerToolsPanel.classList.add('d-none');
            }
            clearToastState();
            renderAll();
        });

        if (elements.toggleCustomerSearchButton && elements.customerToolsPanel) {
            elements.toggleCustomerSearchButton.addEventListener('click', function () {
                elements.customerToolsPanel.classList.toggle('d-none');
                elements.newCustomerPanel.classList.add('d-none');
            });
        }

        elements.openNewCustomerPanelButton.addEventListener('click', function () {
            if (elements.customerToolsPanel) {
                elements.customerToolsPanel.classList.add('d-none');
            }
            elements.newCustomerPanel.classList.remove('d-none');
            elements.newCustomerCiNit.focus();
        });

        elements.hideNewCustomerPanelButton.addEventListener('click', function () {
            elements.newCustomerPanel.classList.add('d-none');
            clearNewCustomerErrors();
        });

        elements.discardNewCustomerButton.addEventListener('click', function () {
            elements.newCustomerCiNit.value = '';
            elements.newCustomerBusinessName.value = '';
            elements.newCustomerPhone.value = '';
            elements.newCustomerEmail.value = '';
            elements.newCustomerAddress.value = '';
            state.newCustomer = null;
            state.selectedCustomer = state.customers.find(function (customer) {
                return customer.id === state.selectedCustomerId;
            }) || null;
            elements.newCustomerPanel.classList.add('d-none');
            clearNewCustomerErrors();
            renderAll();
        });

        elements.useNewCustomerButton.addEventListener('click', function () {
            var validation = validateNewCustomer();
            if (validation.errors.length > 0) {
                showToastError(validation.errors);
                return;
            }

            state.newCustomer = validation.customer;
            state.selectedCustomerId = 0;
            state.selectedCustomer = null;
            elements.newCustomerPanel.classList.add('d-none');
            if (elements.customerToolsPanel) {
                elements.customerToolsPanel.classList.add('d-none');
            }
            clearToastState();
            renderAll();
        });

        elements.paymentMethodOptions.forEach(function (option) {
            option.addEventListener('click', function () {
                var paymentMethod = normalizeText(option.getAttribute('data-payment-method'));
                if (!paymentMethod) {
                    return;
                }

                elements.paymentMethodInput.value = paymentMethod;
                if (elements.paymentMethodLabel) {
                    elements.paymentMethodLabel.textContent = paymentMethod;
                }

                elements.paymentMethodOptions.forEach(function (currentOption) {
                    currentOption.classList.remove('is-selected');
                });
                option.classList.add('is-selected');
                clearToastState();
            });
        });

        elements.closeLinks.forEach(function (link) {
            link.addEventListener('click', function (event) {
                if (event.button !== 0 || event.ctrlKey || event.metaKey || event.shiftKey || event.altKey) {
                    return;
                }

                event.preventDefault();
                closeSaleModal(link.href);
            });
        });

        elements.customerSearchButton.addEventListener('click', runCustomerSearch);
        elements.customerSearchInput.addEventListener('keydown', function (event) {
            if (event.key !== 'Enter') {
                return;
            }

            event.preventDefault();
            runCustomerSearch();
        });
        elements.customerSearchInput.addEventListener('input', function () {
            if (normalizeText(elements.customerSearchInput.value).length === 0) {
                runCustomerSearch();
            }
        });

        elements.productOptionsList.addEventListener('click', function (event) {
            var option = event.target.closest('[data-product-index]');
            if (!option) {
                return;
            }

            var productIndex = parseInteger(option.getAttribute('data-product-index'), -1);
            var product = productIndex >= 0 && productIndex < state.products.length
                ? state.products[productIndex]
                : null;

            if (!product) {
                return;
            }

            addProduct(product);
            clearToastState();
        });

        elements.productSearchButton.addEventListener('click', runProductSearch);
        elements.productSearchInput.addEventListener('keydown', function (event) {
            if (event.key !== 'Enter') {
                return;
            }

            event.preventDefault();
            runProductSearch();
        });
        elements.productSearchInput.addEventListener('input', function () {
            if (normalizeText(elements.productSearchInput.value).length === 0) {
                runProductSearch();
            }
        });

        elements.linesTableBody.addEventListener('click', function (event) {
            var removeButton = event.target.closest('[data-remove-line-index]');
            if (!removeButton) {
                return;
            }

            var lineIndex = parseInteger(removeButton.getAttribute('data-remove-line-index'), -1);
            if (lineIndex < 0 || lineIndex >= state.lines.length) {
                return;
            }

            state.lines.splice(lineIndex, 1);
            clearToastState();
            renderAll();
        });

        elements.linesTableBody.addEventListener('change', function (event) {
            var quantityInput = event.target.closest('[data-line-index]');
            if (!quantityInput) {
                return;
            }

            var lineIndex = parseInteger(quantityInput.getAttribute('data-line-index'), -1);
            if (lineIndex < 0 || lineIndex >= state.lines.length) {
                return;
            }

            state.lines[lineIndex].quantity = parseInteger(quantityInput.value, 0);
            renderAll();
            scrollProductIssueIntoView();
        });

        elements.openConfirmSaleButton.addEventListener('click', function () {
            state.submitAttempted = true;
            var errors = validateSale();
            renderAll();

            if (errors.length > 0) {
                showToastError(errors);
                scrollProductIssueIntoView();
                return;
            }

            clearToastState();
            showSaleConfirmation();
        });

        if (elements.returnToSaleDraftButton) {
            elements.returnToSaleDraftButton.addEventListener('click', returnToSaleDraft);
        }

        if (elements.closeConfirmSaleButton) {
            elements.closeConfirmSaleButton.addEventListener('click', function () {
                closeSaleModal(root.dataset.salesIndexUrl || '/Sales');
            });
        }

        elements.confirmRegisterSaleButton.addEventListener('click', registerSale);

        renderAll();
    });
})();
