(() => {
    const form = document.querySelector('#resume-form');
    if (!form) return;
    const fields = [...form.querySelectorAll('input[name], textarea[name]')];
    const status = document.querySelector('#resume-status');
    const key = 'd2djobs.resume.v1';
    const render = () => {
        const values = Object.fromEntries(fields.map(field => [field.name, field.value.trim()]));
        document.querySelectorAll('[data-preview]').forEach(element => {
            const name = element.dataset.preview;
            element.textContent = values[name] || (name === 'name' ? 'Your name' : '');
        });
        document.querySelector('#preview-contact').textContent = ['email', 'phone', 'location'].map(name => values[name]).filter(Boolean).join('  ·  ');
        document.querySelectorAll('[data-resume-section]').forEach(section => { section.hidden = !values[section.dataset.resumeSection]; });
        document.querySelector('#resume-empty').hidden = ['summary', 'education', 'experience', 'projects', 'skills', 'certifications'].some(name => values[name]);
    };
    form.addEventListener('input', render);
    form.addEventListener('submit', event => {
        event.preventDefault();
        if (!form.elements.name.value.trim()) { status.textContent = 'Enter your full name before printing.'; form.elements.name.focus(); return; }
        render(); window.print();
    });
    document.querySelector('#save-resume').addEventListener('click', () => {
        try { localStorage.setItem(key, JSON.stringify(Object.fromEntries(fields.map(field => [field.name, field.value])))); status.textContent = 'Draft saved on this device. Save again after making changes.'; }
        catch { status.textContent = 'This browser could not save your draft. You can still print or save a PDF.'; }
    });
    document.querySelector('#load-resume').addEventListener('click', () => {
        try {
            const saved = localStorage.getItem(key);
            if (!saved) { status.textContent = 'No saved draft on this device.'; return; }
            const values = JSON.parse(saved);
            if (!values || typeof values !== 'object' || Array.isArray(values)) throw new Error('Invalid draft');
            if (fields.some(field => field.value) && !confirm('Replace the current editor contents with your saved draft?')) return;
            fields.forEach(field => { field.value = typeof values[field.name] === 'string' ? values[field.name].slice(0, field.maxLength) : ''; });
            render(); status.textContent = 'Saved draft loaded.';
        } catch { status.textContent = 'Could not load the saved draft.'; }
    });
    document.querySelector('#clear-resume').addEventListener('click', () => {
        if (!confirm('Clear the editor and delete the saved resume draft from this device?')) return;
        fields.forEach(field => { field.value = ''; }); render();
        try { localStorage.removeItem(key); status.textContent = 'Resume and saved draft cleared.'; }
        catch { status.textContent = 'Editor cleared. Browser storage could not be cleared; remove site data in browser settings.'; }
    });
    render();
})();
