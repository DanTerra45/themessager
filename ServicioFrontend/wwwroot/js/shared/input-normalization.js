(function () {
    function collapseWhitespace(value) {
        var trimmedValue = value.trim();
        if (trimmedValue === '') {
            return trimmedValue;
        }

        return trimmedValue.replace(/\s+/g, ' ');
    }

    function getNormalizationMode(element) {
        if (!element) {
            return 'trim';
        }

        var mode = element.getAttribute('data-normalize');
        if (!mode) {
            return 'trim';
        }

        return mode.toLowerCase();
    }

    function shouldNormalizeField(element) {
        if (!element) {
            return false;
        }

        if (element.hasAttribute('readonly') || element.hasAttribute('disabled')) {
            return false;
        }

        var tagName = element.tagName.toLowerCase();
        if (tagName === 'textarea') {
            return true;
        }

        if (tagName !== 'input') {
            return false;
        }

        var type = (element.getAttribute('type') || 'text').toLowerCase();
        if (type === 'hidden' || type === 'password') {
            return false;
        }

        return type === 'text'
            || type === 'search'
            || type === 'email'
            || type === 'url'
            || type === 'tel';
    }

    function normalizeValue(value) {
        if (typeof value !== 'string') {
            return value;
        }

        return value.trim();
    }

    function normalizeValueByMode(value, mode) {
        var normalizedValue = normalizeValue(value);
        if (typeof normalizedValue !== 'string') {
            return normalizedValue;
        }

        if (mode === 'collapse') {
            return collapseWhitespace(normalizedValue);
        }

        if (mode === 'trim-upper') {
            return normalizedValue.toUpperCase();
        }

        if (mode === 'collapse-upper') {
            return collapseWhitespace(normalizedValue).toUpperCase();
        }

        if (mode === 'trim-lower') {
            return normalizedValue.toLowerCase();
        }

        if (mode === 'collapse-lower') {
            return collapseWhitespace(normalizedValue).toLowerCase();
        }

        return normalizedValue;
    }

    function normalizeFieldValue(element) {
        if (!shouldNormalizeField(element)) {
            return;
        }

        var mode = getNormalizationMode(element);
        var normalizedValue = normalizeValueByMode(element.value, mode);
        if (normalizedValue === element.value) {
            return;
        }

        element.value = normalizedValue;
        element.dispatchEvent(new Event('input', { bubbles: true }));
        element.dispatchEvent(new Event('change', { bubbles: true }));
    }

    function bindFieldNormalization(root) {
        var scope = root || document;
        var fields = scope.querySelectorAll('input, textarea');

        for (var index = 0; index < fields.length; index++) {
            var field = fields[index];
            field.addEventListener('blur', function (event) {
                normalizeFieldValue(event.target);
            }, true);
        }
    }

    function bindFormNormalization(root) {
        var scope = root || document;
        var forms = scope.querySelectorAll('form');

        for (var index = 0; index < forms.length; index++) {
            var form = forms[index];
            form.addEventListener('submit', function (event) {
                var fields = event.target.querySelectorAll('input, textarea');
                for (var fieldIndex = 0; fieldIndex < fields.length; fieldIndex++) {
                    normalizeFieldValue(fields[fieldIndex]);
                }
            }, true);
        }
    }

    document.addEventListener('DOMContentLoaded', function () {
        bindFieldNormalization(document);
        bindFormNormalization(document);
    });
})();
