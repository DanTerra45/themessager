(function () {
    function showModalIfNeeded(flag, modalId) {
        if (!flag || typeof bootstrap === 'undefined') {
            return;
        }

        var modalElement = document.getElementById(modalId);
        if (!modalElement) {
            return;
        }

        bootstrap.Modal.getOrCreateInstance(modalElement).show();
    }

    function bindCancelModal() {
        var cancelModal = document.getElementById('cancelSaleModal');
        if (!cancelModal) {
            return;
        }

        cancelModal.addEventListener('show.bs.modal', function (event) {
            var trigger = event.relatedTarget;
            if (!trigger) {
                return;
            }

            var saleIdInput = document.getElementById('CancelRequest_SaleId');
            var saleCodeInput = document.getElementById('cancelSaleCode');
            var reasonInput = document.getElementById('CancelRequest_Reason');

            if (saleIdInput) {
                saleIdInput.value = trigger.getAttribute('data-sale-id') || '';
            }

            if (saleCodeInput) {
                saleCodeInput.value = trigger.getAttribute('data-sale-code') || '';
            }

            if (reasonInput && cancelModal.dataset.preserveReason !== 'true') {
                reasonInput.value = '';
            }
        });
    }

    function autoOpenReceipt(url) {
        if (!url) {
            return;
        }

        window.open(url, '_blank', 'noopener');

        if (typeof window.history.replaceState !== 'function') {
            return;
        }

        var currentUrl = new URL(window.location.href);
        currentUrl.searchParams.delete('AutoOpenReceiptSaleId');
        window.history.replaceState({}, document.title, currentUrl.toString());
    }

    function escapeHtml(value) {
        var normalizedValue = value;
        if (normalizedValue === null || normalizedValue === undefined) {
            normalizedValue = '';
        }

        return String(normalizedValue)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;');
    }

    function parseInteger(value, fallbackValue) {
        var parsedValue = parseInt(value, 10);
        if (Number.isNaN(parsedValue)) {
            return fallbackValue;
        }

        return parsedValue;
    }

    function parseDecimal(value) {
        var parsedValue = parseFloat(value);
        if (Number.isNaN(parsedValue)) {
            return 0;
        }

        return parsedValue;
    }

    function formatMoney(value) {
        return new Intl.NumberFormat('es-BO', {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2
        }).format(value);
    }

    function findFieldIndex(fieldName) {
        var match = /\[(\d+)\]/.exec(fieldName);
        if (!match || match.length < 2) {
            return -1;
        }

        return parseInteger(match[1], -1);
    }

    function readFieldValue(field) {
        if (!field) {
            return '';
        }

        return field.value || '';
    }

    function collapseWhitespace(value) {
        var trimmedValue = value.trim();
        if (trimmedValue.length === 0) {
            return trimmedValue;
        }

        return trimmedValue.replace(/\s+/g, ' ');
    }

    function getNormalizedFieldValueByMode(field) {
        var rawValue = readFieldValue(field);
        var mode = (field.getAttribute('data-normalize') || 'trim').toLowerCase();
        var normalizedValue = rawValue.trim();

        if (mode === 'collapse') {
            normalizedValue = collapseWhitespace(normalizedValue);
        }

        if (mode === 'trim-upper') {
            normalizedValue = normalizedValue.toUpperCase();
        }

        if (mode === 'collapse-upper') {
            normalizedValue = collapseWhitespace(normalizedValue).toUpperCase();
        }

        if (mode === 'trim-lower') {
            normalizedValue = normalizedValue.toLowerCase();
        }

        if (mode === 'collapse-lower') {
            normalizedValue = collapseWhitespace(normalizedValue).toLowerCase();
        }

        return normalizedValue;
    }

    function normalizeFieldValueByMode(field) {
        var normalizedValue = getNormalizedFieldValueByMode(field);
        if (field.value !== normalizedValue) {
            field.value = normalizedValue;
        }

        return normalizedValue;
    }

    function findValidationSpan(root, fieldName) {
        return root.querySelector("span[data-valmsg-for='" + fieldName + "']");
    }

    function setFieldValidationState(root, field, errorMessage) {
        var validationSpan = findValidationSpan(root, field.name);
        field.classList.add('input-validation-error');
        field.setAttribute('aria-invalid', 'true');

        if (!validationSpan) {
            return;
        }

        validationSpan.textContent = errorMessage;
        validationSpan.classList.remove('field-validation-valid');
        validationSpan.classList.add('field-validation-error');
        validationSpan.setAttribute('data-error-message', errorMessage);
        validationSpan.setAttribute('aria-label', errorMessage);
    }

    function clearFieldValidationState(root, field) {
        var validationSpan = findValidationSpan(root, field.name);
        field.classList.remove('input-validation-error');
        field.removeAttribute('aria-invalid');

        if (!validationSpan) {
            return;
        }

        validationSpan.textContent = '';
        validationSpan.classList.remove('field-validation-error');
        validationSpan.classList.add('field-validation-valid');
        validationSpan.removeAttribute('data-error-message');
        validationSpan.removeAttribute('aria-label');
    }

    window.salesPageShared = {
        showModalIfNeeded: showModalIfNeeded,
        bindCancelModal: bindCancelModal,
        autoOpenReceipt: autoOpenReceipt,
        escapeHtml: escapeHtml,
        parseInteger: parseInteger,
        parseDecimal: parseDecimal,
        formatMoney: formatMoney,
        findFieldIndex: findFieldIndex,
        readFieldValue: readFieldValue,
        getNormalizedFieldValueByMode: getNormalizedFieldValueByMode,
        normalizeFieldValueByMode: normalizeFieldValueByMode,
        findValidationSpan: findValidationSpan,
        setFieldValidationState: setFieldValidationState,
        clearFieldValidationState: clearFieldValidationState
    };
})();
