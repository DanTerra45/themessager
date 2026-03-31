(function () {
    function normalizeMessage(rawMessage) {
        var message = (rawMessage || '').replace(/\s+/g, ' ').trim();
        if (message.length === 0) {
            return 'Valor inválido.';
        }

        var cleaned = message.replace(/^The\s+([A-Za-z0-9_.]+\.)?([A-Za-z0-9_]+)\s+field\s+/i, '');
        if (cleaned !== message) {
            cleaned = cleaned.charAt(0).toUpperCase() + cleaned.slice(1);
            return cleaned;
        }

        return message;
    }

    function resolveRelatedLabel(root, spanElement) {
        var directLabel = spanElement.closest('label');
        if (directLabel) {
            return directLabel;
        }

        var fieldPath = spanElement.getAttribute('data-valmsg-for');
        if (!fieldPath) {
            return null;
        }

        var fieldId = fieldPath.replace(/\./g, '_');
        var label = root.querySelector('label[for="' + fieldId + '"]');
        if (label) {
            return label;
        }

        var input = root.getElementById(fieldId);
        if (!input) {
            return null;
        }

        var localContainer = input.closest('.mb-3, .mb-4, .col-md-2, .col-md-4, .col-md-5, .col-md-6, .form-floating, .input-group');
        if (!localContainer) {
            return null;
        }

        return localContainer.querySelector('label');
    }

    function syncValidationTooltipMessages(scope) {
        var root = scope || document;
        var validationSpans = root.querySelectorAll('span[data-valmsg-for]');

        for (var index = 0; index < validationSpans.length; index++) {
            var span = validationSpans[index];
            var message = normalizeMessage(span.textContent);
            var hasError = span.classList.contains('field-validation-error');
            var relatedLabel = resolveRelatedLabel(root, span);
            var infoIcon = relatedLabel ? relatedLabel.querySelector('.info-tooltip') : null;

            if (hasError) {
                span.setAttribute('data-error-message', message);
                span.setAttribute('aria-label', message);
            } else {
                span.removeAttribute('data-error-message');
                span.removeAttribute('aria-label');
            }

            if (infoIcon) {
                infoIcon.classList.toggle('d-none', hasError);
                infoIcon.setAttribute('aria-hidden', hasError ? 'true' : 'false');
            }
        }
    }

    function collectValidationRoots() {
        var spans = document.querySelectorAll('span[data-valmsg-for]');
        var rootSet = new Set();

        for (var index = 0; index < spans.length; index++) {
            var span = spans[index];
            var root = span.closest('form');
            if (!root) {
                root = span.closest('.modal-content');
            }
            if (!root) {
                root = document.body;
            }

            rootSet.add(root);
        }

        if (rootSet.size === 0) {
            rootSet.add(document.body);
        }

        return Array.from(rootSet);
    }

    function createSyncScheduler() {
        var schedule = window.requestAnimationFrame || function (callback) {
            return window.setTimeout(callback, 16);
        };

        var isQueued = false;
        var pendingScope = null;
        return function (scope) {
            if (!pendingScope) {
                pendingScope = scope || document;
            } else if (pendingScope !== scope) {
                pendingScope = document;
            }

            if (isQueued) {
                return;
            }

            isQueued = true;
            schedule(function () {
                var targetScope = pendingScope || document;
                pendingScope = null;
                isQueued = false;
                syncValidationTooltipMessages(targetScope);
            });
        };
    }

    function bindValidationTooltipEvents(scheduleSync, roots) {
        var eventNames = ['input', 'change', 'blur', 'submit'];

        for (var index = 0; index < roots.length; index++) {
            var root = roots[index];
            var form = root && root.tagName === 'FORM' ? root : (root ? root.querySelector('form') : null);
            if (!form) {
                continue;
            }

            (function (observedForm) {
                for (var eventIndex = 0; eventIndex < eventNames.length; eventIndex++) {
                    var eventName = eventNames[eventIndex];
                    observedForm.addEventListener(eventName, function () {
                        scheduleSync(observedForm);
                    }, true);
                }
            })(form);
        }
    }

    function bindValidationTooltipObserver(scheduleSync, roots) {
        if (typeof MutationObserver === 'undefined') {
            return;
        }

        for (var index = 0; index < roots.length; index++) {
            var root = roots[index];
            if (!root) {
                continue;
            }

            (function (observedRoot) {
                var observer = new MutationObserver(function () {
                    scheduleSync(observedRoot);
                });

                observer.observe(observedRoot, {
                    childList: true,
                    subtree: true,
                    attributes: true,
                    attributeFilter: ['class']
                });
            })(root);
        }
    }

    document.addEventListener('DOMContentLoaded', function () {
        syncValidationTooltipMessages(document);
        var roots = collectValidationRoots();
        var scheduleSync = createSyncScheduler();
        bindValidationTooltipEvents(scheduleSync, roots);
        bindValidationTooltipObserver(scheduleSync, roots);
    });
})();
