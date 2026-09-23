(() => {
    'use strict';

    const notice = document.getElementById('d2djobs-support-notice');
    const dismissalKey = 'd2djobs_adblock_notice_dismissed';
    if (!notice || typeof notice.showModal !== 'function') return;

    // Fail open if storage is unavailable, so navigation cannot cause repeated notices.
    try {
        if (sessionStorage.getItem(dismissalKey) === '1') return;
        sessionStorage.setItem(dismissalKey, '0');
    } catch { return; }

    let previousFocus;
    const dismiss = () => {
        try { sessionStorage.setItem(dismissalKey, '1'); } catch { /* Keep browsing available. */ }
        notice.close();
    };
    notice.querySelector('[data-support-dismiss]').addEventListener('click', dismiss);
    notice.querySelector('[data-support-refresh]').addEventListener('click', () => window.location.reload());
    notice.addEventListener('cancel', event => { event.preventDefault(); dismiss(); });
    notice.addEventListener('close', () => {
        if (previousFocus?.isConnected) previousFocus.focus();
    });

    function makeProbe(className) {
        const probe = document.createElement('div');
        probe.className = className;
        probe.setAttribute('aria-hidden', 'true');
        // Off-screen, but deliberately measurable: hidden/display:none would invalidate the check.
        probe.style.cssText = 'position:fixed;left:-10000px;top:0;width:10px;height:10px;pointer-events:none;';
        document.body.appendChild(probe);
        return probe;
    }

    function isHidden(probe) {
        if (!probe.isConnected) return true;
        const style = window.getComputedStyle(probe);
        return style.display === 'none' || style.visibility === 'hidden' ||
            style.visibility === 'collapse' || style.opacity === '0' ||
            probe.getBoundingClientRect().width === 0 || probe.getBoundingClientRect().height === 0;
    }

    function check() {
        if (document.visibilityState !== 'visible') return;
        const control = makeProbe('d2djobs-layout-probe');
        const bait = makeProbe('adsbox ad-banner ad-placement adsbygoogle');
        const cleanup = () => { bait.remove(); control.remove(); };
        const blocked = () => !isHidden(control) && isHidden(bait);

        window.setTimeout(() => {
            if (!blocked()) { cleanup(); return; }
            window.setTimeout(() => {
                const confirmed = document.visibilityState === 'visible' && blocked();
                cleanup();
                if (!confirmed || document.querySelector('dialog[open], .modal.show')) return;
                previousFocus = document.activeElement;
                notice.showModal(); // Native dialog provides focus containment and an inert background.
            }, 150);
        }, 250);
    }

    // No network requests or dependencies on either advertising provider.
    const schedule = () => window.setTimeout(check, 1250);
    if (document.readyState === 'complete') schedule();
    else window.addEventListener('load', schedule, { once: true });
})();
