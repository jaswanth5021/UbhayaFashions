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

    let isDragging = false;
    let startX = 0;
    let startScroll = 0;
    track.addEventListener("pointerdown", event => {
        if (event.pointerType === "touch" || event.target.closest("a")) return;
        isDragging = true;
        startX = event.clientX;
        startScroll = track.scrollLeft;
        track.classList.add("is-dragging");
        track.setPointerCapture(event.pointerId);
    });
    track.addEventListener("pointermove", event => {
        if (!isDragging) return;
        track.scrollLeft = startScroll - (event.clientX - startX);
    });
    const stopDragging = () => {
        isDragging = false;
        track.classList.remove("is-dragging");
    };
    track.addEventListener("pointerup", stopDragging);
    track.addEventListener("pointercancel", stopDragging);
    track.addEventListener("lostpointercapture", stopDragging);
})();