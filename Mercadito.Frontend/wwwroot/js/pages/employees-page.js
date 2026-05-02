(function () {

    function applyUppercaseInputBehavior() {
        document.querySelectorAll('.js-uppercase').forEach(function (input) {
            input.addEventListener('input', function () {
                input.value = input.value.toUpperCase();
            });
        });
    }

    function initializeRoleDropdown(config) {
        var hiddenInput = document.getElementById(config.hiddenInputId);
        var triggerText = document.getElementById(config.triggerTextId);
        var itemSelector = '[data-role-dropdown="' + config.dropdownKey + '"]';
        var items = document.querySelectorAll(itemSelector);

        if (!hiddenInput || !triggerText || items.length === 0) {
            return;
        }

        function applyRoleSelection(roleValue) {
            hiddenInput.value = roleValue;
            triggerText.textContent = roleValue;

            items.forEach(function (item) {
                var icon = item.querySelector('.check-icon');
                var isSelected = item.getAttribute('data-value') === roleValue;

                item.classList.toggle('is-selected', isSelected);

                if (icon) {
                    icon.classList.toggle('d-none', !isSelected);
                }
            });

            hiddenInput.dispatchEvent(new Event('change', { bubbles: true }));
        }

        var currentValue = hiddenInput.value;
        if (currentValue !== 'Cajero' && currentValue !== 'Inventario') {
            currentValue = 'Cajero';
        }

        applyRoleSelection(currentValue);

        var menuElement = items[0].closest('.role-dropdown-menu');
        if (!menuElement) {
            return;
        }

        menuElement.addEventListener('click', function (event) {
            var selectedItem = event.target.closest(itemSelector);
            if (!selectedItem || !menuElement.contains(selectedItem)) {
                return;
            }

            var selectedValue = selectedItem.getAttribute('data-value');
            if (!selectedValue) {
                return;
            }

            applyRoleSelection(selectedValue);
        });
    }

    function bindDeleteEmployeeModal() {
        var deleteEmployeeModal = document.getElementById('deleteEmployeeModal');
        if (!deleteEmployeeModal) {
            return;
        }

        deleteEmployeeModal.addEventListener('show.bs.modal', function (event) {
            var button = event.relatedTarget;
            if (!button) {
                return;
            }

            var employeeId = button.getAttribute('data-id') || '';
            var employeeName = button.getAttribute('data-name') || '-';

            var deleteIdInput = document.getElementById('DeleteEmployeeId');
            var deleteNameLabel = document.getElementById('deleteEmployeeName');

            if (deleteIdInput) {
                deleteIdInput.value = employeeId;
            }
            if (deleteNameLabel) {
                deleteNameLabel.textContent = employeeName;
            }
        });
    }

    function showModalIfNeeded(configFlag, modalId) {
        if (!configFlag) {
            return;
        }

        var modalElement = document.getElementById(modalId);
        if (!modalElement || typeof bootstrap === 'undefined') {
            return;
        }

        var modal = new bootstrap.Modal(modalElement);
        modal.show();
    }

    document.addEventListener('DOMContentLoaded', function () {
        applyUppercaseInputBehavior();

        if (typeof bootstrap === 'undefined') {
            return;
        }

        initializeRoleDropdown({
            hiddenInputId: 'NewEmployee_Cargo',
            triggerTextId: 'newRoleDropdownText',
            dropdownKey: 'newRole'
        });

        initializeRoleDropdown({
            hiddenInputId: 'EditEmployee_Cargo',
            triggerTextId: 'editRoleDropdownText',
            dropdownKey: 'editRole'
        });

        bindDeleteEmployeeModal();

        var pageConfig = window.employeesPageConfig || {};
        showModalIfNeeded(pageConfig.showCreateModal, 'addEmployeeModal');
        showModalIfNeeded(pageConfig.showEditModal, 'editEmployeeModal');
    });
})();
