(function () {
    var DATEPICKER_LOCALE_ES = {
        days: ['Domingo', 'Lunes', 'Martes', 'Miercoles', 'Jueves', 'Viernes', 'Sabado'],
        daysShort: ['Dom', 'Lun', 'Mar', 'Mie', 'Jue', 'Vie', 'Sab'],
        daysMin: ['Do', 'Lu', 'Ma', 'Mi', 'Ju', 'Vi', 'Sa'],
        months: ['Enero', 'Febrero', 'Marzo', 'Abril', 'Mayo', 'Junio', 'Julio', 'Agosto', 'Septiembre', 'Octubre', 'Noviembre', 'Diciembre'],
        monthsShort: ['Ene', 'Feb', 'Mar', 'Abr', 'May', 'Jun', 'Jul', 'Ago', 'Sep', 'Oct', 'Nov', 'Dic'],
        today: 'Hoy',
        clear: 'Limpiar',
        dateFormat: 'dd/MM/yyyy',
        timeFormat: 'hh:mm aa',
        firstDay: 1
    };

    function initSteppers() {
        var buttons = document.querySelectorAll('.brutal-stepper-btn[data-step]');
        buttons.forEach(function (button) {
            button.addEventListener('click', function () {
                var inputGroup = button.closest('[data-stepper]') || button.closest('.input-group');
                if (!inputGroup) {
                    return;
                }

                var numericInput = inputGroup.querySelector('input[type=number]');
                if (!numericInput) {
                    return;
                }

                if (button.getAttribute('data-step') === '-1') {
                    numericInput.stepDown();
                } else {
                    numericInput.stepUp();
                }

                numericInput.dispatchEvent(new Event('input', { bubbles: true }));
                numericInput.dispatchEvent(new Event('change', { bubbles: true }));
            });
        });
    }

    function resizeProductDescription(textarea) {
        var minHeight = 96;
        var maxHeight = 180;

        textarea.style.height = minHeight + 'px';
        var contentHeight = textarea.scrollHeight;
        var targetHeight = contentHeight > maxHeight ? maxHeight : contentHeight;

        if (targetHeight < minHeight) {
            targetHeight = minHeight;
        }

        textarea.style.height = targetHeight + 'px';
        textarea.style.overflowY = contentHeight > maxHeight ? 'auto' : 'hidden';
    }

    function initDescriptionAutoresize() {
        var textareas = document.querySelectorAll('.product-description-textarea');
        textareas.forEach(function (textarea) {
            resizeProductDescription(textarea);
            textarea.addEventListener('input', function () {
                resizeProductDescription(textarea);
            });
            textarea.addEventListener('focus', function () {
                resizeProductDescription(textarea);
            });
        });
    }

    function showModalIfNeeded(configFlag, modalId) {
        if (!configFlag || typeof bootstrap === 'undefined') {
            return;
        }

        var modalElement = document.getElementById(modalId);
        if (!modalElement) {
            return;
        }

        var modal = new bootstrap.Modal(modalElement);
        modal.show();
    }

    function hideBootstrapDropdown(trigger) {
        if (!trigger || typeof bootstrap === 'undefined') {
            return;
        }

        var dropdownInstance = bootstrap.Dropdown.getOrCreateInstance(trigger);
        dropdownInstance.hide();
    }

    function initDatePickers() {
        if (typeof AirDatepicker === 'undefined') {
            return;
        }

        var dateInputs = document.querySelectorAll('.date-picker-input');

        dateInputs.forEach(function (input) {
            var minDate = input.getAttribute('data-min-date');
            var pickerConfiguration = {
                locale: DATEPICKER_LOCALE_ES,
                dateFormat: 'yyyy-MM-dd',
                autoClose: true,
                isMobile: false
            };

            if (minDate) {
                pickerConfiguration.minDate = minDate;
            }

            new AirDatepicker(input, pickerConfiguration);
        });
    }

    function bindDeleteProductModal() {
        var deleteProductModal = document.getElementById('deleteProductModal');
        if (!deleteProductModal) {
            return;
        }

        deleteProductModal.addEventListener('show.bs.modal', function (event) {
            var button = event.relatedTarget;
            if (!button) {
                return;
            }

            var productId = button.getAttribute('data-id') || '';
            var productName = button.getAttribute('data-name') || '-';
            var hasCategories = (button.getAttribute('data-has-categories') || '').toLowerCase() === 'true';

            var deleteIdInput = document.getElementById('DeleteProductId');
            var deleteNameLabel = document.getElementById('deleteProductName');
            var deleteCategoryWarning = document.getElementById('deleteProductCategoryWarning');

            if (deleteIdInput) {
                deleteIdInput.value = productId;
            }
            if (deleteNameLabel) {
                deleteNameLabel.textContent = productName;
            }
            if (deleteCategoryWarning) {
                deleteCategoryWarning.classList.toggle('d-none', !hasCategories);
            }
        });
    }

    function setDropdownItemVisualState(item, isSelected) {
        var icon = item.querySelector('.check-icon');
        item.classList.toggle('is-selected', isSelected);
        if (icon) {
            icon.classList.toggle('d-none', !isSelected);
        }
    }

    function applyMenuSelection(valueInput, textOutput, items, valueAttribute, selectedValue, selectedName) {
        valueInput.value = selectedValue;
        textOutput.textContent = selectedName;

        items.forEach(function (item) {
            var itemValue = item.getAttribute(valueAttribute) || '';
            setDropdownItemVisualState(item, itemValue === selectedValue);
        });
    }

    function initializeMenuSelection(valueInput, textOutput, items, valueAttribute, nameAttribute, defaultValue, defaultName) {
        var currentValue = valueInput.value || defaultValue;

        for (var index = 0; index < items.length; index++) {
            var item = items[index];
            var itemValue = item.getAttribute(valueAttribute) || '';
            if (itemValue !== currentValue) {
                continue;
            }

            var itemName = item.getAttribute(nameAttribute) || defaultName;
            applyMenuSelection(valueInput, textOutput, items, valueAttribute, itemValue, itemName);
            return itemValue;
        }

        applyMenuSelection(valueInput, textOutput, items, valueAttribute, defaultValue, defaultName);
        return defaultValue;
    }

    function updateCategoryDropdownText(textSpan, checkboxes) {
        if (!textSpan) {
            return;
        }

        var totalCount = checkboxes.length;
        if (totalCount === 0) {
            textSpan.textContent = 'No hay categorías creadas.';
            return;
        }

        var checkedCount = 0;
        var singleCheckedName = '';

        for (var index = 0; index < checkboxes.length; index++) {
            var checkbox = checkboxes[index];
            if (!checkbox.checked) {
                continue;
            }

            checkedCount++;
            if (checkedCount === 1) {
                singleCheckedName = checkbox.getAttribute('data-name') || '';
            }
        }

        if (checkedCount === 0) {
            textSpan.textContent = 'Sin categorías seleccionadas';
            return;
        }

        if (checkedCount === 1) {
            textSpan.textContent = singleCheckedName;
            return;
        }

        textSpan.textContent = checkedCount + ' categorías seleccionadas';
    }

    function initCategoryDropdown(checkboxSelector, dropdownTextId, dropdownTriggerId) {
        var textSpan = document.getElementById(dropdownTextId);
        if (!textSpan) {
            return;
        }

        var checkboxes = Array.from(document.querySelectorAll(checkboxSelector));
        if (checkboxes.length === 0) {
            updateCategoryDropdownText(textSpan, checkboxes);
            return;
        }

        checkboxes.forEach(function (box) {
            var label = box.closest('.category-select-item');
            if (!label) {
                return;
            }

            setDropdownItemVisualState(label, box.checked);
        });

        updateCategoryDropdownText(textSpan, checkboxes);

        var menuSelector = 'ul[aria-labelledby="' + dropdownTriggerId + '"]';
        var menuElement = document.querySelector(menuSelector);
        if (!menuElement) {
            return;
        }

        menuElement.addEventListener('change', function (event) {
            var changedBox = event.target;
            if (!changedBox || typeof changedBox.matches !== 'function' || !changedBox.matches(checkboxSelector)) {
                return;
            }

            updateCategoryDropdownText(textSpan, checkboxes);

            var label = changedBox.closest('.category-select-item');
            if (!label) {
                return;
            }

            setDropdownItemVisualState(label, changedBox.checked);
        });
    }

    function initFilterDropdown() {
        var filterInput = document.getElementById('categoryFilterInput');
        var filterTrigger = document.getElementById('productsFilterDropdown');
        var filterText = document.getElementById('productsFilterDropdownText');
        var filterSearch = document.getElementById('productsFilterSearch');
        var filterItems = Array.from(document.querySelectorAll('.products-filter-item'));

        if (!filterInput || !filterText || filterItems.length === 0) {
            return;
        }

        function filterCategoryItemsBySearch(term) {
            var normalizedTerm = (term || '').trim().toLowerCase();
            filterItems.forEach(function (item) {
                var value = item.getAttribute('data-filter-value') || '';
                var name = (item.getAttribute('data-filter-name') || '').toLowerCase();
                var itemContainer = item.closest('li');
                if (!itemContainer) {
                    return;
                }

                var shouldShow = value === '0' || normalizedTerm.length === 0 || name.indexOf(normalizedTerm) >= 0;
                itemContainer.classList.toggle('d-none', !shouldShow);
            });
        }

        initializeMenuSelection(
            filterInput,
            filterText,
            filterItems,
            'data-filter-value',
            'data-filter-name',
            '0',
            'Todas las categorías');

        var filterMenu = document.querySelector('.products-filter-dropdown-menu');
        if (filterMenu) {
            filterMenu.addEventListener('click', function (event) {
                var selectedItem = event.target.closest('.products-filter-item');
                if (!selectedItem || !filterMenu.contains(selectedItem)) {
                    return;
                }

                var selectedValue = selectedItem.getAttribute('data-filter-value') || '';
                var selectedName = selectedItem.getAttribute('data-filter-name') || '';
                applyMenuSelection(filterInput, filterText, filterItems, 'data-filter-value', selectedValue, selectedName);
                hideBootstrapDropdown(filterTrigger);
            });
        }

        if (filterSearch) {
            filterSearch.addEventListener('input', function () {
                filterCategoryItemsBySearch(filterSearch.value);
            });

            filterSearch.addEventListener('keydown', function (event) {
                if (event.key === 'Enter') {
                    event.preventDefault();
                }
            });
        }

        if (filterTrigger) {
            filterTrigger.addEventListener('shown.bs.dropdown', function () {
                if (!filterSearch) {
                    return;
                }

                filterSearch.value = '';
                filterCategoryItemsBySearch('');
                filterSearch.focus();
            });

            filterTrigger.addEventListener('hidden.bs.dropdown', function () {
                if (filterSearch) {
                    filterSearch.value = '';
                }
                filterCategoryItemsBySearch('');
            });
        }
    }

    function initOrderDropdown() {
        var orderInput = document.getElementById('productsOrderPresetInput');
        var orderTrigger = document.getElementById('productsOrderDropdown');
        var orderText = document.getElementById('productsOrderDropdownText');
        var orderItems = Array.from(document.querySelectorAll('.products-order-item'));

        if (!orderInput || !orderText || orderItems.length === 0) {
            return;
        }

        initializeMenuSelection(
            orderInput,
            orderText,
            orderItems,
            'data-order-value',
            'data-order-name',
            'custom',
            'Personalizado');

        var orderMenu = document.querySelector('.products-order-dropdown-menu');
        if (orderMenu) {
            orderMenu.addEventListener('click', function (event) {
                var selectedItem = event.target.closest('.products-order-item');
                if (!selectedItem || !orderMenu.contains(selectedItem)) {
                    return;
                }

                var selectedValue = selectedItem.getAttribute('data-order-value') || '';
                var selectedName = selectedItem.getAttribute('data-order-name') || '';
                applyMenuSelection(orderInput, orderText, orderItems, 'data-order-value', selectedValue, selectedName);

                var parentForm = orderInput.closest('form');
                if (parentForm) {
                    parentForm.requestSubmit();
                }
                hideBootstrapDropdown(orderTrigger);
            });
        }

        if (orderTrigger) {
            orderTrigger.addEventListener('hidden.bs.dropdown', function () {
                var persistedOrderValue = orderInput.value || 'custom';
                orderItems.forEach(function (item) {
                    var itemValue = item.getAttribute('data-order-value') || '';
                    setDropdownItemVisualState(item, itemValue === persistedOrderValue);
                });
            });
        }
    }

    document.addEventListener('DOMContentLoaded', function () {
        initSteppers();
        initDescriptionAutoresize();
        initDatePickers();
        bindDeleteProductModal();
        initCategoryDropdown('.new-category-checkbox', 'newCategoryDropdownText', 'newCategoryDropdown');
        initCategoryDropdown('.edit-category-checkbox', 'editCategoryDropdownText', 'editCategoryDropdown');
        initFilterDropdown();
        initOrderDropdown();

        var pageConfig = window.productsPageConfig || {};
        showModalIfNeeded(pageConfig.showCreateModal, 'addProductModal');
        showModalIfNeeded(pageConfig.showEditModal, 'editProductModal');
    });
})();
