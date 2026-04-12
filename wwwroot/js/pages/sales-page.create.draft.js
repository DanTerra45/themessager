(function () {
    var shared = window.salesPageShared;
    if (!shared) {
        return;
    }

    var escapeHtml = shared.escapeHtml;
    var parseInteger = shared.parseInteger;
    var parseDecimal = shared.parseDecimal;
    var formatMoney = shared.formatMoney;
    var findFieldIndex = shared.findFieldIndex;
    var readFieldValue = shared.readFieldValue;

    var stockLimitMessage = 'No puedes agregar más unidades de ese producto porque alcanzaste el stock disponible.';

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

    function readDraftLines(formElement) {
        var lines = [];
        var productInputs = formElement.querySelectorAll("input[name^='SaleDraft.Lines'][name$='.ProductId']");

        productInputs.forEach(function (productInput) {
            var index = findFieldIndex(productInput.name);
            if (index < 0) {
                return;
            }

            var quantityInput = formElement.querySelector("input[name='SaleDraft.Lines[" + index + "].Quantity']");
            var productNameInput = formElement.querySelector("input[name='DraftLineDetails[" + index + "].ProductName']");
            var batchInput = formElement.querySelector("input[name='DraftLineDetails[" + index + "].Batch']");
            var unitPriceInput = formElement.querySelector("input[name='DraftLineDetails[" + index + "].UnitPrice']");
            var stockInput = formElement.querySelector("input[name='DraftLineDetails[" + index + "].Stock']");

            lines.push({
                productId: parseInteger(productInput.value, 0),
                productName: readFieldValue(productNameInput),
                batch: readFieldValue(batchInput),
                unitPrice: parseDecimal(readFieldValue(unitPriceInput)),
                stock: parseInteger(readFieldValue(stockInput), 0),
                quantity: parseInteger(readFieldValue(quantityInput), 1)
            });
        });

        return lines;
    }

    function createDraftIssueState(draftLines) {
        var invalidProductIds = {};
        var blockingMessage = '';
        var focusProductId = 0;

        for (var index = 0; index < draftLines.length; index += 1) {
            var line = draftLines[index];
            var lineMessage = '';

            if (line.quantity <= 0) {
                lineMessage = 'La cantidad debe ser mayor que cero.';
            }

            if (!lineMessage && line.stock > 0 && line.quantity > line.stock) {
                lineMessage = stockLimitMessage;
            }

            if (!lineMessage) {
                continue;
            }

            invalidProductIds[String(line.productId)] = lineMessage;

            if (blockingMessage.length === 0) {
                blockingMessage = lineMessage;
                focusProductId = line.productId;
            }
        }

        return {
            invalidProductIds: invalidProductIds,
            hasBlockingErrors: blockingMessage.length > 0,
            blockingMessage: blockingMessage,
            focusProductId: focusProductId
        };
    }

    function scrollProductOptionIntoView(container, productId) {
        if (!productId) {
            return;
        }

        var option = container.querySelector('[data-sale-product-option="true"][data-product-id="' + String(productId) + '"]');
        if (!option) {
            return;
        }

        option.scrollIntoView({
            behavior: 'smooth',
            block: 'nearest',
            inline: 'nearest'
        });
    }

    function bindProductSelection(container, selectedProductInput, onProductSelected) {
        container.addEventListener('click', function (event) {
            var option = event.target.closest('[data-sale-product-option="true"]');
            if (!option) {
                return;
            }

            var selectedProductId = option.getAttribute('data-product-id');
            if (!selectedProductId) {
                return;
            }

            selectedProductInput.value = selectedProductId;
            markSelectedOption(container, '[data-sale-product-option="true"]', 'data-product-id', selectedProductId);
            onProductSelected(option);
        });
    }

    function getSelectedProductOption(container, selectedProductId) {
        if (!selectedProductId) {
            return null;
        }

        var option = null;
        var productOptions = container.querySelectorAll('[data-sale-product-option="true"]');
        productOptions.forEach(function (currentOption) {
            if (option) {
                return;
            }

            if (currentOption.getAttribute('data-product-id') === String(selectedProductId)) {
                option = currentOption;
            }
        });

        if (!option) {
            return null;
        }

        return {
            productId: parseInteger(option.getAttribute('data-product-id'), 0),
            productName: option.getAttribute('data-product-name') || '',
            batch: option.getAttribute('data-product-batch') || '',
            unitPrice: parseDecimal(option.getAttribute('data-product-price')),
            stock: parseInteger(option.getAttribute('data-product-stock'), 0)
        };
    }

    function renderDraftHiddenFields(container, draftLines) {
        if (draftLines.length === 0) {
            container.innerHTML = '';
            return;
        }

        var markup = '';
        for (var index = 0; index < draftLines.length; index += 1) {
            var line = draftLines[index];
            markup += '<input type="hidden" name="DraftLineDetails[' + index + '].ProductId" value="' + escapeHtml(line.productId) + '" />';
            markup += '<input type="hidden" name="DraftLineDetails[' + index + '].ProductName" value="' + escapeHtml(line.productName) + '" />';
            markup += '<input type="hidden" name="DraftLineDetails[' + index + '].Batch" value="' + escapeHtml(line.batch) + '" />';
            markup += '<input type="hidden" name="DraftLineDetails[' + index + '].UnitPrice" value="' + escapeHtml(line.unitPrice) + '" />';
            markup += '<input type="hidden" name="DraftLineDetails[' + index + '].Stock" value="' + escapeHtml(line.stock) + '" />';
        }

        container.innerHTML = markup;
    }

    function renderDraftTable(container, draftLines, issueState) {
        if (draftLines.length === 0) {
            container.innerHTML = '<tr><td colspan="6" class="text-center text-muted py-4">Aún no agregaste productos a la venta.</td></tr>';
            return;
        }

        var markup = '';
        for (var index = 0; index < draftLines.length; index += 1) {
            var line = draftLines[index];
            var amount = line.unitPrice * line.quantity;
            var quantityInputClasses = 'form-control text-center sale-line-quantity-input';
            var productKey = String(line.productId);

            if (Object.prototype.hasOwnProperty.call(issueState.invalidProductIds, productKey)) {
                quantityInputClasses += ' sale-line-quantity-input-invalid';
            }

            markup += '<tr>';
            markup += '<td class="sale-draft-product-cell"><input type="hidden" name="SaleDraft.Lines[' + index + '].ProductId" value="' + escapeHtml(line.productId) + '" />' + escapeHtml(line.productName) + '</td>';
            markup += '<td class="sale-draft-batch-cell">' + escapeHtml(line.batch) + '</td>';
            markup += '<td class="sale-draft-quantity-cell">';
            markup += '<input type="number" name="SaleDraft.Lines[' + index + '].Quantity" value="' + escapeHtml(line.quantity) + '" class="' + quantityInputClasses + '" min="1" max="' + escapeHtml(line.stock) + '" data-draft-quantity-index="' + index + '" />';
            markup += '</td>';
            markup += '<td class="sale-draft-price-cell">' + escapeHtml(formatMoney(line.unitPrice)) + ' Bs.</td>';
            markup += '<td class="sale-draft-price-cell fw-bold">' + escapeHtml(formatMoney(amount)) + ' Bs.</td>';
            markup += '<td class="sale-draft-action-cell">';
            markup += '<button type="button" class="b-btn b-btn-danger sale-remove-line-button" data-draft-remove-product-id="' + escapeHtml(line.productId) + '">';
            markup += '<i class="bi bi-dash-lg"></i>';
            markup += '</button>';
            markup += '</td>';
            markup += '</tr>';
        }

        container.innerHTML = markup;
    }

    function updateDraftTotal(totalElement, draftLines) {
        var total = 0;
        for (var index = 0; index < draftLines.length; index += 1) {
            total += draftLines[index].unitPrice * draftLines[index].quantity;
        }

        totalElement.textContent = 'Total: ' + formatMoney(total) + ' Bs.';
    }

    async function runProductSearch(searchUrl, term, productOptionsContainer, selectedProductInput, issueState) {
        if (!searchUrl) {
            return;
        }

        var requestUrl = new URL(searchUrl, window.location.origin);
        requestUrl.searchParams.set('productSearchTerm', term);

        var response = await fetch(requestUrl.toString(), {
            headers: {
                'X-Requested-With': 'XMLHttpRequest'
            }
        });

        if (!response.ok) {
            return;
        }

        var products = await response.json();
        renderProductOptions(productOptionsContainer, products, selectedProductInput, issueState);
    }

    function renderProductOptions(container, products, selectedProductInput, issueState) {
        if (products.length === 0) {
            container.innerHTML = '<article class="sale-product-option-empty">No hay productos que coincidan con la búsqueda actual.</article>';
            selectedProductInput.value = '';
            return;
        }

        var selectedProductStillExists = false;
        var markup = '';
        for (var index = 0; index < products.length; index += 1) {
            var product = products[index];
            var optionClasses = 'sale-product-option';
            var ariaPressed = 'false';
            var productKey = String(product.id);
            var issueMessage = '';

            if (issueState && Object.prototype.hasOwnProperty.call(issueState.invalidProductIds, productKey)) {
                issueMessage = issueState.invalidProductIds[productKey];
                optionClasses += ' sale-product-option-warning';
            }

            if (String(product.id) === String(selectedProductInput.value)) {
                optionClasses += ' is-selected';
                ariaPressed = 'true';
                selectedProductStillExists = true;
            }

            markup += '<button type="button" class="' + optionClasses + '" data-sale-product-option="true" data-product-id="' + escapeHtml(product.id) + '" data-product-name="' + escapeHtml(product.name) + '" data-product-batch="' + escapeHtml(product.batch) + '" data-product-price="' + escapeHtml(product.price) + '" data-product-stock="' + escapeHtml(product.stock) + '" aria-pressed="' + ariaPressed + '">';
            markup += '<span class="sale-product-option-header"><span>' + escapeHtml(product.name) + '</span><span>' + escapeHtml(formatMoney(product.price)) + ' Bs.</span></span>';
            markup += '<span class="sale-product-option-meta"><span>Lote ' + escapeHtml(product.batch) + '</span><span class="sale-product-option-stock">Stock ' + escapeHtml(product.stock);
            if (issueMessage.length > 0) {
                markup += ' <span class="info-tooltip sale-warning-tooltip" data-bs-toggle="tooltip" data-bs-custom-class="sale-stock-tooltip" data-bs-title="' + escapeHtml(issueMessage) + '" tabindex="0"><i class="bi bi-exclamation-triangle-fill"></i></span>';
            }

            markup += '</span></span>';
            markup += '</button>';
        }

        container.innerHTML = markup;
        initializeSaleWarningTooltips(container);

        if (selectedProductStillExists) {
            return;
        }

        selectedProductInput.value = '';
    }

    function initializeSaleWarningTooltips(container) {
        if (typeof bootstrap === 'undefined' || typeof bootstrap.Tooltip === 'undefined') {
            return;
        }

        var tooltips = container.querySelectorAll('.sale-warning-tooltip[data-bs-toggle="tooltip"]');
        tooltips.forEach(function (tooltipElement) {
            bootstrap.Tooltip.getOrCreateInstance(tooltipElement, {
                container: 'body',
                trigger: 'hover focus'
            });
        });
    }

    function readVisibleProductOptions(container) {
        var products = [];
        var options = container.querySelectorAll('[data-sale-product-option="true"]');

        options.forEach(function (option) {
            products.push({
                id: parseInteger(option.getAttribute('data-product-id'), 0),
                name: option.getAttribute('data-product-name') || '',
                batch: option.getAttribute('data-product-batch') || '',
                price: parseDecimal(option.getAttribute('data-product-price')),
                stock: parseInteger(option.getAttribute('data-product-stock'), 0)
            });
        });

        return products;
    }

    function createSaleDraftLinesController(options) {
        var createSaleForm = options.createSaleForm;
        var productSearchInput = options.productSearchInput;
        var productSearchButton = options.productSearchButton;
        var productOptionsContainer = options.productOptionsContainer;
        var selectedProductInput = options.selectedProductInput;
        var draftHiddenFields = options.draftHiddenFields;
        var draftTableBody = options.draftTableBody;
        var draftTotalValue = options.draftTotalValue;
        var productSearchUrl = options.productSearchUrl || '';
        var onStateChanged = typeof options.onStateChanged === 'function' ? options.onStateChanged : function () { };
        var draftLines = readDraftLines(createSaleForm);

        function notifyStateChanged() {
            onStateChanged(getIssueState());
        }

        function getIssueState() {
            return createDraftIssueState(draftLines);
        }

        function renderState() {
            var issueState = getIssueState();

            renderDraftHiddenFields(draftHiddenFields, draftLines);
            renderDraftTable(draftTableBody, draftLines, issueState);
            renderProductOptions(productOptionsContainer, readVisibleProductOptions(productOptionsContainer), selectedProductInput, issueState);
            updateDraftTotal(draftTotalValue, draftLines);

            if (issueState.hasBlockingErrors) {
                scrollProductOptionIntoView(productOptionsContainer, issueState.focusProductId);
            }

            notifyStateChanged();
        }

        bindProductSelection(productOptionsContainer, selectedProductInput, function (option) {
            var selectedProduct = getSelectedProductOption(productOptionsContainer, option.getAttribute('data-product-id'));
            if (!selectedProduct) {
                return;
            }

            var existingLine = null;
            for (var index = 0; index < draftLines.length; index += 1) {
                if (draftLines[index].productId === selectedProduct.productId) {
                    existingLine = draftLines[index];
                    break;
                }
            }

            if (existingLine) {
                if (selectedProduct.stock > 0 && existingLine.quantity >= selectedProduct.stock) {
                    return;
                }

                existingLine.quantity += 1;
                existingLine.stock = selectedProduct.stock;
                renderState();
                return;
            }

            draftLines.push({
                productId: selectedProduct.productId,
                productName: selectedProduct.productName,
                batch: selectedProduct.batch,
                unitPrice: selectedProduct.unitPrice,
                stock: selectedProduct.stock,
                quantity: 1
            });
            renderState();
        });

        productSearchButton.addEventListener('click', function () {
            runProductSearch(productSearchUrl, productSearchInput.value, productOptionsContainer, selectedProductInput, getIssueState());
        });

        productSearchInput.addEventListener('keydown', function (event) {
            if (event.key !== 'Enter') {
                return;
            }

            event.preventDefault();
            runProductSearch(productSearchUrl, productSearchInput.value, productOptionsContainer, selectedProductInput, getIssueState());
        });

        bindResetToAllOnEmpty(productSearchInput, function () {
            runProductSearch(productSearchUrl, '', productOptionsContainer, selectedProductInput, getIssueState());
        });

        draftTableBody.addEventListener('click', function (event) {
            var removeButton = event.target.closest('[data-draft-remove-product-id]');
            if (!removeButton) {
                return;
            }

            var productIdToRemove = parseInteger(removeButton.getAttribute('data-draft-remove-product-id'), 0);
            if (productIdToRemove <= 0) {
                return;
            }

            draftLines = draftLines.filter(function (line) {
                return line.productId !== productIdToRemove;
            });
            renderState();
        });

        draftTableBody.addEventListener('change', function (event) {
            var quantityField = event.target.closest('[data-draft-quantity-index]');
            if (!quantityField) {
                return;
            }

            var lineIndex = parseInteger(quantityField.getAttribute('data-draft-quantity-index'), -1);
            if (lineIndex < 0 || lineIndex >= draftLines.length) {
                return;
            }

            var quantity = parseInteger(quantityField.value, 1);
            if (quantity <= 0) {
                quantity = 0;
            }

            draftLines[lineIndex].quantity = quantity;
            renderState();
        });

        return {
            getIssueState: getIssueState,
            renderState: renderState
        };
    }

    window.salesPageCreateDraft = {
        createSaleDraftLinesController: createSaleDraftLinesController
    };
})();
