(() => {
    const carousels = [...document.querySelectorAll('[data-product-carousel]')];

    carousels.forEach(carousel => {
        if (carousel.dataset.carouselInitialized === 'true') return;

        const images = [...carousel.querySelectorAll('[data-carousel-image]')];
        const dots = [...carousel.querySelectorAll('[data-carousel-dot]')];
        const previousButton = carousel.querySelector('[data-carousel-prev]');
        const nextButton = carousel.querySelector('[data-carousel-next]');

        if (images.length < 2) return;

        carousel.dataset.carouselInitialized = 'true';

        let current = 0;
        let timer = null;
        let touchStartX = 0;
        let isPointerOver = false;

        const showImage = index => {
            current = (index + images.length) % images.length;

            images.forEach((image, imageIndex) => {
                image.classList.toggle('is-active', imageIndex === current);
            });

            dots.forEach((dot, dotIndex) => {
                dot.classList.toggle('is-active', dotIndex === current);
            });

            const count = carousel.querySelector('.product-image-count');
            if (count) count.textContent = `${current + 1}/${images.length}`;
        };

        const nextImage = () => showImage(current + 1);
        const previousImage = () => showImage(current - 1);

        previousButton?.addEventListener('click', event => {
            event.preventDefault();
            event.stopPropagation();
            previousImage();
        });

        nextButton?.addEventListener('click', event => {
            event.preventDefault();
            event.stopPropagation();
            nextImage();
        });

        const start = () => {
            if (timer || document.hidden || isPointerOver) return;
            timer = window.setInterval(nextImage, 2200);
        };

        const stop = () => {
            if (!timer) return;
            window.clearInterval(timer);
            timer = null;
        };

        carousel.addEventListener('mouseenter', () => {
            isPointerOver = true;
            stop();
        });

        carousel.addEventListener('mouseleave', () => {
            isPointerOver = false;
            start();
        });

        carousel.addEventListener('touchstart', event => {
            touchStartX = event.changedTouches[0]?.clientX ?? 0;
            stop();
        }, { passive: true });

        carousel.addEventListener('touchend', event => {
            const touchEndX = event.changedTouches[0]?.clientX ?? 0;
            const distance = touchEndX - touchStartX;

            if (Math.abs(distance) >= 35) {
                showImage(distance < 0 ? current + 1 : current - 1);
            }

            window.setTimeout(start, 700);
        }, { passive: true });

        dots.forEach((dot, index) => {
            dot.addEventListener('click', event => {
                event.preventDefault();
                event.stopPropagation();
                showImage(index);
            });
        });

        const observer = 'IntersectionObserver' in window
            ? new IntersectionObserver(entries => {
                entries.forEach(entry => {
                    if (entry.isIntersecting) {
                        start();
                    } else {
                        stop();
                    }
                });
            }, { threshold: 0.15 })
            : null;

        if (observer) {
            observer.observe(carousel);
        } else {
            start();
        }

        document.addEventListener('visibilitychange', () => {
            if (document.hidden) {
                stop();
            } else {
                start();
            }
        });

        showImage(0);
    });
})();
