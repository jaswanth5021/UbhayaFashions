document.addEventListener("DOMContentLoaded", () => {
    const openButton = document.getElementById("headerSearchOpen");
    const panel = document.getElementById("headerSearchPanel");
    const closeButton = document.getElementById("headerSearchClose");
    const backdrop = document.getElementById("headerSearchBackdrop");
    const form = document.getElementById("headerSearchForm");
    const input = document.getElementById("headerSearchInput");

    if (!openButton || !panel || !closeButton || !backdrop || !form || !input) {
        return;
    }

    const openSearch = () => {
        panel.classList.add("is-open");
        panel.setAttribute("aria-hidden", "false");
        openButton.setAttribute("aria-expanded", "true");
        document.body.classList.add("header-search-open");
        window.setTimeout(() => input.focus(), 50);
    };

    const closeSearch = () => {
        panel.classList.remove("is-open");
        panel.setAttribute("aria-hidden", "true");
        openButton.setAttribute("aria-expanded", "false");
        document.body.classList.remove("header-search-open");
    };

    openButton.addEventListener("click", openSearch);
    closeButton.addEventListener("click", closeSearch);
    backdrop.addEventListener("click", closeSearch);

    document.addEventListener("keydown", (event) => {
        if (event.key === "Escape" && panel.classList.contains("is-open")) {
            closeSearch();
        }
    });

    form.addEventListener("submit", (event) => {
        const search = input.value.trim();

        if (!search) {
            event.preventDefault();
            input.focus();
        }
    });
});
