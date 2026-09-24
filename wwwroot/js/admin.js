(() => {
 document.querySelectorAll('[data-copy-share-text]').forEach(button => {
  let resetTimer;
  button.addEventListener('click', async () => {
   const text = button.dataset.copyShareText;
   const status = button.parentElement.querySelector('[data-copy-status]');
   clearTimeout(resetTimer);
   try {
    try { await navigator.clipboard.writeText(text); }
    catch {
     const field = document.createElement('textarea');
     field.value = text;
     field.style.cssText = 'position:fixed;left:-9999px;top:0';
     document.body.append(field);
     try {
      field.select();
      if (!document.execCommand('copy')) throw new Error('Copy failed');
     } finally { field.remove(); button.focus(); }
    }
    button.textContent = 'Copied!';
    status.textContent = 'Share text copied.';
   } catch {
    button.textContent = 'Try copying again';
    status.textContent = 'Could not copy share text. Please try again.';
   }
   resetTimer = setTimeout(() => { button.textContent = 'Copy share text'; status.textContent = ''; }, 3000);
  });
 });
 document.querySelectorAll('[data-confirm-delete]').forEach(form => form.addEventListener('submit', event => { if (!confirm('Delete this job permanently? This cannot be undone.')) event.preventDefault(); }));
 const input=document.querySelector('[data-logo-input]'), preview=document.querySelector('#logoPreview');
 let objectUrl; const original=preview?.getAttribute('src');
 input?.addEventListener('change',()=>{if(objectUrl)URL.revokeObjectURL(objectUrl);const file=input.files?.[0];if(!file){preview.hidden=!original;if(original)preview.src=original;return;}objectUrl=URL.createObjectURL(file);preview.src=objectUrl;preview.hidden=false;});
})();
