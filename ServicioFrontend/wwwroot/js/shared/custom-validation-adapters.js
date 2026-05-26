(function ($) {
    if (!$ || !$.validator || !$.validator.unobtrusive) {
        return;
    }

    function setDynamicMessage(validator, element, ruleName, message) {
        if (!validator || !validator.settings || !validator.settings.messages) {
            return;
        }

        var elementName = '';
        if (element && element.name) {
            elementName = element.name;
        }
        if (!elementName) {
            return;
        }

        if (!validator.settings.messages[elementName]) {
            validator.settings.messages[elementName] = {};
        }

        validator.settings.messages[elementName][ruleName] = message;
    }

    $.validator.addMethod('ci', function (value, element, params) {
        var rawValue = '';
        if (value) {
            rawValue = value.toString().trim();
        }
        if (rawValue.length === 0) {
            setDynamicMessage(this, element, 'ci', params.requiredMessage);
            return false;
        }

        if (!/^\d+$/.test(rawValue)) {
            return false;
        }

        var parsedValue = parseInt(rawValue, 10);
        if (isNaN(parsedValue)) {
            return false;
        }

        return parsedValue >= params.min && parsedValue <= params.max;
    }, function (params, element) {
        var rawValue = $(element).val();
        if (!rawValue || rawValue.toString().trim().length === 0) {
            return params.requiredMessage;
        }

        return params.message;
    });

    $.validator.unobtrusive.adapters.add('ci', ['min', 'max', 'required'], function (options) {
        options.rules.ci = {
            min: parseInt(options.params.min, 10),
            max: parseInt(options.params.max, 10),
            requiredMessage: options.params.required,
            message: options.message
        };
        options.messages.ci = options.message;
    });

    $.validator.addMethod('positive', function (value, element, params) {
        var rawValue = '';
        if (value) {
            rawValue = value.toString().trim();
        }
        if (rawValue.length === 0) {
            setDynamicMessage(this, element, 'positive', params.requiredMessage);
            return false;
        }

        var normalizedValue = rawValue.replace(',', '.');
        var parsedValue = Number(normalizedValue);
        if (!isFinite(parsedValue)) {
            return false;
        }

        if (params.allowZero) {
            return parsedValue >= 0;
        }

        return parsedValue > 0;
    }, function (params, element) {
        var rawValue = $(element).val();
        if (!rawValue || rawValue.toString().trim().length === 0) {
            return params.requiredMessage;
        }

        return params.message;
    });

    $.validator.unobtrusive.adapters.add('positive', ['allowzero', 'required'], function (options) {
        var allowZeroFlag = options.params.allowzero;

        options.rules.positive = {
            allowZero: allowZeroFlag === true || allowZeroFlag === 'true',
            requiredMessage: options.params.required,
            message: options.message
        };
        options.messages.positive = options.message;
    });
})(window.jQuery);
