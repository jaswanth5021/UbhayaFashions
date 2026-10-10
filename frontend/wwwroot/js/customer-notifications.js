(() => {
    const menu = document.getElementById('customerNotificationMenu');
    const toggle = document.getElementById('customerNotificationToggle');
    const panel = document.getElementById('customerNotificationPanel');
    const items = document.getElementById('customerNotificationItems');
    const countBadge = document.getElementById('customerNotificationCount');
    const token = document.querySelector('.customer-notification-antiforgery input[name="__RequestVerificationToken"]')?.value;
    const list = document.querySelector('[data-notification-list]');
    if (!menu && !list) return;

    let unreadCount = 0;
    let loaded = false;

    const updateBadge = count => {
        unreadCount = Math.max(0, Number(count) || 0);
        if (!countBadge) return;
        countBadge.textContent = unreadCount > 99 ? '99+' : String(unreadCount);
        countBadge.hidden = unreadCount === 0;
        toggle?.setAttribute('aria-label', unreadCount ? `Notifications, ${unreadCount} unread` : 'Notifications');
    };

    const safeLink = value => typeof value === 'string' && value.startsWith('/') && !value.startsWith('//') ? value : '/Products';
    const escapeAttribute = value => String(value ?? '').replace(/[&<>"']/g, character => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[character]);
    const escapeMarkup = value => {
        const span = document.createElement('span');
        span.textContent = value ?? '';
        return span.innerHTML;
    };

    const renderMenu = data => {
        if (!items) return;
        updateBadge(data.unreadCount);
        const notifications = Array.isArray(data.items) ? data.items : [];
        if (notifications.length === 0) {
            items.innerHTML = '<div class="customer-notification-empty"><span aria-hidden="true">✦</span><strong>You’re all caught up</strong><small>New arrivals will appear here.</small></div>';
            return;
        }
        items.innerHTML = notifications.map(item => `
            <article class="customer-notification-item ${item.readDate ? '' : 'is-unread'}" data-notification-id="${Number(item.id)}">
                ${item.productImageUrl ? `<img class="customer-notification-image" src="${escapeAttribute(item.productImageUrl)}" alt="" loading="lazy">` : '<span class="customer-notification-spark" aria-hidden="true">✦</span>'}
                <div class="customer-notification-copy">
                    <a href="${safeLink(item.link)}" data-notification-open><strong>${escapeMarkup(item.title)}</strong><span>${escapeMarkup(item.message)}</span></a>
                    <time>${new Date(item.createdDate).toLocaleDateString(undefined, { day: 'numeric', month: 'short' })}</time>
                </div>
                <button type="button" class="customer-notification-dismiss" aria-label="Dismiss notification" data-notification-dismiss>×</button>
            </article>`).join('');
    };

    const loadRecent = async (force = false) => {
        if (!items || (loaded && !force)) return;
        items.innerHTML = '<p class="customer-notification-loading">Loading updates…</p>';
        try {
            const response = await fetch('/Notifications/Recent', { credentials: 'same-origin', headers: { 'X-Requested-With': 'XMLHttpRequest' }, cache: 'no-store' });
            if (response.status === 401) {
                const result = await response.json();
                const loginUrl = safeLink(result.loginUrl);
                items.innerHTML = `<p class="customer-notification-loading is-error">${escapeMarkup(result.message || 'Your sign-in session has expired.')} <a href="${loginUrl}">Sign in again</a>.</p>`;
                return;
            }
            if (!response.ok) throw new Error('Notifications are temporarily unavailable.');
            renderMenu(await response.json());
            loaded = true;
        } catch {
            items.innerHTML = '<p class="customer-notification-loading is-error">Could not load notifications. Try again.</p>';
        }
    };

    const postAction = async (id, action) => {
        const response = await fetch(`/Notifications/${id}/${action}`, {
            method: 'POST',
            credentials: 'same-origin',
            headers: { 'X-Requested-With': 'XMLHttpRequest', 'RequestVerificationToken': token || '' }
        });
        if (!response.ok) throw new Error('Notification could not be updated.');
        return response.json();
    };

    const deleteNotification = async card => {
        const id = Number(card?.dataset.notificationId);
        if (!id) return;
        const response = await fetch(`/Notifications/${id}/Dismiss`, {
            method: 'POST',
            credentials: 'same-origin',
            headers: { 'X-Requested-With': 'XMLHttpRequest', 'RequestVerificationToken': token || '' }
        });
        if (!response.ok || !(await response.json()).success) throw new Error('Notification could not be dismissed.');
        card.remove();
        if (card.classList.contains('is-unread')) updateBadge(unreadCount - 1);
        if (items && !items.querySelector('.customer-notification-item')) {
            items.innerHTML = '<div class="customer-notification-empty"><span aria-hidden="true">✦</span><strong>You’re all caught up</strong><small>New arrivals will appear here.</small></div>';
        }
    };

    toggle?.addEventListener('click', async () => {
        const open = toggle.getAttribute('aria-expanded') !== 'true';
        toggle.setAttribute('aria-expanded', String(open));
        menu.classList.toggle('is-open', open);
        panel.hidden = !open;
        if (open) await loadRecent();
    });

    document.addEventListener('click', event => {
        if (menu && !menu.contains(event.target)) {
            menu.classList.remove('is-open');
            panel.hidden = true;
            toggle?.setAttribute('aria-expanded', 'false');
        }
    });

    document.addEventListener('keydown', event => {
        if (event.key !== 'Escape' || !menu?.classList.contains('is-open')) return;
        menu.classList.remove('is-open');
        panel.hidden = true;
        toggle?.setAttribute('aria-expanded', 'false');
        toggle.focus();
    });

    menu?.addEventListener('click', async event => {
        const dismiss = event.target.closest('[data-notification-dismiss]');
        const open = event.target.closest('[data-notification-open]');
        const card = event.target.closest('[data-notification-id]');
        if (dismiss) {
            event.preventDefault();
            try { await deleteNotification(card); } catch { window.AppNotifications?.show('Could not dismiss this notification.', 'warning'); }
        } else if (open && card?.classList.contains('is-unread')) {
            const href = safeLink(open.getAttribute('href'));
            try { await postAction(card.dataset.notificationId, 'Read'); } catch { /* The product page remains available if reading cannot be recorded. */ }
            window.location.assign(href);
        }
    });

    list?.addEventListener('click', async event => {
        const dismiss = event.target.closest('[data-notification-dismiss]');
        const open = event.target.closest('[data-notification-open]');
        const card = event.target.closest('[data-notification-id]');
        if (dismiss) {
            try {
                await deleteNotification(card);
                if (!list.querySelector('[data-notification-id]')) window.location.reload();
            } catch { window.AppNotifications?.show('Could not dismiss this notification.', 'warning'); }
        } else if (open && card?.classList.contains('is-unread')) {
            try { await postAction(card.dataset.notificationId, 'Read'); } catch { /* Continue to the product page. */ }
            window.location.assign(safeLink(open.getAttribute('href')));
        }
    });

    loadRecent();
    window.setInterval(() => loadRecent(true), 60000);
})();
