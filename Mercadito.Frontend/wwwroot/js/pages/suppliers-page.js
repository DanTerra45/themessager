(function () {
    function bindDeleteSupplierModal() {
        var deleteSupplierModal = document.getElementById('deleteSupplierModal');
        if (!deleteSupplierModal) {
            return;
        }

        deleteSupplierModal.addEventListener('show.bs.modal', function (event) {
            var trigger = event.relatedTarget;
            if (!trigger) {
                return;
            }

            var supplierId = trigger.getAttribute('data-id') || '';
            var supplierName = trigger.getAttribute('data-name') || '-';
            var deleteIdInput = document.getElementById('DeleteSupplierId');
            var deleteNameLabel = document.getElementById('deleteSupplierName');

            if (deleteIdInput) {
                deleteIdInput.value = supplierId;
            }

            if (deleteNameLabel) {
                deleteNameLabel.textContent = supplierName;
            }
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

        bootstrap.Modal.getOrCreateInstance(modalElement).show();
    }

    function bindEditSupplierModal() {
        var editModal = document.getElementById('editarProveedorModal');
        if (!editModal) {
            return;
        }

        editModal.addEventListener('show.bs.modal', async function (event) {
            var trigger = event.relatedTarget;
            if (!trigger) {
                return;
            }

            var detailsUrl = trigger.getAttribute('data-details-url');
            if (!detailsUrl) {
                return;
            }

            try {
                var response = await fetch(detailsUrl, {
                    headers: {
                        'X-Requested-With': 'XMLHttpRequest'
                    }
                });

                if (!response.ok) {
                    throw new Error('No se pudo cargar el proveedor.');
                }

                var supplier = await response.json();
                var editIdInput = document.getElementById('EditSupplier_Id');
                var editCodigoInput = document.getElementById('EditSupplier_Codigo');
                var editRazonSocialInput = document.getElementById('EditSupplier_Nombre');
                var editDireccionInput = document.getElementById('EditSupplier_Direccion');
                var editContactoInput = document.getElementById('EditSupplier_Contacto');
                var editTelefonoInput = document.getElementById('EditSupplier_Telefono');
                var editRubroInput = document.getElementById('EditSupplier_Rubro');

                if (editIdInput) {
                    editIdInput.value = readSupplierValue(supplier, 'id');
                }

                if (editCodigoInput) {
                    editCodigoInput.value = readSupplierValue(supplier, 'codigo');
                }

                if (editRazonSocialInput) {
                    editRazonSocialInput.value = readSupplierValue(supplier, 'nombre');
                }

                if (editDireccionInput) {
                    editDireccionInput.value = readSupplierValue(supplier, 'direccion');
                }

                if (editContactoInput) {
                    editContactoInput.value = readSupplierValue(supplier, 'contacto');
                }

                if (editTelefonoInput) {
                    editTelefonoInput.value = readSupplierValue(supplier, 'telefono');
                }

                if (editRubroInput) {
                    editRubroInput.value = readSupplierValue(supplier, 'rubro');
                }
            } catch (error) {
                console.error(error);
            }
        });
    }

    function readSupplierValue(supplier, key) {
        if (!supplier) {
            return '';
        }

        if (Object.prototype.hasOwnProperty.call(supplier, key)) {
            return supplier[key] || '';
        }

        var pascalKey = key.charAt(0).toUpperCase() + key.slice(1);
        return supplier[pascalKey] || '';
    }

    document.addEventListener('DOMContentLoaded', function () {
        if (typeof bootstrap === 'undefined') {
            return;
        }

        bindDeleteSupplierModal();
        bindEditSupplierModal();

        var configElement = document.getElementById('suppliersPageConfig');
        if (!configElement) {
            return;
        }

        showModalIfNeeded(configElement.dataset.showCreateModal === 'true', 'nuevoProveedorModal');
        showModalIfNeeded(configElement.dataset.showEditModal === 'true', 'editarProveedorModal');
    });
})();
