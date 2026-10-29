(() => {
    const header = document.querySelector(".header");
    if (!header) return;

    let scheduled = false;
    const update = () => {
        header.classList.toggle("is-scrolled", window.scrollY > 12);
        scheduled = false;
    };

    window.addEventListener("scroll", () => {
        if (scheduled) return;
        scheduled = true;
        window.requestAnimationFrame(update);
    }, { passive: true });

    update();
})();
