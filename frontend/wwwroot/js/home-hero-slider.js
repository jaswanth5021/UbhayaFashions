(() => {
    const slider = document.querySelector('[data-hero-slider]');
    if (!slider) return;

    const slides = [...slider.querySelectorAll('[data-hero-slide]')];
    const dots = [...slider.querySelectorAll('[data-hero-dot]')];
    if (slides.length < 2) return;

    let current = 0;
    let timer;

    const show = index => {
        current = (index + slides.length) % slides.length;
        slides.forEach((slide, i) => slide.classList.toggle('is-active', i === current));
        dots.forEach((dot, i) => dot.classList.toggle('is-active', i === current));
    };

    const restart = () => {
        clearInterval(timer);
        timer = setInterval(() => show(current + 1), 5000);
    };

    slider.querySelector('[data-hero-prev]')?.addEventListener('click', () => {
        show(current - 1);
        restart();
    });

    slider.querySelector('[data-hero-next]')?.addEventListener('click', () => {
        show(current + 1);
        restart();
    });

    dots.forEach(dot => {
        dot.addEventListener('click', () => {
            show(Number(dot.dataset.heroDot));
            restart();
        });
    });

    slider.addEventListener('mouseenter', () => clearInterval(timer));
    slider.addEventListener('mouseleave', restart);

    show(0);
    restart();
})();
