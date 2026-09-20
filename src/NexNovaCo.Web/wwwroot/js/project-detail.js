import { createReveal } from './reveal.js';

// Visual enhancement only. Route selection, text, media and gallery controls are Blazor-owned.
const sessions = new WeakMap();
export function initialize(root) {
    if (!root?.isConnected || sessions.has(root)) return;
    const motion = window.matchMedia('(prefers-reduced-motion: reduce)');
    const disposeReveal = createReveal(root, motion);
    root.dataset.projectDetailEnhanced = 'true';
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
