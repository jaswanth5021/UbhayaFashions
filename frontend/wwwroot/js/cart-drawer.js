(() => {
    const drawer = document.getElementById('cartDrawer');
    const backdrop = document.getElementById('cartDrawerBackdrop');
    const closeButton = document.getElementById('cartDrawerClose');
    const openButton = document.getElementById('cartDrawerOpen');
    const beginCheckout = document.getElementById('cartDrawerBeginCheckout');
    const backToCart = document.getElementById('cartDrawerBackToItems');
    const itemsStep = document.getElementById('cartDrawerItemsStep');
    const addressStep = document.getElementById('cartDrawerAddressStep');
    const footerStep = document.getElementById('cartDrawerFooterStep');
    const addressField = document.getElementById('drawerShippingAddress');
    const savedAddressOptions = [...document.querySelectorAll('input[name="drawerSavedAddress"]')];
    const columns = drawer?.querySelector('.cart-drawer-columns');
    if (!drawer || !backdrop) return;

    const setBagCount = (count) => {
        const link = document.getElementById('cartDrawerOpen');
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
        badge.textContent = String(count);
        badge.setAttribute('aria-label', `${count} items`);
    };

    itemsStep?.addEventListener('submit', async event => {
        const form = event.target.closest('form[data-cart-remove]');
        if (!form) return;
        event.preventDefault();
        const button = form.querySelector('button[type="submit"]');
        if (button) button.disabled = true;
        try {
            const response = await fetch(form.action, {
                method: 'POST',
                body: new FormData(form),
                credentials: 'same-origin',
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            });
            const result = await response.json();
            if (!response.ok || !result.success) throw new Error(result.message || 'Could not remove this item.');

            form.closest('.cart-drawer-item')?.remove();
            setBagCount(result.cartCount);
            const total = drawer.querySelector('.cart-drawer-total strong');
            if (total) total.textContent = `Rs. ${new Intl.NumberFormat('en-IN', { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(result.cartTotal)}`;
            if (result.isEmpty) {
                const empty = document.createElement('p');
                empty.className = 'cart-drawer-empty';
                empty.textContent = 'Your cart is empty.';
                itemsStep.append(empty);
                columns.hidden = true;
                footerStep?.querySelectorAll('.cart-drawer-checkout').forEach(action => { action.hidden = true; });
            }
        } catch (error) {
            const notice = document.createElement('p');
            notice.className = 'cart-drawer-notice is-error';
            notice.setAttribute('role', 'alert');
            notice.textContent = error.message || 'Could not remove this item.';
            drawer.querySelector('.cart-drawer-notice')?.remove();
            columns.after(notice);
            if (button) button.disabled = false;
        }
    });

    const setOpen = (open) => {
        if (open) showAddressStep(false);
        drawer.classList.toggle('is-open', open);
        backdrop.classList.toggle('is-open', open);
        backdrop.hidden = !open;
        drawer.setAttribute('aria-hidden', String(!open));
        document.body.classList.toggle('cart-drawer-open', open);
    };

    const showAddressStep = (show) => {
        if (!itemsStep || !addressStep || !footerStep) return;
        itemsStep.hidden = show;
        addressStep.hidden = !show;
        footerStep.hidden = show;
        if (columns) columns.hidden = show;
        if (show && addressField) {
            const selected = savedAddressOptions.find(option => option.checked);
            if (selected) addressField.value = selected.dataset.address || '';
            addressStep.scrollTop = 0;
        }
    };

    closeButton?.addEventListener('click', () => setOpen(false));
    backdrop.addEventListener('click', () => setOpen(false));
    openButton?.addEventListener('click', (event) => {
        event.preventDefault();
        setOpen(true);
    });
    beginCheckout?.addEventListener('click', () => showAddressStep(true));
    backToCart?.addEventListener('click', () => showAddressStep(false));
    savedAddressOptions.forEach(option => {
        option.addEventListener('change', () => {
            if (addressField) addressField.value = option.dataset.address || '';
        });
    });
    document.getElementById('drawerAddNewAddress')?.addEventListener('click', () => {
        savedAddressOptions.forEach(option => { option.checked = false; });
        if (addressField) {
            addressField.value = '';
            addressField.focus();
        }
    });
    addressField?.addEventListener('input', () => {
        savedAddressOptions.forEach(option => { option.checked = false; });
    });
    document.addEventListener('keydown', (event) => {
        if (event.key === 'Escape') setOpen(false);
    });

    if (drawer.classList.contains('is-open')) setOpen(true);
})();
