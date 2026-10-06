(() => {
    document.querySelectorAll("[data-related-track]").forEach(track => {
        const section = track.closest(".product-bestsellers");
        const previous = section?.querySelector("[data-related-prev]");
        const next = section?.querySelector("[data-related-next]");
        if (!previous || !next) return;

        const updateButtons = () => {
            const maxScroll = Math.max(0, track.scrollWidth - track.clientWidth);
            previous.disabled = track.scrollLeft <= 1;
            next.disabled = track.scrollLeft >= maxScroll - 1;
        };

        const scroll = direction => {
            const card = track.querySelector(".product-bestseller-card");
            const gap = card ? parseFloat(getComputedStyle(track).columnGap) || 0 : 0;
            const distance = card ? (card.getBoundingClientRect().width + gap) * 2 : track.clientWidth * .8;
            track.scrollBy({ left: direction * distance, behavior: "smooth" });
        };

        previous.addEventListener("click", () => scroll(-1));
        next.addEventListener("click", () => scroll(1));
        track.addEventListener("scroll", updateButtons, { passive: true });
        window.addEventListener("resize", updateButtons);
        updateButtons();
    });
})();
