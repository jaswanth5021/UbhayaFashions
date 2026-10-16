(() => {
    const root = document.getElementById("apiLoader");
    if (!root) return;

    let pending = 0;
    let hideTimer = 0;
    let shownAt = 0;
    const minimumVisibleMs = 280;

    const show = () => {
        window.clearTimeout(hideTimer);
        if (!root.classList.contains("is-active")) {
            shownAt = Date.now();
        }
        root.classList.add("is-active");
        root.setAttribute("aria-hidden", "false");
        document.documentElement.setAttribute("aria-busy", "true");
    };

    const hide = () => {
        const wait = Math.max(0, minimumVisibleMs - (Date.now() - shownAt));
        window.clearTimeout(hideTimer);
        hideTimer = window.setTimeout(() => {
            if (pending > 0) return;
            root.classList.remove("is-active");
            root.setAttribute("aria-hidden", "true");
            document.documentElement.removeAttribute("aria-busy");
        }, wait);
    };

    const begin = () => {
        pending += 1;
        show();
    };

    const end = () => {
        pending = Math.max(0, pending - 1);
        if (pending === 0) hide();
    };

    const isSameOrigin = (url) => {
        try {
            return new URL(url, window.location.href).origin === window.location.origin;
        } catch {
            return false;
        }
    };

    const originalFetch = window.fetch.bind(window);
    window.fetch = (input, init) => {
        const url = typeof input === "string" ? input : input instanceof URL ? input.href : input.url;
        if (!isSameOrigin(url)) return originalFetch(input, init);
        begin();
        return originalFetch(input, init).finally(end);
    };

    const originalOpen = XMLHttpRequest.prototype.open;
    const originalSend = XMLHttpRequest.prototype.send;
    XMLHttpRequest.prototype.open = function (method, url, ...rest) {
        this.__apiLoaderUrl = url;
        return originalOpen.call(this, method, url, ...rest);
    };
    XMLHttpRequest.prototype.send = function (...args) {
        if (isSameOrigin(this.__apiLoaderUrl || "")) {
            begin();
            this.addEventListener("loadend", end, { once: true });
        }
        return originalSend.apply(this, args);
    };

    const armNavigation = (event) => {
        window.setTimeout(() => {
            if (event.defaultPrevented) return;
            begin();
        }, 0);
    };

    document.addEventListener("click", (event) => {
        if (event.defaultPrevented || event.button !== 0) return;
        if (event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) return;
        const anchor = event.target.closest?.("a[href]");
        if (!anchor || anchor.hasAttribute("download")) return;
        if (anchor.target && anchor.target !== "_self") return;
        let url;
        try {
            url = new URL(anchor.href, window.location.href);
        } catch {
            return;
        }
        if (url.origin !== window.location.origin) return;
        if (url.pathname === window.location.pathname && url.search === window.location.search) return;
        armNavigation(event);
    }, true);

    document.addEventListener("submit", (event) => {
        const form = event.target;
        if (!(form instanceof HTMLFormElement)) return;
        if (form.target && form.target !== "_self") return;
        armNavigation(event);
    }, true);

    window.addEventListener("pageshow", (event) => {
        if (!event.persisted) return;
        pending = 0;
        root.classList.remove("is-active");
        root.setAttribute("aria-hidden", "true");
        document.documentElement.removeAttribute("aria-busy");
    });

    const finishInitialLoad = () => {
        if (pending === 0) hide();
    };

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", finishInitialLoad);
    } else {
        finishInitialLoad();
    }
})();
