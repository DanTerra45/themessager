(function () {
    function shouldApplyPattern(element) {
        var type = (element.getAttribute('type') || '').toLowerCase();
        return type === '' || type === 'text' || type === 'search' || type === 'tel' || type === 'email' || type === 'url' || type === 'password';
    }

    function setAttributeIfMissing(element, attributeName, attributeValue) {
        if (element.hasAttribute(attributeName)) {
            return;
        }

        if (attributeValue === null || attributeValue === undefined || attributeValue === '') {
            return;
        }

        element.setAttribute(attributeName, attributeValue);
    }

    function syncFieldAttributes(element) {
        if (!element || element.hasAttribute('readonly') || element.hasAttribute('disabled')) {
            return;
        }

        var type = (element.getAttribute('type') || '').toLowerCase();
        if (type === 'hidden') {
            return;
        }

        if (element.hasAttribute('data-val-required')) {
            setAttributeIfMissing(element, 'required', 'required');
        }

        var maxLengthValue = element.getAttribute('data-val-length-max');
        var minLengthValue = element.getAttribute('data-val-length-min');
        var rangeMinValue = element.getAttribute('data-val-range-min');
        var rangeMaxValue = element.getAttribute('data-val-range-max');
        var regexPatternValue = element.getAttribute('data-val-regex-pattern');

        setAttributeIfMissing(element, 'maxlength', maxLengthValue);
        setAttributeIfMissing(element, 'minlength', minLengthValue);
        setAttributeIfMissing(element, 'min', rangeMinValue);
        setAttributeIfMissing(element, 'max', rangeMaxValue);

        if (shouldApplyPattern(element)) {
            setAttributeIfMissing(element, 'pattern', regexPatternValue);
        }
    }

    function syncNativeValidationAttributes(root) {
        var scope = root || document;
        var fields = scope.querySelectorAll('input[data-val], textarea[data-val], select[data-val]');
        for (var index = 0; index < fields.length; index++) {
            syncFieldAttributes(fields[index]);
        }
    }

    document.addEventListener('DOMContentLoaded', function () {
        syncNativeValidationAttributes(document);
    });
})();
