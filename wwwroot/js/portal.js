
(() => {
    const nav = document.querySelector('.portal-nav');
    const menu = document.querySelector('.menu-toggle');
    menu?.addEventListener('click', () => { const open = nav.classList.toggle('open'); menu.setAttribute('aria-expanded', String(open)); });
    const theme = document.querySelector('.theme-toggle');
    try { document.body.classList.toggle('dark-mode', localStorage.getItem('darkMode') === 'true'); } catch {}
    theme?.setAttribute('aria-pressed', String(document.body.classList.contains('dark-mode')));
    theme?.addEventListener('click', () => { const dark = document.body.classList.toggle('dark-mode'); theme.setAttribute('aria-pressed', String(dark)); try { localStorage.setItem('darkMode', String(dark)); } catch {} });
    document.querySelector('#sort')?.addEventListener('change', () => document.querySelector('#sort-form').requestSubmit());
    const input = document.querySelector('#jobSearch'), box = document.querySelector('#suggestionsBox');
    if (input && box) {
        let timer, request, sequence = 0;
        const close = () => { box.hidden = true; box.replaceChildren(); };
        input.addEventListener('input', () => {
            clearTimeout(timer); request?.abort(); close();
            const term = input.value.trim(), version = ++sequence;
            if (term.length < 2) return;
            timer = setTimeout(async () => {
                request = new AbortController();
                try {
                    const response = await fetch('/Home/SearchSuggestions?term=' + encodeURIComponent(term), { signal: request.signal });
                    if (!response.ok) return;
                    const items = await response.json();
                    if (version !== sequence) return;
                    close();
                    items.forEach(value => { const button = document.createElement('button'); button.type = 'button'; button.textContent = value; button.addEventListener('click', () => { input.value = value; close(); input.form.requestSubmit(); }); box.append(button); });
                    box.hidden = items.length === 0;
                } catch { if (version === sequence) close(); }
            }, 250);
        });
        input.addEventListener('keydown', event => { if (event.key === 'Escape') { sequence++; clearTimeout(timer); request?.abort(); close(); } if (event.key === 'ArrowDown' && !box.hidden) { event.preventDefault(); box.querySelector('button')?.focus(); } });
        box.addEventListener('keydown', event => { if (event.key === 'Escape') { close(); input.focus(); } });
        document.addEventListener('click', event => { if (!event.target.closest('.autocomplete-field')) { sequence++; clearTimeout(timer); request?.abort(); close(); } });
    }
    document.querySelector('[data-copy-link]')?.addEventListener('click', async (event) => {
        const status = document.querySelector('#copy-status');
        try { await navigator.clipboard.writeText(event.currentTarget.dataset.copyText || window.location.href); status.textContent = 'Share text copied.'; }
        catch { status.textContent = 'Copy the URL from your address bar to share this job.'; }
    });

    const whatsappPopup = document.querySelector('[data-whatsapp-popup]');
    const whatsappClose = document.querySelector('[data-whatsapp-close]');
    let whatsappLauncher = document.querySelector('[data-whatsapp-launcher]');
    if (whatsappPopup && whatsappClose) {
        if (!whatsappLauncher) {
            const groupLink = whatsappPopup.querySelector('.whatsapp-popup-copy>a');
            const widget = whatsappPopup.closest('.whatsapp-widget');
            if (groupLink && widget) {
                whatsappLauncher = document.createElement('a');
                whatsappLauncher.className = 'whatsapp-launcher';
                whatsappLauncher.dataset.whatsappLauncher = '';
                whatsappLauncher.href = groupLink.href;
                whatsappLauncher.target = '_blank';
                whatsappLauncher.rel = 'noopener noreferrer';
                whatsappLauncher.setAttribute('aria-label', 'Join the D2DJobs WhatsApp group');
                whatsappLauncher.hidden = true;
                const icon = document.createElement('i');
                icon.className = 'fa-brands fa-whatsapp';
                icon.setAttribute('aria-hidden', 'true');
                whatsappLauncher.append(icon);
                widget.append(whatsappLauncher);
            }
        }
    }
    if (whatsappPopup && whatsappClose && whatsappLauncher) {
        const dismissalKey = 'd2djobs-whatsapp-invitation-dismissed-v2';
        const wasDismissed = () => { try { return sessionStorage.getItem(dismissalKey) === 'true'; } catch { return false; } };
        const dismissInvitation = () => {
            whatsappPopup.hidden = true;
            whatsappLauncher.hidden = false;
            try { sessionStorage.setItem(dismissalKey, 'true'); } catch {}
        };
        whatsappClose.addEventListener('click', dismissInvitation);
        document.addEventListener('keydown', event => {
            if (event.key === 'Escape' && !whatsappPopup.hidden) {
                dismissInvitation();
            }
        });
        if (wasDismissed()) whatsappLauncher.hidden = false;
        else window.setTimeout(() => {
            if (document.visibilityState === 'visible') whatsappPopup.hidden = false;
        }, 900);
    }
})();

