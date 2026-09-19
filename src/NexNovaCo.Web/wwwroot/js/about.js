import { createReveal } from './reveal.js';
import { initializeCarousels } from './carousels.js';

// About content stays in Razor; only the partner carousel and scroll reveals use JS.
const sessions = new WeakMap();
export function initialize(root) {
    if (!root?.isConnected || sessions.has(root)) return;
    const motion = window.matchMedia('(prefers-reduced-motion: reduce)');
    const disposeCarousels = initializeCarousels(root, motion);
    const disposeReveal = createReveal(root, motion);
    root.dataset.aboutEnhanced = 'true';

    // Client-side cleanup still runs if the Interactive Server circuit is unavailable.
    const removalObserver = new MutationObserver(() => { if (!root.isConnected) dispose(); });
    const dispose = () => {
        removalObserver.disconnect();
        disposeReveal();
        disposeCarousels();
        window.removeEventListener('pagehide', dispose);
        sessions.delete(root);
    };
    removalObserver.observe(root.parentNode, { childList: true });
    window.addEventListener('pagehide', dispose, { once: true });
    sessions.set(root, dispose);
}
