(function () {
    if (typeof window === "undefined" || !window.history || typeof window.history.replaceState !== "function") {
        return;
    }

    var url = new URL(window.location.href);
    if (!url.searchParams.has("handler")) {
        return;
    }

    url.searchParams.delete("handler");

    var query = url.searchParams.toString();
    var cleanedUrl = query.length > 0
        ? url.pathname + "?" + query + url.hash
        : url.pathname + url.hash;

    window.history.replaceState(window.history.state, document.title, cleanedUrl);
})();
