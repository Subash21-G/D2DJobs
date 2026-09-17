(() => {
 document.querySelectorAll('[data-confirm-delete]').forEach(form => form.addEventListener('submit', event => { if (!confirm('Delete this job permanently? This cannot be undone.')) event.preventDefault(); }));
 const input=document.querySelector('[data-logo-input]'), preview=document.querySelector('#logoPreview');
 let objectUrl; const original=preview?.getAttribute('src');
 input?.addEventListener('change',()=>{if(objectUrl)URL.revokeObjectURL(objectUrl);const file=input.files?.[0];if(!file){preview.hidden=!original;if(original)preview.src=original;return;}objectUrl=URL.createObjectURL(file);preview.src=objectUrl;preview.hidden=false;});
})();