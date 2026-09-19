import { createReveal } from './reveal.js';

// Only scroll reveals use JS. FAQ expansion, content and navigation stay in Razor/Blazor.
const sessions = new WeakMap();
export function initialize(root) {
    if (!root?.isConnected || sessions.has(root)) return;
    const motion = window.matchMedia('(prefers-reduced-motion: reduce)');
    const disposeReveal = createReveal(root, motion);
    root.dataset.servicesEnhanced = 'true';

    // Client-side cleanup still runs if the Interactive Server circuit is unavailable.
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
