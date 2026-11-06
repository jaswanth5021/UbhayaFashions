(() => {
    const slider = document.querySelector('[data-hero-slider]');
    if (!slider) return;

    const slides = [...slider.querySelectorAll('[data-hero-slide]')];
    const dots = [...slider.querySelectorAll('[data-hero-dot]')];
    if (slides.length < 2) return;

    const pauseButton = slider.querySelector('[data-hero-pause]');
    const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)');
    let current = 0;
    let timer;
    let pointerInside = false;
    let pausedByUser = false;
    let rotationEnabledByUser = false;

    const isPaused = () => pausedByUser || (reducedMotion.matches && !rotationEnabledByUser);

    const updatePauseButton = () => {
        if (!pauseButton) return;

        const paused = isPaused();
        pauseButton.textContent = paused ? 'Play' : 'Pause';
        pauseButton.setAttribute('aria-label', paused ? 'Start automatic banner rotation' : 'Pause automatic banner rotation');
    };

    const show = index => {
        current = (index + slides.length) % slides.length;
        slides.forEach((slide, i) => {
            const active = i === current;
            slide.classList.toggle('is-active', active);
            slide.setAttribute('aria-hidden', active ? 'false' : 'true');
        });
        dots.forEach((dot, i) => {
            const active = i === current;
            dot.classList.toggle('is-active', active);
            dot.setAttribute('aria-current', active ? 'true' : 'false');
        });
    };

    const scheduleRotation = () => {
        clearInterval(timer);
        updatePauseButton();
        if (isPaused() || pointerInside || document.hidden) return;
        timer = setInterval(() => show(current + 1), 5000);
    };

    slider.querySelector('[data-hero-prev]')?.addEventListener('click', () => {
        show(current - 1);
        scheduleRotation();
    });

    slider.querySelector('[data-hero-next]')?.addEventListener('click', () => {
        show(current + 1);
        scheduleRotation();
    });

    dots.forEach(dot => {
        dot.addEventListener('click', () => {
            show(Number(dot.dataset.heroDot));
            scheduleRotation();
        });
    });

    pauseButton?.addEventListener('click', () => {
        if (isPaused()) {
            pausedByUser = false;
            rotationEnabledByUser = true;
        } else {
            pausedByUser = true;
        }
        scheduleRotation();
    });

    slider.addEventListener('mouseenter', () => {
        pointerInside = true;
        scheduleRotation();
    });
    slider.addEventListener('mouseleave', () => {
        pointerInside = false;
        scheduleRotation();
    });
    document.addEventListener('visibilitychange', scheduleRotation);
    reducedMotion.addEventListener?.('change', scheduleRotation);

    show(0);
    scheduleRotation();
})();
