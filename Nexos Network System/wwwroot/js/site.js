// NEXUS initial loading animation (see #nx-loader markup in the layouts).
// An inline head script in each layout decides *whether* the loader may play
// (first page of a session, motion allowed). site.js only drives its
// lifecycle when it is actually visible.

(function () {
    "use strict";

    var loader = document.getElementById("nx-loader");
    if (!loader) return;

    var SKIP_KEY = "nx-boot-skip";

    var done = false;

    // Shorten the CSS timeline if the document is already loaded
    // when site.js executes (cache hits / SPAs-like fast repeats).
    function progress() {
        if (done) return;
        if (document.readyState === "complete") {
            done = true;
            loader.style.setProperty("--nx-load-progress", "1");
        }
        // Note: if still loading, the window load listener below updates it.
    }

    function finish() {
        if (loader.classList.contains("nx-leaving")) return;
        loader.classList.add("nx-leaving");
        loader.style.setProperty("--nx-load-progress", "1");
        setTimeout(function () {
            loader.style.setProperty("display", "none");
        }, 520);
    }

    // Only visible when the head script allowed it (first page of session,
    // motion allowed). The nx-boot-skip class belongs to the inline head
    // script alone - site.js never removes it.
    if (getComputedStyle(loader).display === "none") {
        return;
    }

    // Mark session as played IMMEDIATELY on the first page. A fast second
    // navigation must not replay the intro - this keeps the guarantee even
    // if this page never fires its events.
    try { sessionStorage.setItem(SKIP_KEY, "1"); } catch (e) { /* private mode */ }

    // Real-load progress line: document readyState driven.
    if (document.readyState === "complete") {
        loader.style.setProperty("--nx-load-progress", "1");
        done = true;
    } else {
        document.addEventListener("readystatechange", progress);
        window.addEventListener("load", progress);
    }

    // Fade out as soon as the ribbon sequence is complete (~2.75s) and
    // document finished. If the document is slow, wait for it so the
    // reveal shows a working page.
    var MIN_SHOW = 2900;
    var elapsed = 0;
    var tick = setInterval(function () {
        elapsed += 100;
        if (elapsed >= MIN_SHOW && document.readyState === "complete") finish();
        // Hard cap: never hold the user longer than 4.5s
        if (elapsed > 4500) finish();
    }, 100);
})();
