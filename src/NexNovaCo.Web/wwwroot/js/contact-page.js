import { createReveal } from './reveal.js';

// Visual enhancement only: EditForm owns all validation, submission and reset state.
const sessions = new WeakMap();
export function initialize(root) {
    if (!root?.isConnected || sessions.has(root)) return;
    const disposeReveal = createReveal(root, window.matchMedia('(prefers-reduced-motion: reduce)'));
    root.dataset.contactEnhanced = 'true';
    const removalObserver = new MutationObserver(() => { if (!root.isConnected) dispose(); });
    const dispose = () => {
        removalObserver.disconnect();
        disposeReveal();
        window.removeEventListener('pagehide', dispose);
        sessions.delete(root);
    };
    removalObserver.observe(root.parentNode, { childList: true });
    window.addEventListener('pagehide', dispose, { once: true });
    sessions.set(root, dispose);
}
