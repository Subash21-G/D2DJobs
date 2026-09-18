
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
    document.querySelector('[data-copy-link]')?.addEventListener('click', async () => {
        const status = document.querySelector('#copy-status');
        try { await navigator.clipboard.writeText(event.currentTarget.dataset.copyText || window.location.href); status.textContent = 'Share text copied.'; }
        catch { status.textContent = 'Copy the URL from your address bar to share this job.'; }
    });
})();

