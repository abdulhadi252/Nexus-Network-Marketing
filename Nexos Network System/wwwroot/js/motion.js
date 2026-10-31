// NEXUS motion layer.
// Three jobs only:
//   1. run the branded boot loader once per browsing session (never on
//      internal navigation) and hand the page back as soon as it is ready;
//   2. scroll-reveal public sections that start below the fold;
//   3. give card grids their stagger index and support collapsible sub-menus.
// Everything else is pure CSS. No library, no animation loop.
(function () {
    "use strict";

    var root = document.documentElement;
    var reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)");

    function prefersReducedMotion() {
        return reducedMotion.matches;
    }

    // ---------------------------------------------------------------
    // 1. Branded boot loader
    // ---------------------------------------------------------------
    function initBootLoader() {
        var loader = document.getElementById("nxLoader");
        if (!loader) return;

        // Already booted in this tab, or the user asked for less motion:
        // drop the overlay before it can hold the page.
        if (root.classList.contains("nx-booted") || prefersReducedMotion()) {
            loader.parentNode.removeChild(loader);
            return;
        }

        // Mark the session so the next internal navigation skips the loader.
        try {
            sessionStorage.setItem("nx-boot", String(Date.now()));
        } catch (e) {
            /* storage blocked (private mode) - the loader just shows again */
        }

        var started = Date.now();
        var MIN_VISIBLE = 1500;  // the ribbon needs time to form the logo
        var MAX_VISIBLE = 2100;  // never hold an already-ready page for longer
        var closing = false;

        function hide() {
            if (closing) return;
            closing = true;

            var remaining = Math.max(0, MIN_VISIBLE - (Date.now() - started));

            window.setTimeout(function () {
                loader.classList.add("is-done");

                window.setTimeout(function () {
                    if (loader.parentNode) loader.parentNode.removeChild(loader);
                }, 460);
            }, remaining);
        }

        if (document.readyState === "complete") {
            hide();
        } else {
            window.addEventListener("load", hide, { once: true });
        }

        // Hard cap: a stalled subresource can never lock the application.
        window.setTimeout(hide, MAX_VISIBLE);
    }

    // ---------------------------------------------------------------
    // 2. Scroll reveal for public pages
    // Only sections that start below the fold are tagged, so nothing
    // above the fold double-animates and nothing is hidden by the markup.
    // ---------------------------------------------------------------
    function initScrollReveal() {
        if (prefersReducedMotion() || !("IntersectionObserver" in window)) return;
        if (document.body.classList.contains("panel-body")) return;

        var heroClasses = ["hp-hero", "svc-hero", "ab-hero", "contact-hero", "pl-hero", "page-hero"];
        var horizon = window.innerHeight * 0.92;
        var targets = [];
        var i;

        function isHero(element) {
            for (var h = 0; h < heroClasses.length; h++) {
                if (element.classList.contains(heroClasses[h])) return true;
            }
            return false;
        }

        var sections = document.querySelectorAll("main > section");
        for (i = 0; i < sections.length; i++) {
            var section = sections[i];
            if (isHero(section)) continue;
            if (section.getBoundingClientRect().top < horizon) continue;
            targets.push(section);
        }

        var cards = document.querySelectorAll(
            ".hp-svc-card, .hp-plan-card, .pl-plan, .svc-main-card, .svc-support-card, " +
            ".ab-value-card, .ab-step, .contact-card, .pl-service, " +
            ".hp-svc-card__media, .svc-main-card__media, .ab-story__photo, .ab-presence__photo"
        );

        for (i = 0; i < cards.length; i++) {
            var card = cards[i];
            var owner = card.closest("section");
            if (owner && targets.indexOf(owner) !== -1) continue;
            if (card.getBoundingClientRect().top < horizon) continue;
            targets.push(card);
        }

        if (!targets.length) return;

        var observer = new IntersectionObserver(function (entries) {
            for (var e = 0; e < entries.length; e++) {
                if (!entries[e].isIntersecting) continue;
                entries[e].target.classList.add("is-revealed");
                observer.unobserve(entries[e].target);
            }
        }, { rootMargin: "0px 0px -6% 0px", threshold: 0.1 });

        for (i = 0; i < targets.length; i++) {
            var node = targets[i];
            node.setAttribute("data-nx-reveal", "");
            node.style.setProperty("--nx-delay", (i % 3) * 70 + "ms");
            observer.observe(node);
        }
    }

    // ---------------------------------------------------------------
    // 3. Stagger index for card grids (picked up by motion.css)
    // ---------------------------------------------------------------
    function initStagger() {
        var groups = document.querySelectorAll(
            ".panel-content > .row, .row.g-4, .ab-values__grid, .svc-support-grid, " +
            ".donut__legend, .pay-stats"
        );

        for (var g = 0; g < groups.length; g++) {
            var children = groups[g].children;
            for (var c = 0; c < children.length; c++) {
                if (c > 6) break;
                children[c].style.setProperty("--nx-i", String(c));
            }
        }
    }

    // ---------------------------------------------------------------
    // 4. Collapsible sidebar sub-menus
    // ---------------------------------------------------------------
    function initSubmenus() {
        document.addEventListener("click", function (event) {
            var toggle = event.target.closest("[data-nx-submenu]");
            if (!toggle) return;

            var host = toggle.closest("li") || toggle.parentElement;
            if (!host) return;

            event.preventDefault();

            var open = host.classList.toggle("is-open");
            toggle.setAttribute("aria-expanded", open ? "true" : "false");
        });
    }

    // ---------------------------------------------------------------
    // 5. Cinematic hero headings
    // Words are wrapped at runtime so the markup stays exactly as authored
    // and a failed script leaves a perfectly normal heading behind.
    // ---------------------------------------------------------------
    function splitHeading(heading) {
        if (!heading || heading.getAttribute("data-nx-split")) return;
        heading.setAttribute("data-nx-split", "true");

        var label = (heading.textContent || "").replace(/\s+/g, " ").trim();
        if (label) heading.setAttribute("aria-label", label);

        var fragment = document.createDocumentFragment();
        var wordIndex = 0;

        function makeWord(content) {
            var outer = document.createElement("span");
            outer.className = "nx-word";
            outer.style.setProperty("--nx-w", String(Math.min(wordIndex++, 12)));

            var inner = document.createElement("span");
            inner.className = "nx-word__in";
            inner.appendChild(content);
            outer.appendChild(inner);
            return outer;
        }

        var nodes = Array.prototype.slice.call(heading.childNodes);
        for (var n = 0; n < nodes.length; n++) {
            var node = nodes[n];

            if (node.nodeType === 3) {
                var parts = node.textContent.split(/(\s+)/);
                for (var p = 0; p < parts.length; p++) {
                    var piece = parts[p];
                    if (!piece) continue;
                    if (/^\s+$/.test(piece)) fragment.appendChild(document.createTextNode(" "));
                    else fragment.appendChild(makeWord(document.createTextNode(piece)));
                }
            } else if (node.nodeType === 1) {
                if (node.tagName === "BR") fragment.appendChild(document.createElement("br"));
                else fragment.appendChild(makeWord(node.cloneNode(true)));
            }
        }

        heading.textContent = "";
        heading.appendChild(fragment);
        heading.classList.add("nx-split");
    }

    function initHeroSplit() {
        if (prefersReducedMotion()) return;

        var headings = document.querySelectorAll(
            ".hp-hero__title, .svc-hero__title, .ab-hero__title, " +
            ".contact-hero__title, .pl-hero__title, .lg-title"
        );

        for (var i = 0; i < headings.length; i++) splitHeading(headings[i]);
    }

    // ---------------------------------------------------------------
    // 6. Count-up for KPI values
    // Display only: the server-rendered text is parsed, animated, and then
    // restored verbatim, so the final value is always exactly the model's.
    // Elements with child markup are skipped so nothing can be destroyed.
    // ---------------------------------------------------------------
    function animateMetric(el, order) {
        if (el.childElementCount > 0) return;

        var original = el.textContent;
        var match = original.match(/-?\d[\d,]*(?:\.\d+)?/);
        if (!match) return;

        var raw = match[0];
        var target = parseFloat(raw.replace(/,/g, ""));
        if (!isFinite(target) || Math.abs(target) < 2) return;

        var decimals = raw.indexOf(".") > -1 ? raw.split(".")[1].length : 0;
        var grouped = raw.indexOf(",") > -1;
        var prefix = original.slice(0, match.index);
        var suffix = original.slice(match.index + raw.length);
        var formatter = new Intl.NumberFormat(undefined, {
            minimumFractionDigits: decimals,
            maximumFractionDigits: decimals,
            useGrouping: grouped
        });

        var duration = 850;
        var delay = 240 + Math.min(order, 6) * 60;
        var started = null;

        function frame(now) {
            if (started === null) started = now;

            var progress = Math.min(1, (now - started) / duration);
            var eased = 1 - Math.pow(1 - progress, 3);
            var value = target * eased;

            el.textContent = prefix + formatter.format(decimals ? value : Math.round(value)) + suffix;

            if (progress < 1) requestAnimationFrame(frame);
            else el.textContent = original;
        }

        window.setTimeout(function () { requestAnimationFrame(frame); }, delay);
    }

    function initCountUp() {
        if (prefersReducedMotion()) return;

        var metrics = document.querySelectorAll(
            ".kpi-card__value, .stat-card .stat-value, .donut__total, .pay-stat__value"
        );

        for (var i = 0; i < metrics.length; i++) animateMetric(metrics[i], i);
    }

    // ---------------------------------------------------------------
    // 7. Magnetic hover on primary actions (2-3px, fine pointers only)
    // ---------------------------------------------------------------
    function initMagnetic() {
        if (prefersReducedMotion()) return;
        if (!window.matchMedia("(hover: hover) and (pointer: fine)").matches) return;

        var buttons = document.querySelectorAll(
            ".btn-primary, .btn-hp-primary, .btn-contact, .lg-submit, .nx-magnetic"
        );

        function attach(el) {
            var pending = null;
            var offsetX = 0;
            var offsetY = 0;

            function apply() {
                pending = null;
                el.style.setProperty("--nx-mx", offsetX.toFixed(2) + "px");
                el.style.setProperty("--nx-my", offsetY.toFixed(2) + "px");
            }

            el.addEventListener("pointermove", function (event) {
                if (event.pointerType !== "mouse") return;

                var rect = el.getBoundingClientRect();
                var dx = (event.clientX - (rect.left + rect.width / 2)) / (rect.width / 2);
                var dy = (event.clientY - (rect.top + rect.height / 2)) / (rect.height / 2);

                offsetX = Math.max(-1, Math.min(1, dx)) * 2.5;
                offsetY = Math.max(-1, Math.min(1, dy)) * 2;

                if (!pending) pending = requestAnimationFrame(apply);
            });

            el.addEventListener("pointerleave", function () {
                el.style.setProperty("--nx-mx", "0px");
                el.style.setProperty("--nx-my", "0px");
            });
        }

        for (var i = 0; i < buttons.length; i++) attach(buttons[i]);
    }

    // ---------------------------------------------------------------
    // 8. Submit buttons show progress while the post is in flight
    // Validation that cancels the submit leaves the button untouched.
    // ---------------------------------------------------------------
    function initButtonLoading() {
        document.addEventListener("submit", function (event) {
            if (event.defaultPrevented) return;

            var form = event.target;
            if (!form || form.nodeName !== "FORM") return;
            if (form.hasAttribute("data-nx-no-loading")) return;

            var button = event.submitter || form.querySelector('button[type="submit"], input[type="submit"]');
            if (!button || button.classList.contains("is-loading")) return;

            button.classList.add("is-loading");
            button.setAttribute("aria-busy", "true");
        });
    }

    // ---------------------------------------------------------------
    // 9. Soft page transition between internal pages
    // Only anchors that really navigate get the veil, and it fades itself
    // out again if the navigation never happens.
    // ---------------------------------------------------------------
    function initPageTransition() {
        if (prefersReducedMotion()) return;

        var veil = document.createElement("div");
        veil.className = "nx-veil";
        veil.setAttribute("aria-hidden", "true");
        document.body.appendChild(veil);

        var releaseTimer = null;

        document.addEventListener("click", function (event) {
            if (event.defaultPrevented || event.button !== 0) return;
            if (event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) return;

            var link = event.target.closest("a[href]");
            if (!link) return;
            if (link.target && link.target !== "_self") return;
            if (link.hasAttribute("download") || link.hasAttribute("data-nx-no-transition")) return;

            var href = link.getAttribute("href");
            if (!href || href.charAt(0) === "#") return;
            if (/^(mailto:|tel:|javascript:)/i.test(href)) return;

            var url;
            try { url = new URL(link.href, window.location.href); } catch (e) { return; }

            if (url.origin !== window.location.origin) return;
            if (url.pathname === window.location.pathname && url.search === window.location.search) return;

            veil.classList.add("is-active");

            window.clearTimeout(releaseTimer);
            releaseTimer = window.setTimeout(function () {
                veil.classList.remove("is-active");
            }, 1400);
        });

        // Returning through the back/forward cache must never leave the veil up.
        window.addEventListener("pageshow", function () {
            veil.classList.remove("is-active");
        });
    }

    // ---------------------------------------------------------------
    // 10. A small, tasteful celebration on a confirmed payment only
    // ---------------------------------------------------------------
    function initPaymentCelebration() {
        if (prefersReducedMotion()) return;
        if (!/\/Payments\//i.test(window.location.pathname)) return;

        var alertBox = document.querySelector(".alert-success");
        if (!alertBox) return;

        var message = alertBox.textContent || "";
        if (!/payment|receipt|recorded|confirmed/i.test(message)) return;
        if (/reject|fail|declin|error|invalid/i.test(message)) return;

        alertBox.classList.add("nx-celebrate-host");

        for (var i = 0; i < 9; i++) {
            var spark = document.createElement("span");
            spark.className = "nx-celebrate";
            spark.setAttribute("aria-hidden", "true");
            spark.style.left = (10 + Math.random() * 78).toFixed(0) + "%";
            spark.style.top = "14px";
            spark.style.setProperty("--nx-sx", (Math.random() * 64 - 32).toFixed(0) + "px");
            spark.style.setProperty("--nx-sy", (-16 - Math.random() * 26).toFixed(0) + "px");
            spark.style.animationDelay = (i * 35) + "ms";
            alertBox.appendChild(spark);
        }

        window.setTimeout(function () {
            var sparks = alertBox.querySelectorAll(".nx-celebrate");
            for (var s = 0; s < sparks.length; s++) sparks[s].parentNode.removeChild(sparks[s]);
        }, 1600);
    }

    initBootLoader();
    initStagger();
    initScrollReveal();
    initSubmenus();
    initHeroSplit();
    initCountUp();
    initMagnetic();
    initButtonLoading();
    initPageTransition();
    initPaymentCelebration();
})();
