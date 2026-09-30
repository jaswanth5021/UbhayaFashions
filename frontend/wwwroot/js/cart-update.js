(() => {
    document.addEventListener('change', event => {
        const select = event.target;
        if (!(select instanceof HTMLSelectElement)) return;
        const form = select.closest('[data-cart-update]');
        if (!form) return;

        if (select.matches('[data-cart-size]')) {
            const stock = Number(select.selectedOptions[0]?.dataset.stock || 0);
            const quantity = form.querySelector('[data-cart-quantity]');
            if (!quantity || stock < 1) return;
            const previous = Number(quantity.value) || 1;
            quantity.replaceChildren();
            for (let qty = 1; qty <= stock; qty++) {
                const option = document.createElement('option');
                option.value = String(qty);
                option.textContent = String(qty);
                option.selected = qty === Math.min(previous, stock);
                quantity.append(option);
            }
        }

        if (form.dataset.saving === 'true') return;
        form.dataset.saving = 'true';
        form.requestSubmit();
    });
})();
