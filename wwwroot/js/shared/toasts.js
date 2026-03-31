(function () {
    document.addEventListener('DOMContentLoaded', function () {
        if (typeof bootstrap === 'undefined') {
            return;
        }

        var toastElements = document.querySelectorAll('.toast');
        for (var index = 0; index < toastElements.length; index++) {
            var toast = new bootstrap.Toast(toastElements[index]);
            toast.show();
        }
    });
})();
