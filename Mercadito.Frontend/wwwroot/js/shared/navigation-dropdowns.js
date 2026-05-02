(function () {
    document.addEventListener('DOMContentLoaded', function () {
        if (typeof bootstrap === 'undefined') {
            return;
        }

        var desktopMediaQuery = window.matchMedia('(min-width: 576px)');
        var dropdownItems = document.querySelectorAll('[data-app-nav-dropdown="true"]');
        var dropdownStates = [];

        for (var index = 0; index < dropdownItems.length; index++) {
            var dropdownState = createDropdownState(dropdownItems[index]);
            if (!dropdownState) {
                continue;
            }

            dropdownStates.push(dropdownState);
        }

        for (var stateIndex = 0; stateIndex < dropdownStates.length; stateIndex++) {
            wireNavigationDropdown(dropdownStates[stateIndex], dropdownStates, desktopMediaQuery);
        }
    });

    function createDropdownState(dropdownItem) {
        var toggle = dropdownItem.querySelector('[data-app-nav-dropdown-toggle="true"]');
        if (!toggle) {
            return null;
        }

        var menu = dropdownItem.querySelector('.dropdown-menu');
        if (!menu) {
            return null;
        }

        return {
            dropdownItem: dropdownItem,
            toggle: toggle,
            menu: menu,
            dropdownInstance: bootstrap.Dropdown.getOrCreateInstance(toggle),
            isPinnedOpen: false
        };
    }

    function wireNavigationDropdown(dropdownState, dropdownStates, desktopMediaQuery) {
        dropdownState.dropdownItem.addEventListener('mouseenter', function () {
            if (!desktopMediaQuery.matches) {
                return;
            }

            openDropdown(dropdownState, dropdownStates);
        });

        dropdownState.dropdownItem.addEventListener('mouseleave', function () {
            if (!desktopMediaQuery.matches) {
                return;
            }

            if (dropdownState.isPinnedOpen) {
                return;
            }

            dropdownState.dropdownInstance.hide();
        });

        dropdownState.toggle.addEventListener('click', function (event) {
            if (!desktopMediaQuery.matches) {
                return;
            }

            event.preventDefault();
            event.stopPropagation();

            if (dropdownState.menu.classList.contains('show')) {
                if (dropdownState.isPinnedOpen) {
                    dropdownState.isPinnedOpen = false;
                    dropdownState.dropdownInstance.hide();
                    return;
                }

                dropdownState.isPinnedOpen = true;
                openDropdown(dropdownState, dropdownStates);
                return;
            }

            dropdownState.isPinnedOpen = true;
            openDropdown(dropdownState, dropdownStates);
        });

        dropdownState.toggle.addEventListener('hidden.bs.dropdown', function () {
            dropdownState.isPinnedOpen = false;
        });

        desktopMediaQuery.addEventListener('change', function (event) {
            if (event.matches) {
                return;
            }

            closeAllDropdowns(dropdownStates);
        });
    }

    function openDropdown(activeDropdownState, dropdownStates) {
        closeOtherDropdowns(activeDropdownState, dropdownStates);
        activeDropdownState.dropdownInstance.show();
    }

    function closeOtherDropdowns(activeDropdownState, dropdownStates) {
        for (var index = 0; index < dropdownStates.length; index++) {
            var dropdownState = dropdownStates[index];
            if (dropdownState === activeDropdownState) {
                continue;
            }

            dropdownState.isPinnedOpen = false;
            dropdownState.dropdownInstance.hide();
        }
    }

    function closeAllDropdowns(dropdownStates) {
        for (var index = 0; index < dropdownStates.length; index++) {
            dropdownStates[index].isPinnedOpen = false;
            dropdownStates[index].dropdownInstance.hide();
        }
    }
})();
