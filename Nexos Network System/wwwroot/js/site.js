// Panel shell: collapsible sidebar.
// On desktop the collapsed state is remembered for the session so it survives
// navigation; below the desktop breakpoint the sidebar is an off-canvas drawer
// that always starts closed.
(function () {
    "use strict";

    var wrapper = document.getElementById("panelWrapper");
    var button = document.getElementById("panelSidebarToggle");
    if (!wrapper || !button) return;

    var STORAGE_KEY = "nx-nav-collapsed";
    var desktopQuery = window.matchMedia("(min-width: 992px)");

    function isDesktop() {
        return desktopQuery.matches;
    }

    function readStored() {
        try { return sessionStorage.getItem(STORAGE_KEY) === "1"; } catch (e) { return false; }
    }

    // Keeps the two states (desktop "collapsed", mobile "open") mutually exclusive
    // and mirrors the result onto aria-expanded.
    function applyState() {
        var hidden;

        if (isDesktop()) {
            wrapper.classList.remove("is-nav-open");
            hidden = readStored();
            wrapper.classList.toggle("is-nav-collapsed", hidden);
        } else {
            wrapper.classList.remove("is-nav-collapsed");
            hidden = !wrapper.classList.contains("is-nav-open");
        }

        button.setAttribute("aria-expanded", hidden ? "false" : "true");
    }

    button.addEventListener("click", function () {
        if (isDesktop()) {
            var collapsed = !wrapper.classList.contains("is-nav-collapsed");
            try { sessionStorage.setItem(STORAGE_KEY, collapsed ? "1" : "0"); } catch (e) { /* private mode */ }
        } else {
            wrapper.classList.toggle("is-nav-open");
        }

        applyState();
    });

    // Closing the drawer after a navigation keeps the target page unobstructed.
    wrapper.addEventListener("click", function (event) {
        if (isDesktop() || !event.target.closest(".panel-sidebar a")) return;
        wrapper.classList.remove("is-nav-open");
        applyState();
    });

    document.addEventListener("keydown", function (event) {
        if (event.key !== "Escape" || isDesktop() || !wrapper.classList.contains("is-nav-open")) return;
        wrapper.classList.remove("is-nav-open");
        applyState();
        button.focus();
    });

    document.addEventListener("click", function (event) {
        if (isDesktop() || !wrapper.classList.contains("is-nav-open")) return;
        if (event.target.closest(".panel-sidebar") || event.target.closest("#panelSidebarToggle")) return;
        wrapper.classList.remove("is-nav-open");
        applyState();
    });

    if (desktopQuery.addEventListener) {
        desktopQuery.addEventListener("change", applyState);
    } else if (desktopQuery.addListener) {
        desktopQuery.addListener(applyState);
    }

    applyState();
})();
