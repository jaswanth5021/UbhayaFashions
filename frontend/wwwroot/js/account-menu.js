(() => {
    const toggle = document.getElementById('accountMenuToggle');
    const menu = document.getElementById('accountMenuDropdown');
    const accountMenu = toggle?.closest('.account-menu');

    if (!toggle || !menu || !accountMenu) return;

    const setExpanded = expanded => {
        toggle.setAttribute('aria-expanded', String(expanded));
    };

    accountMenu.addEventListener('pointerenter', () => {
        accountMenu.classList.remove('is-dismissed');
        setExpanded(true);
    });

    accountMenu.addEventListener('pointerleave', () => {
        if (!accountMenu.contains(document.activeElement))
            setExpanded(false);
        accountMenu.classList.remove('is-dismissed');
    });

    accountMenu.addEventListener('focusin', () => {
        accountMenu.classList.remove('is-dismissed');
        setExpanded(true);
    });

    accountMenu.addEventListener('focusout', event => {
        if (!accountMenu.contains(event.relatedTarget))
            setExpanded(false);
    });

    accountMenu.addEventListener('keydown', event => {
        if (event.key !== 'Escape') return;
        accountMenu.classList.add('is-dismissed');
        setExpanded(false);
        document.activeElement.blur();
    });
})();
