(() => {
    const form = document.querySelector('#resume-form');
    if (!form) return;

    const fields = [...form.querySelectorAll('input[name], textarea[name]')];
    const status = document.querySelector('#resume-status');
    const preview = document.querySelector('#resume-preview');
    const sectionList = document.querySelector('#resume-section-list');
    const sectionControls = document.querySelector('#section-controls');
    const customEditorList = document.querySelector('#custom-section-editors');
    const key = 'd2djobs.resume.v1';
    const templates = {
        graduate: { label: 'Fresher graduate | clean and confident', role: 'Graduate / Fresher', color: '#2453A6', experience: 'Internship / Industrial Training', heading: 'INTERNSHIP / INDUSTRIAL TRAINING', skills: 'Technical skills: your tools and technologies\nCore skills: strengths relevant to the role', experienceHint: 'Internship / Training | Organization | Dates\nDescribe your responsibilities and work. Add only results you can verify.', projects: 'Project name | Tools used\nWhat you built, your contribution and the outcome.', credentials: 'Certification / achievement | Issuer | Year' },
        developer: { label: 'Software developer | modern technical', role: 'Software Developer', color: '#156F74', experience: 'Professional Experience', heading: 'PROFESSIONAL EXPERIENCE', skills: 'Languages: your programming languages\nFrameworks: your libraries and platforms\nTools: databases, cloud and developer tools', experienceHint: 'Job title | Company | Dates\nDescribe features, APIs, testing or releases you worked on.', projects: 'Project name | Tech stack | Repository or demo\nDescribe the feature you built and your contribution.', credentials: 'Technology certification | Issuer | Year' },
        analyst: { label: 'Data analyst | clear and analytical', role: 'Data Analyst', color: '#25764C', experience: 'Analytics Experience', heading: 'ANALYTICS EXPERIENCE', skills: 'Data: SQL, spreadsheets, statistics\nBI: reporting and visualization tools\nOther tools relevant to your work', experienceHint: 'Analyst role | Organization | Dates\nDescribe analysis, reporting or data quality work you performed.', projects: 'Analysis / dashboard | Dataset and tools\nDescribe the question, method and finding.', credentials: 'Analytics certification | Issuer | Year' },
        mechanical: { label: 'Mechanical design | structured engineering', role: 'Mechanical Design Engineer', color: '#5157A1', experience: 'Engineering Experience', heading: 'ENGINEERING EXPERIENCE', skills: 'CAD: software you use\nEngineering: design, drawing or manufacturing skills\nStandards and tools relevant to the role', experienceHint: 'Engineering role | Organization | Dates\nDescribe design, drawing, testing or production work you performed.', projects: 'Design project | CAD / tools\nDescribe the design challenge and your contribution.', credentials: 'Engineering certification | Issuer | Year' },
        engineering: { label: 'Engineering fresher | practical and precise', role: 'Engineering Graduate', color: '#1B697D', experience: 'Industrial Training', heading: 'INDUSTRIAL TRAINING', skills: 'Technical skills: methods and equipment\nQuality / production tools you know\nSafety and collaboration skills', experienceHint: 'Training / internship | Organization | Dates\nDescribe the process, equipment or quality work you learned.', projects: 'Engineering project | Tools / methods\nDescribe your objective, contribution and outcome.', credentials: 'Technical certification | Issuer | Year' },
        support: { label: 'Customer support | friendly and clear', role: 'Customer Support Specialist', color: '#416C9B', experience: 'Work Experience / Internship', heading: 'WORK EXPERIENCE / INTERNSHIP', skills: 'Customer communication and service\nSupport channels / CRM tools\nLanguages and troubleshooting skills', experienceHint: 'Support role | Organization | Dates\nDescribe how you handled customer questions or resolved issues.', projects: 'Service improvement / project\nDescribe the process you supported and your contribution.', credentials: 'Customer service certification | Issuer | Year' },
        sales: { label: 'Sales & marketing | energetic and focused', role: 'Sales & Marketing Associate', color: '#A35E30', experience: 'Sales / Marketing Experience', heading: 'SALES / MARKETING EXPERIENCE', skills: 'Sales: prospecting, CRM, negotiation\nMarketing: content, campaigns, analytics\nPlatforms and tools you use', experienceHint: 'Sales / marketing role | Organization | Dates\nDescribe outreach, campaigns or client work; include verified results only.', projects: 'Campaign / market project | Channels\nDescribe the audience, your contribution and outcome.', credentials: 'Sales / marketing certification | Issuer | Year' },
        finance: { label: 'Finance & accounts | polished and precise', role: 'Finance / Accounts Associate', color: '#3F5888', experience: 'Accounting Experience', heading: 'ACCOUNTING EXPERIENCE', skills: 'Accounting: reconciliation, ledger, reporting\nTools: spreadsheet / accounting software\nCompliance and documentation skills', experienceHint: 'Accounts role | Organization | Dates\nDescribe reconciliations, records or reports you handled.', projects: 'Finance / accounting project | Tools\nDescribe the records analyzed or process improved.', credentials: 'Finance certification | Issuer | Year' },
        hr: { label: 'HR & recruitment | warm and professional', role: 'HR / Recruitment Associate', color: '#805A8C', experience: 'HR / Recruitment Experience', heading: 'HR / RECRUITMENT EXPERIENCE', skills: 'Recruitment: sourcing, screening, coordination\nHR operations and employee support\nHRIS / ATS tools you use', experienceHint: 'HR role | Organization | Dates\nDescribe hiring coordination, onboarding or employee support work.', projects: 'People / process project | Tools\nDescribe the HR process and your contribution.', credentials: 'HR certification | Issuer | Year' },
        operations: { label: 'Operations & logistics | organized and direct', role: 'Operations / Logistics Associate', color: '#346B62', experience: 'Operations Experience', heading: 'OPERATIONS EXPERIENCE', skills: 'Inventory, procurement or supply chain\nPlanning, documentation and coordination\nSystems / tools you use', experienceHint: 'Operations role | Organization | Dates\nDescribe inventory, purchasing or logistics work you performed.', projects: 'Operations project | Process / tools\nDescribe the workflow and your contribution.', credentials: 'Operations certification | Issuer | Year' }
    };

    let selectedTemplate = 'graduate';
    let suggestedHeadline = '';
    let customAccent = null;
    let customSections = [];
    let customizedTitles = new Set();
    let sectionSettings = {};
    const originalSections = [...sectionList.children];

    function sectionKey(section) {
        return section.dataset.customSection || section.dataset.resumeSection;
    }

    function headingFor(section) {
        return section.querySelector('[data-section-heading]');
    }

    function applyStyle() {
        const details = templates[selectedTemplate];
        const accent = customAccent || details.color;
        const colorControl = document.querySelector('#resume-accent');
        preview.style.setProperty('--resume-accent', accent);
        preview.style.setProperty('--resume-font', document.querySelector('#resume-font').value);
        preview.dataset.density = document.querySelector('#resume-density').value;
        preview.dataset.headerAlign = document.querySelector('#resume-header-align').value;
        colorControl.value = accent;
    }

    function refreshSectionControls() {
        sectionControls.replaceChildren();
        [...sectionList.children].forEach((section, index, ordered) => {
            const id = sectionKey(section);
            const heading = headingFor(section);
            const settings = sectionSettings[id] || (sectionSettings[id] = { title: heading.textContent, visible: true });
            const row = document.createElement('div');
            row.className = 'section-control-row';
            row.dataset.controlSection = id;

            const titleLabel = document.createElement('label');
            titleLabel.className = 'section-title-label';
            titleLabel.textContent = 'Section title';
            const titleInput = document.createElement('input');
            titleInput.type = 'text';
            titleInput.maxLength = 60;
            titleInput.value = settings.title;
            titleInput.setAttribute('aria-label', `Title for ${settings.title}`);
            titleInput.addEventListener('input', () => {
                settings.title = titleInput.value.trim() || 'Untitled section';
                heading.textContent = settings.title;
                titleInput.setAttribute('aria-label', `Title for ${settings.title}`);
                customizedTitles.add(id);
                const custom = customSections.find(item => item.id === id);
                if (custom) custom.title = settings.title;
            });
            titleLabel.append(titleInput);

            const visibility = document.createElement('label');
            visibility.className = 'section-visibility';
            const checkbox = document.createElement('input');
            checkbox.type = 'checkbox';
            checkbox.checked = settings.visible !== false;
            checkbox.addEventListener('change', () => { settings.visible = checkbox.checked; render(); });
            visibility.append(checkbox, document.createTextNode(' Show'));

            const movement = document.createElement('div');
            movement.className = 'section-movement';
            [['Move section up', '↑', -1], ['Move section down', '↓', 1]].forEach(([label, glyph, delta]) => {
                const button = document.createElement('button');
                button.type = 'button';
                button.className = 'section-move-button';
                button.textContent = glyph;
                button.setAttribute('aria-label', label);
                button.disabled = (delta < 0 && index === 0) || (delta > 0 && index === ordered.length - 1);
                button.addEventListener('click', () => {
                    const target = ordered[index + delta];
                    if (!target) return;
                    if (delta < 0) sectionList.insertBefore(section, target);
                    else sectionList.insertBefore(target, section);
                    refreshSectionControls();
                });
                movement.append(button);
            });

            if (section.dataset.customSection) {
                const remove = document.createElement('button');
                remove.type = 'button';
                remove.className = 'section-remove-button';
                remove.textContent = 'Remove';
                remove.addEventListener('click', () => {
                    if (!confirm(`Remove the ${settings.title} section and its content?`)) return;
                    customSections = customSections.filter(item => item.id !== id);
                    section.remove();
                    document.querySelector(`[data-custom-editor="${CSS.escape(id)}"]`)?.remove();
                    delete sectionSettings[id];
                    refreshSectionControls();
                    render();
                });
                movement.append(remove);
            }

            row.append(titleLabel, visibility, movement);
            sectionControls.append(row);
        });
    }

    function setTemplate(template, suggestRole = false) {
        if (!Object.hasOwn(templates, template)) template = 'graduate';
        selectedTemplate = template;
        const details = templates[template];
        preview.className = preview.className.replace(/\btemplate-[\w-]+\b/g, '').trim();
        preview.classList.add(`template-${template}`);
        document.querySelector('#template-caption').textContent = details.label;
        document.querySelector('#experience-label').textContent = details.experience;
        const experienceHeading = document.querySelector('[data-section-heading="experience"]');
        if (experienceHeading && !customizedTitles.has('experience')) {
            experienceHeading.textContent = details.heading;
            if (sectionSettings.experience) sectionSettings.experience.title = details.heading;
        }
        form.elements.skills.placeholder = details.skills;
        form.elements.experience.placeholder = details.experienceHint;
        form.elements.projects.placeholder = details.projects;
        form.elements.certifications.placeholder = details.credentials;
        document.querySelector('#template-guidance').textContent = `The ${details.role} template is ready. Add your own information below; the live preview updates as you type.`;
        document.querySelectorAll('[data-template-choice]').forEach(button => {
            const active = button.dataset.templateChoice === template;
            button.classList.toggle('is-active', active);
            button.setAttribute('aria-pressed', String(active));
        });
        applyStyle();
        refreshSectionControls();
        const roleField = form.elements.headline;
        if (suggestRole && (!roleField.value.trim() || roleField.value.trim() === suggestedHeadline)) {
            roleField.value = details.role;
            suggestedHeadline = details.role;
            render();
        }
    }

    function addCustomSection(item) {
        const section = document.createElement('section');
        section.dataset.customSection = item.id;
        const heading = document.createElement('h2');
        heading.dataset.sectionHeading = item.id;
        heading.textContent = item.title;
        const content = document.createElement('p');
        content.className = 'preserve-lines';
        content.dataset.customPreview = item.id;
        section.append(heading, content);
        sectionList.append(section);
        sectionSettings[item.id] = { title: item.title, visible: item.visible !== false };

        const editor = document.createElement('div');
        editor.className = 'custom-section-editor-card';
        editor.dataset.customEditor = item.id;
        const label = document.createElement('label');
        const textarea = document.createElement('textarea');
        textarea.id = `custom-content-${item.id}`;
        textarea.rows = 3;
        textarea.maxLength = 4000;
        textarea.value = item.content || '';
        textarea.placeholder = 'Add relevant details, dates, and your contribution.';
        label.htmlFor = textarea.id;
        label.textContent = `${item.title} details`;
        textarea.setAttribute('aria-label', `${item.title} details`);
        textarea.addEventListener('input', render);
        editor.append(label, textarea);
        customEditorList.append(editor);
        refreshSectionControls();
    }

    function render() {
        const values = Object.fromEntries(fields.map(field => [field.name, field.value.trim()]));
        document.querySelectorAll('[data-preview]').forEach(element => {
            const name = element.dataset.preview;
            element.textContent = values[name] || (name === 'name' ? 'Your name' : '');
        });
        document.querySelector('#preview-contact').textContent = ['email', 'phone', 'location'].map(name => values[name]).filter(Boolean).join(' | ');
        let hasResumeContent = ['summary', 'education', 'experience', 'projects', 'skills', 'certifications'].some(name => values[name]);
        [...sectionList.children].forEach(section => {
            const id = sectionKey(section);
            const settings = sectionSettings[id] || { visible: true };
            const custom = section.dataset.customSection ? customSections.find(item => item.id === id) : null;
            const content = custom ? document.querySelector(`[data-custom-editor="${CSS.escape(id)}"] textarea`)?.value.trim() || '' : values[id] || '';
            const customPreview = section.querySelector('[data-custom-preview]');
            if (customPreview) customPreview.textContent = content;
            section.hidden = settings.visible === false || !content;
            if (content) hasResumeContent = true;
        });
        document.querySelector('#resume-empty').hidden = hasResumeContent;
    }

    function resetCustomization() {
        customAccent = null;
        customizedTitles = new Set();
        sectionSettings = {};
        originalSections.forEach(section => {
            const heading = headingFor(section);
            sectionSettings[section.dataset.resumeSection] = { title: heading.dataset.defaultTitle || heading.textContent, visible: true };
            sectionList.append(section);
        });
        customSections = [];
        customEditorList.replaceChildren();
        document.querySelector('#resume-accent').value = templates[selectedTemplate].color;
        document.querySelector('#resume-font').value = 'Arial, sans-serif';
        document.querySelector('#resume-density').value = 'comfortable';
        document.querySelector('#resume-header-align').value = 'left';
        applyStyle();
        refreshSectionControls();
    }

    form.addEventListener('input', render);
    document.querySelectorAll('[data-template-choice]').forEach(button => button.addEventListener('click', () => setTemplate(button.dataset.templateChoice, true)));
    const requestedTemplate = new URLSearchParams(window.location.search).get('template');
    if (requestedTemplate && Object.hasOwn(templates, requestedTemplate)) setTemplate(requestedTemplate, true);
    else setTemplate('graduate');

    const accent = document.querySelector('#resume-accent');
    accent.addEventListener('input', () => { customAccent = accent.value; applyStyle(); });
    document.querySelectorAll('[data-accent-preset]').forEach(button => button.addEventListener('click', () => {
        accent.value = button.dataset.accentPreset;
        customAccent = accent.value;
        applyStyle();
    }));
    ['#resume-font', '#resume-density', '#resume-header-align'].forEach(selector => document.querySelector(selector).addEventListener('change', applyStyle));

    document.querySelector('#show-add-section').addEventListener('click', () => {
        const editor = document.querySelector('#custom-section-form');
        editor.hidden = false;
        document.querySelector('#new-section-title').focus();
    });
    document.querySelector('#cancel-section').addEventListener('click', () => {
        document.querySelector('#custom-section-form').hidden = true;
        document.querySelector('#new-section-title').value = '';
        document.querySelector('#new-section-content').value = '';
    });
    document.querySelector('#create-section').addEventListener('click', () => {
        const titleInput = document.querySelector('#new-section-title');
        const title = titleInput.value.trim();
        if (!title) { titleInput.focus(); return; }
        const id = `custom-${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 6)}`;
        const item = { id, title: title.slice(0, 60), content: document.querySelector('#new-section-content').value.trim(), visible: true };
        customSections.push(item);
        addCustomSection(item);
        document.querySelector('#custom-section-form').hidden = true;
        titleInput.value = '';
        document.querySelector('#new-section-content').value = '';
        render();
    });

    form.addEventListener('submit', event => {
        event.preventDefault();
        if (!form.elements.name.value.trim()) { status.textContent = 'Enter your full name before printing.'; form.elements.name.focus(); return; }
        render();
        window.print();
    });

    document.querySelector('#save-resume').addEventListener('click', () => {
        try {
            const customization = {
                accent: customAccent,
                font: document.querySelector('#resume-font').value,
                density: document.querySelector('#resume-density').value,
                headerAlign: document.querySelector('#resume-header-align').value,
                sectionSettings,
                sectionOrder: [...sectionList.children].map(sectionKey),
                customizedTitles: [...customizedTitles]
            };
            const draft = { ...Object.fromEntries(fields.map(field => [field.name, field.value])), template: selectedTemplate, customization, customSections };
            localStorage.setItem(key, JSON.stringify(draft));
            status.textContent = 'Resume and design saved on this device.';
        } catch { status.textContent = 'This browser could not save your draft. You can still print or save a PDF.'; }
    });

    document.querySelector('#load-resume').addEventListener('click', () => {
        try {
            const saved = localStorage.getItem(key);
            if (!saved) { status.textContent = 'No saved draft on this device.'; return; }
            const values = JSON.parse(saved);
            if (!values || typeof values !== 'object' || Array.isArray(values)) throw new Error('Invalid draft');
            const layoutChanged = selectedTemplate !== 'graduate' || customAccent !== null || customizedTitles.size > 0 ||
                Object.values(sectionSettings).some(setting => setting.visible === false) ||
                document.querySelector('#resume-font').value !== 'Arial, sans-serif' ||
                document.querySelector('#resume-density').value !== 'comfortable' ||
                document.querySelector('#resume-header-align').value !== 'left';
            if ((fields.some(field => field.value) || customSections.length || layoutChanged) && !confirm('Replace the current resume and design with your saved draft?')) return;
            fields.forEach(field => { field.value = typeof values[field.name] === 'string' ? values[field.name].slice(0, field.maxLength) : ''; });
            sectionList.replaceChildren(...originalSections);
            originalSections.forEach(section => {
                const heading = headingFor(section);
                heading.textContent = heading.dataset.defaultTitle || heading.textContent;
            });
            sectionSettings = Object.fromEntries(originalSections.map(section => {
                const heading = headingFor(section);
                return [section.dataset.resumeSection, { title: heading.textContent, visible: true }];
            }));
            customSections = [];
            customEditorList.replaceChildren();
            suggestedHeadline = '';
            customizedTitles = new Set();
            customAccent = null;
            document.querySelector('#resume-font').value = 'Arial, sans-serif';
            document.querySelector('#resume-density').value = 'comfortable';
            document.querySelector('#resume-header-align').value = 'left';
            setTemplate(values.template);
            if (Array.isArray(values.customSections)) values.customSections.forEach(item => {
                if (item && typeof item.id === 'string' && typeof item.title === 'string') {
                    const safeItem = { id: item.id.replace(/[^\w-]/g, '').slice(0, 60), title: item.title.slice(0, 60), content: typeof item.content === 'string' ? item.content.slice(0, 4000) : '', visible: item.visible !== false };
                    if (safeItem.id && !customSections.some(existing => existing.id === safeItem.id)) { customSections.push(safeItem); addCustomSection(safeItem); }
                }
            });
            const customization = values.customization && typeof values.customization === 'object' ? values.customization : {};
            customizedTitles = new Set(Array.isArray(customization.customizedTitles) ? customization.customizedTitles.filter(id => typeof id === 'string') : []);
            if (customization.sectionSettings && typeof customization.sectionSettings === 'object') {
                Object.entries(customization.sectionSettings).forEach(([id, setting]) => {
                    if (setting && typeof setting === 'object' && sectionSettings[id]) {
                        if (typeof setting.title === 'string') sectionSettings[id].title = setting.title.slice(0, 60);
                        sectionSettings[id].visible = setting.visible !== false;
                    }
                });
            }
            [...sectionList.children].forEach(section => {
                const setting = sectionSettings[sectionKey(section)];
                if (setting) headingFor(section).textContent = setting.title;
            });
            if (Array.isArray(customization.sectionOrder)) {
                const sections = new Map([...sectionList.children].map(section => [sectionKey(section), section]));
                const used = new Set();
                customization.sectionOrder.forEach(id => { if (sections.has(id)) { sectionList.append(sections.get(id)); used.add(id); } });
                sections.forEach((section, id) => { if (!used.has(id)) sectionList.append(section); });
            }
            if (typeof customization.accent === 'string' && /^#[0-9a-f]{6}$/i.test(customization.accent)) customAccent = customization.accent;
            const fontSelect = document.querySelector('#resume-font');
            if ([...fontSelect.options].some(option => option.value === customization.font)) fontSelect.value = customization.font;
            if (['comfortable', 'compact'].includes(customization.density)) document.querySelector('#resume-density').value = customization.density;
            if (['left', 'center'].includes(customization.headerAlign)) document.querySelector('#resume-header-align').value = customization.headerAlign;
            applyStyle();
            refreshSectionControls();
            render();
            status.textContent = 'Saved resume and custom design loaded.';
        } catch { status.textContent = 'Could not load the saved resume draft.'; }
    });

    document.querySelector('#clear-resume').addEventListener('click', () => {
        if (!confirm('Clear the resume, custom sections, and saved draft from this device?')) return;
        fields.forEach(field => { field.value = ''; });
        sectionList.replaceChildren(...originalSections);
        customSections = [];
        customEditorList.replaceChildren();
        document.querySelector('#custom-section-form').hidden = true;
        document.querySelector('#new-section-title').value = '';
        document.querySelector('#new-section-content').value = '';
        document.querySelectorAll('[data-section-heading]').forEach(heading => { if (heading.dataset.defaultTitle) heading.textContent = heading.dataset.defaultTitle; });
        suggestedHeadline = '';
        selectedTemplate = 'graduate';
        resetCustomization();
        customizedTitles = new Set();
        setTemplate('graduate');
        render();
        try { localStorage.removeItem(key); status.textContent = 'Resume, custom sections, and saved draft cleared.'; }
        catch { status.textContent = 'Resume cleared. Browser storage could not be cleared.'; }
    });

    resetCustomization();
    setTemplate(requestedTemplate && Object.hasOwn(templates, requestedTemplate) ? requestedTemplate : 'graduate', !!requestedTemplate);
    render();
})();
