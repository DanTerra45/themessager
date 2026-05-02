(function () {
    function showModalIfNeeded(configFlag, modalId) {
        if (!configFlag || typeof bootstrap === 'undefined') {
            return;
        }

        var modalElement = document.getElementById(modalId);
        if (!modalElement) {
            return;
        }

        bootstrap.Modal.getOrCreateInstance(modalElement).show();
    }

    function buildUsernamePreview(email) {
        if (!email) {
            return '(sin datos suficientes aún)';
        }

        var localPart = email.split('@')[0] || '';
        var normalized = localPart
            .toLowerCase()
            .replace(/[^a-z0-9._-]+/g, '.')
            .replace(/[._-]{2,}/g, '.')
            .replace(/^[._-]+|[._-]+$/g, '');

        if (!normalized) {
            return '(sin datos suficientes aún)';
        }

        if (normalized.length < 4) {
            return '(sin datos suficientes aún)';
        }

        if (normalized.length > 40) {
            normalized = normalized.substring(0, 40);
        }

        return normalized;
    }

    function initUsernamePreview() {
        var emailInput = document.getElementById('NewUser_Email');
        var usernamePreviewInput = document.getElementById('NewUser_Username');

        if (!emailInput || !usernamePreviewInput) {
            return;
        }

        function updateUsernamePreview() {
            usernamePreviewInput.value = buildUsernamePreview(emailInput.value);
        }

        emailInput.addEventListener('input', updateUsernamePreview);
        updateUsernamePreview();
    }

    function wireDropdownSelection(dropdownName, hiddenInputId, textNodeId) {
        var hiddenInput = document.getElementById(hiddenInputId);
        var textNode = document.getElementById(textNodeId);

        if (!hiddenInput || !textNode) {
            return;
        }

        var selector = '[data-users-dropdown="' + dropdownName + '"]';
        var options = document.querySelectorAll(selector);

        options.forEach(function (option) {
            option.addEventListener('click', function () {
                hiddenInput.value = option.getAttribute('data-value') || '';
                textNode.textContent = option.getAttribute('data-label') || '';
            });
        });
    }

    function bindSendResetLinkModal() {
        var sendResetLinkModal = document.getElementById('sendResetLinkModal');
        if (!sendResetLinkModal) {
            return;
        }

        sendResetLinkModal.addEventListener('show.bs.modal', function (event) {
            var trigger = event.relatedTarget;
            if (!trigger) {
                return;
            }

            var userIdInput = document.getElementById('SendResetLink_UserId');
            var usernameInput = document.getElementById('SendResetLink_Username');
            var emailInput = document.getElementById('SendResetLink_Email');

            if (userIdInput) {
                userIdInput.value = trigger.getAttribute('data-user-id') || '';
            }

            if (usernameInput) {
                usernameInput.value = trigger.getAttribute('data-username') || '';
            }

            if (emailInput) {
                emailInput.value = trigger.getAttribute('data-user-email') || '';
            }
        });
    }

    function bindTemporaryPasswordModal() {
        var temporaryPasswordModal = document.getElementById('temporaryPasswordModal');
        if (!temporaryPasswordModal) {
            return;
        }

        temporaryPasswordModal.addEventListener('show.bs.modal', function (event) {
            var trigger = event.relatedTarget;
            if (!trigger) {
                return;
            }

            var userIdInput = document.getElementById('TemporaryPassword_UserId');
            var usernameInput = document.getElementById('TemporaryPassword_Username');
            var passwordInput = document.getElementById('TemporaryPassword_TemporaryPassword');
            var confirmInput = document.getElementById('TemporaryPassword_ConfirmTemporaryPassword');

            if (userIdInput) {
                userIdInput.value = trigger.getAttribute('data-user-id') || '';
            }

            if (usernameInput) {
                usernameInput.value = trigger.getAttribute('data-username') || '';
            }

            if (passwordInput) {
                passwordInput.value = '';
            }

            if (confirmInput) {
                confirmInput.value = '';
            }
        });
    }

    function bindDeactivateModal() {
        var deactivateModal = document.getElementById('deactivateUserModal');
        if (!deactivateModal) {
            return;
        }

        deactivateModal.addEventListener('show.bs.modal', function (event) {
            var trigger = event.relatedTarget;
            if (!trigger) {
                return;
            }

            var userIdInput = document.getElementById('DeactivateUserId');
            var usernameLabel = document.getElementById('deactivateUserName');

            if (userIdInput) {
                userIdInput.value = trigger.getAttribute('data-user-id') || '';
            }

            if (usernameLabel) {
                usernameLabel.textContent = trigger.getAttribute('data-username') || 'este usuario';
            }
        });
    }

    document.addEventListener('DOMContentLoaded', function () {
        initUsernamePreview();
        wireDropdownSelection('employee', 'NewUser_EmployeeId', 'newUserEmployeeDropdownText');
        wireDropdownSelection('role', 'NewUser_Role', 'newUserRoleDropdownText');

        if (typeof bootstrap === 'undefined') {
            return;
        }

        bindSendResetLinkModal();
        bindTemporaryPasswordModal();
        bindDeactivateModal();

        var configElement = document.getElementById('usersPageConfig');
        if (!configElement) {
            return;
        }

        showModalIfNeeded(configElement.dataset.showCreateModal === 'true', 'nuevoUsuarioModal');
        showModalIfNeeded(configElement.dataset.showSendResetLinkModal === 'true', 'sendResetLinkModal');
        showModalIfNeeded(configElement.dataset.showTemporaryPasswordModal === 'true', 'temporaryPasswordModal');
        showModalIfNeeded(configElement.dataset.showDeactivateModal === 'true', 'deactivateUserModal');
    });
})();
