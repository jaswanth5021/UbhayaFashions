(() => {
    const section = document.querySelector('.featured-products');
    if (!section) return;

    const carousels = [...section.querySelectorAll('[data-product-carousel]')];

    carousels.forEach(carousel => {
        const images = [...carousel.querySelectorAll('[data-carousel-image]')];
        const dots = [...carousel.querySelectorAll('[data-carousel-dot]')];

        if (images.length < 2) return;

        let current = 0;
        let timer = null;

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

        const start = () => {
            if (timer) return;
            timer = window.setInterval(nextImage, 1600);
        };

        const stop = () => {
            if (!timer) return;
            window.clearInterval(timer);
            timer = null;
            showImage(0);
        };

        // Desktop: start the automatic image rotation when the product is hovered,
        // similar to fashion marketplaces such as Myntra.
        carousel.closest('.product-card')?.addEventListener('mouseenter', start);
        carousel.closest('.product-card')?.addEventListener('mouseleave', stop);

        // Mobile/touch: allow swiping through every product image and also
        // rotate automatically while the card is visible.
        let touchStartX = 0;

        carousel.addEventListener('touchstart', event => {
            touchStartX = event.changedTouches[0]?.clientX ?? 0;
        }, { passive: true });

        carousel.addEventListener('touchend', event => {
            const touchEndX = event.changedTouches[0]?.clientX ?? 0;
            const distance = touchEndX - touchStartX;

            if (Math.abs(distance) < 35) return;

            showImage(distance < 0 ? current + 1 : current - 1);
        }, { passive: true });

        if (window.matchMedia('(hover: none)').matches) {
            start();
        }

        dots.forEach((dot, index) => {
            dot.addEventListener('click', event => {
                event.preventDefault();
                event.stopPropagation();
                showImage(index);
            });
        });
    });

    let feedback = document.querySelector('.new-arrivals-toast');
    if (!feedback) {
        feedback = document.createElement('div');
        feedback.className = 'new-arrivals-toast';
        feedback.setAttribute('role', 'status');
        feedback.setAttribute('aria-live', 'polite');
        feedback.hidden = true;
        document.body.append(feedback);
    }
    let feedbackTimer;
    const announce = (message, isError = false) => {
        if (!feedback) return;
        feedback.textContent = message;
        feedback.hidden = false;
        feedback.classList.toggle('is-error', isError);
        feedback.classList.remove('is-visible');
        requestAnimationFrame(() => feedback.classList.add('is-visible'));
        clearTimeout(feedbackTimer);
        feedbackTimer = setTimeout(() => {
            feedback.classList.remove('is-visible');
            setTimeout(() => { feedback.hidden = true; }, 220);
        }, 3500);
    };

    const updateCount = (link, count) => {
        if (!link) return;
        let badge = link.querySelector('.header-icon-count');
        if (count < 1) {
            badge?.remove();
            return;
        }
        if (!badge) {
            badge = document.createElement('span');
            badge.className = 'header-icon-count';
            link.prepend(badge);
        }
        badge.textContent = count;
        badge.setAttribute('aria-label', `${count} items`);
    };

    section.addEventListener('click', async event => {
        const sizeButton = event.target.closest('.size-options button:not(:disabled)');
        if (sizeButton && section.contains(sizeButton)) {
            const card = sizeButton.closest('.product-card');
            card.querySelectorAll('.size-options button').forEach(button => {
                button.classList.toggle('is-selected', button === sizeButton);
                button.setAttribute('aria-pressed', String(button === sizeButton));
            });
            return;
        }

        const bagButton = event.target.closest('.add-to-bag');
        if (bagButton && section.contains(bagButton)) {
            const card = bagButton.closest('.product-card');
            const selectedSize = card.querySelector('.size-options button.is-selected');
            if (card.querySelector('.size-options') && !selectedSize) {
                announce('Please select a size first.', true);
                return;
            }
            const token = card.querySelector('input[name="__RequestVerificationToken"]') || section.querySelector('input[name="__RequestVerificationToken"]');
            const body = new FormData();
            body.append('productId', bagButton.dataset.productId);
            body.append('quantity', '1');
            if (selectedSize) body.append('size', selectedSize.dataset.size);
            if (token) body.append('__RequestVerificationToken', token.value);
            bagButton.disabled = true;
            try {
                const response = await fetch('/Cart/Add', { method: 'POST', body, credentials: 'same-origin', headers: { 'X-Requested-With': 'XMLHttpRequest' } });
                const result = await response.json();
                if (!response.ok || !result.success) throw new Error(result.message || 'Could not add this item to your bag.');
                updateCount(document.getElementById('cartDrawerOpen'), result.cartCount);
                announce(result.message);
            } catch (error) {
                announce(error.message || 'Could not add this item to your bag.', true);
            } finally {
                bagButton.disabled = false;
            }
            return;
        }

        const wishlistButton = event.target.closest('.wishlist-button');
        if (!wishlistButton || !section.contains(wishlistButton)) return;
        event.preventDefault();
        const form = wishlistButton.closest('form');
        if (!form || wishlistButton.disabled) return;
        wishlistButton.disabled = true;
        try {
            const response = await fetch(form.action, { method: 'POST', body: new FormData(form), credentials: 'same-origin', headers: { 'X-Requested-With': 'XMLHttpRequest' } });
            const result = await response.json();
            if (!response.ok || !result.success) throw new Error(result.message || 'Could not update your wishlist.');
            const nowSaved = result.isWishlisted;
            wishlistButton.classList.toggle('is-saved', nowSaved);
            wishlistButton.title = nowSaved ? 'Remove from wishlist' : 'Add to wishlist';
            wishlistButton.setAttribute('aria-label', `${nowSaved ? 'Remove' : 'Add'} ${wishlistButton.dataset.productName} ${nowSaved ? 'from' : 'to'} wishlist`);
            form.querySelector('[name="isWishlisted"]').value = String(nowSaved);
            const countBadge = document.querySelector('.wishlist-header-icon .header-icon-count');
            const currentCount = Number(countBadge?.textContent || 0);
            updateCount(document.querySelector('.wishlist-header-icon'), Math.max(0, currentCount + (nowSaved ? 1 : -1)));
            announce(result.message);
        } catch (error) {
            announce(error.message || 'Could not update your wishlist.', true);
        } finally {
            wishlistButton.disabled = false;
        }
    });
})();
