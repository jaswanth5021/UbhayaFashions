(() => {
    const stack = document.getElementById('appNotificationStack');
    if (!stack) return;

    const symbols = { success: '\u2713', warning: '!', error: '\u00d7', info: 'i' };

    const show = (message, kind = 'info', duration = 6000) => {
        const text = String(message ?? '').trim();
        if (!text) return;
        if (!Object.hasOwn(symbols, kind)) kind = 'info';

        const notice = document.createElement('div');
        notice.className = 'app-notification';
        notice.dataset.kind = kind;
        notice.setAttribute('role', kind === 'warning' || kind === 'error' ? 'alert' : 'status');

        const icon = document.createElement('span');
        icon.className = 'app-notification-icon';
        icon.setAttribute('aria-hidden', 'true');
        icon.textContent = symbols[kind];

        const content = document.createElement('span');
        content.className = 'app-notification-message';
        content.textContent = text;

        const close = document.createElement('button');
        close.className = 'app-notification-close';
        close.type = 'button';
        close.setAttribute('aria-label', 'Dismiss notification');
        close.textContent = '\u00d7';

        let timer;
        const dismiss = () => {
            window.clearTimeout(timer);
            notice.classList.remove('is-visible');
            window.setTimeout(() => notice.remove(), 220);
        };

        close.addEventListener('click', dismiss);
        notice.addEventListener('pointerenter', () => window.clearTimeout(timer));
        notice.addEventListener('pointerleave', () => { timer = window.setTimeout(dismiss, 1800); });
        stack.append(notice);
        requestAnimationFrame(() => notice.classList.add('is-visible'));
        timer = window.setTimeout(dismiss, Math.max(2500, duration));
        while (stack.children.length > 4) stack.firstElementChild.remove();
    };

    window.AppNotifications = { show };

    document.addEventListener('DOMContentLoaded', () => {
        document.querySelectorAll('[data-app-notification], .wishlist-notice:not([data-app-notification]), .product-review-notice:not([data-app-notification]), .auth-card > .success:not([data-app-notification])').forEach(element => {
            const kind = element.dataset.appNotification || (element.classList.contains('error') ? 'warning' : 'success');
            show(element.textContent, kind);
            element.remove();
        });
    });

    // Replace blocking browser alerts with the shared, dismissible warning toast.
    window.alert = message => show(message, 'warning', 7500);
})();
