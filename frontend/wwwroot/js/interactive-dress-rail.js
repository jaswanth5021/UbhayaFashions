(() => {
    const section = document.querySelector("[data-dress-rail-section]");
    if (!section) return;

    const track = section.querySelector("[data-dress-rail-track]");
    const previous = section.querySelector("[data-dress-rail-prev]");
    const next = section.querySelector("[data-dress-rail-next]");
    if (!track) return;

    const scrollAmount = () => Math.max(220, Math.round(track.clientWidth * 0.72));
    previous?.addEventListener("click", () => track.scrollBy({ left: -scrollAmount(), behavior: "smooth" }));
    next?.addEventListener("click", () => track.scrollBy({ left: scrollAmount(), behavior: "smooth" }));

    let pointerDown = false;
    let dragged = false;
    let startX = 0;
    let startScroll = 0;

    track.addEventListener("pointerdown", event => {
        if (event.pointerType === "touch" || event.button !== 0) return;
        pointerDown = true;
        dragged = false;
        startX = event.clientX;
        startScroll = track.scrollLeft;
    });

    track.addEventListener("pointermove", event => {
        if (!pointerDown) return;
        const distance = event.clientX - startX;
        if (Math.abs(distance) > 5) {
            dragged = true;
            track.classList.add("is-dragging");
            track.scrollLeft = startScroll - distance;
        }
    });

    const stopDragging = () => {
        pointerDown = false;
        track.classList.remove("is-dragging");
    };
    track.addEventListener("pointerup", stopDragging);
    track.addEventListener("pointercancel", stopDragging);
    track.addEventListener("mouseleave", stopDragging);

    track.addEventListener("click", event => {
        if (!dragged) return;
        event.preventDefault();
        event.stopPropagation();
        dragged = false;
    }, true);
})();