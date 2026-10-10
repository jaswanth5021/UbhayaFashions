(() => {
    const toggle = document.getElementById('accountMenuToggle');
    const menu = document.getElementById('accountMenuDropdown');
    const accountMenu = toggle?.closest('.account-menu');

    if (!toggle || !menu || !accountMenu) return;

    const setExpanded = expanded => {
        toggle.setAttribute('aria-expanded', String(expanded));
    };

    const closeMenu = () => {
        accountMenu.classList.remove('is-open');
        setExpanded(false);
    };

    toggle.addEventListener('click', () => {
        const expanded = toggle.getAttribute('aria-expanded') === 'true';
        accountMenu.classList.toggle('is-open', !expanded);
        setExpanded(!expanded);
    });

    document.addEventListener('click', event => {
        if (!accountMenu.contains(event.target)) closeMenu();
    });

    accountMenu.addEventListener('focusout', event => {
        if (!accountMenu.contains(event.relatedTarget)) closeMenu();
    });

    accountMenu.addEventListener('keydown', event => {
        if (event.key !== 'Escape') return;
        closeMenu();
        toggle.focus();
    });
})();
