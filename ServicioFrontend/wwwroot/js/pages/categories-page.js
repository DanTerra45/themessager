(function () {
    function bindDeleteCategoryModal() {
        var deleteCategoryModal = document.getElementById('deleteCategoryModal');
        if (!deleteCategoryModal) {
            return;
        }

        deleteCategoryModal.addEventListener('show.bs.modal', function (event) {
            var button = event.relatedTarget;
            if (!button) {
                return;
            }

            var categoryId = button.getAttribute('data-id') || '';
            var categoryName = button.getAttribute('data-name') || '-';

            var deleteIdInput = document.getElementById('DeleteCategoryId');
            var deleteNameLabel = document.getElementById('deleteCategoryName');

            if (deleteIdInput) {
                deleteIdInput.value = categoryId;
            }
            if (deleteNameLabel) {
                deleteNameLabel.textContent = categoryName;
            }
        });
    }

    function clearCreateCategoryForm() {
        var createForm = document.getElementById('createCategoryForm');
        if (!createForm) {
            return;
        }

        createForm.reset();

        createForm.querySelectorAll('.input-validation-error').forEach(function (field) {
            field.classList.remove('input-validation-error');
        });

        createForm.querySelectorAll('span[data-valmsg-for]').forEach(function (span) {
            span.textContent = '';
            span.classList.remove('field-validation-error');
            span.classList.add('field-validation-valid');
            span.removeAttribute('data-error-message');
            span.removeAttribute('aria-label');
        });
    }

    function bindCreateCategoryResetOnOpen() {
        var openAddCategoryButton = document.getElementById('openAddCategoryModal');
        if (!openAddCategoryButton) {
            return;
        }

        openAddCategoryButton.addEventListener('click', function () {
            clearCreateCategoryForm();
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
        if (typeof bootstrap === 'undefined') {
            return;
        }

        bindDeleteCategoryModal();
        bindCreateCategoryResetOnOpen();

        var pageConfig = window.categoriesPageConfig || {};
        showModalIfNeeded(pageConfig.showCreateModal, 'addCategoryModal');
        showModalIfNeeded(pageConfig.showEditModal, 'editCategoryModal');
    });
})();
