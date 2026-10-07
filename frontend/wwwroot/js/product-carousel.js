(() => {
    document.querySelectorAll("[data-product-carousel]").forEach(carousel => {
        if (carousel.dataset.carouselInitialized === "true") return;
        carousel.dataset.carouselInitialized = "true";

        const images = [...carousel.querySelectorAll("img[data-carousel-image]")];
        const dots = [...carousel.querySelectorAll("[data-carousel-dot]")];
        const previousButton = carousel.querySelector("[data-carousel-prev]");
        const nextButton = carousel.querySelector("[data-carousel-next]");
        const fallback = carousel.closest(".product-image-wrapper")?.querySelector(".product-image-fallback")
            ?? carousel.closest(".catalog-image-wrap")?.querySelector(".catalog-image-fallback-overlay");
        if (!images.length) return;

        let current = 0;
        let timer = null;
        let touchStartX = 0;
        let isPointerOver = false;

        const availableImages = () => images.filter(image =>
            image.isConnected && image.dataset.carouselFailed !== "true");

        const updateControls = activeImages => {
            const hasMultiple = activeImages.length > 1;
            if (previousButton) previousButton.hidden = !hasMultiple;
            if (nextButton) nextButton.hidden = !hasMultiple;
            const dotTrack = carousel.querySelector(".product-image-dots");
            if (dotTrack) dotTrack.hidden = !hasMultiple;

            dots.forEach(dot => {
                const dotIndex = Number(dot.dataset.carouselDot);
                const image = images.find(item => Number(item.dataset.carouselImage) === dotIndex);
                dot.hidden = !image || image.dataset.carouselFailed === "true";
            });
        };

        const showImage = index => {
            const activeImages = availableImages();
            if (!activeImages.length) {
                carousel.classList.add("is-image-unavailable");
                if (fallback) fallback.hidden = false;
                updateControls(activeImages);
                return;
            }

            carousel.classList.remove("is-image-unavailable");
            current = ((index % activeImages.length) + activeImages.length) % activeImages.length;
            const activeImage = activeImages[current];

            images.forEach(image => image.classList.toggle("is-active", image === activeImage));
            dots.forEach(dot => {
                dot.classList.toggle("is-active", Number(dot.dataset.carouselDot) === Number(activeImage.dataset.carouselImage));
            });

            if (fallback) fallback.hidden = activeImage.complete && activeImage.naturalWidth > 0;
            updateControls(activeImages);
        };

        const handleImageFailure = image => {
            if (image.dataset.carouselFailed === "true") return;
            image.dataset.carouselFailed = "true";
            image.classList.remove("is-active");
            showImage(Math.min(current, availableImages().length - 1));
        };

        images.forEach(image => {
            image.addEventListener("error", () => handleImageFailure(image));
            image.addEventListener("load", () => {
                if (image.classList.contains("is-active") && fallback) fallback.hidden = true;
            });
            if (image.complete && image.naturalWidth === 0) handleImageFailure(image);
        });

        const stop = () => {
            if (!timer) return;
            window.clearInterval(timer);
            timer = null;
        };

        const start = () => {
            if (timer || document.hidden || isPointerOver || availableImages().length < 2) return;
            timer = window.setInterval(() => showImage(current + 1), 2600);
        };

        previousButton?.addEventListener("click", event => {
            event.preventDefault();
            event.stopPropagation();
            stop();
            showImage(current - 1);
        });

        nextButton?.addEventListener("click", event => {
            event.preventDefault();
            event.stopPropagation();
            stop();
            showImage(current + 1);
        });

        dots.forEach(dot => {
            dot.addEventListener("click", event => {
                event.preventDefault();
                event.stopPropagation();
                stop();
                const activeImages = availableImages();
                const requestedIndex = activeImages.findIndex(image =>
                    Number(image.dataset.carouselImage) === Number(dot.dataset.carouselDot));
                if (requestedIndex >= 0) showImage(requestedIndex);
            });
        });

        carousel.addEventListener("mouseenter", () => {
            isPointerOver = true;
            stop();
        });
        carousel.addEventListener("mouseleave", () => {
            isPointerOver = false;
            start();
        });
        carousel.addEventListener("touchstart", event => {
            touchStartX = event.changedTouches[0]?.clientX ?? 0;
            stop();
        }, { passive: true });
        carousel.addEventListener("touchend", event => {
            const distance = (event.changedTouches[0]?.clientX ?? 0) - touchStartX;
            if (Math.abs(distance) >= 35) showImage(current + (distance < 0 ? 1 : -1));
            window.setTimeout(start, 700);
        }, { passive: true });

        if ("IntersectionObserver" in window) {
            const observer = new IntersectionObserver(entries => {
                entries.forEach(entry => entry.isIntersecting ? start() : stop());
            }, { threshold: .15 });
            observer.observe(carousel);
        } else {
            start();
        }

        document.addEventListener("visibilitychange", () => document.hidden ? stop() : start());
        showImage(current);
    });
})();
