import { createReveal } from './reveal.js';
import { initializeCarousels } from './carousels.js';

// Transitional Home enhancement. All entity content is rendered by Razor, never fetched/generated here.
// ThemeCarousel freezes its rendered subtree while Owl temporarily wraps/clones it.
const sessions = new WeakMap();
export function initialize(root) {
    if (!root?.isConnected || sessions.has(root)) return;
    const motion = window.matchMedia('(prefers-reduced-motion: reduce)');
    const disposeCarousels = initializeCarousels(root, motion);
    const counters = [];
    const disposeReveal = createReveal(root, motion);

    const counterObserver = new IntersectionObserver(entries => {
        for (const entry of entries) {
            if (!entry.isIntersecting) continue;
            const heading = entry.target;
            counterObserver.unobserve(heading);
            if (motion.matches || !window.countUp?.CountUp) continue;
            const counter = new window.countUp.CountUp(heading.querySelector('[data-count-display]'), Number(heading.dataset.countValue), { duration: 2 });
            if (!counter.error) { counters.push(counter); counter.start(); }
        }
    });
    root.querySelectorAll('[data-count-value]').forEach(element => counterObserver.observe(element));

    const applyMotion = () => {
        root.dataset.reducedMotion = String(motion.matches);
        if (motion.matches) {
            for (const counter of counters) counter.reset();
            root.querySelectorAll('[data-count-value]').forEach(element => {
                element.querySelector('[data-count-display]').textContent = Number(element.dataset.countValue).toLocaleString('en-US');
            });
        }
    };
    motion.addEventListener('change', applyMotion);
    applyMotion();
    root.dataset.homeEnhanced = 'true';

    // Runs client-side even when the server circuit is gone. No late interop into detached DOM.
    const removalObserver = new MutationObserver(() => { if (!root.isConnected) dispose(); });
    const dispose = () => {
        removalObserver.disconnect();
        disposeReveal();
        counterObserver.disconnect();
        motion.removeEventListener('change', applyMotion);
        window.removeEventListener('pagehide', dispose);
        for (const counter of counters) counter.reset();
        disposeCarousels();
        sessions.delete(root);
    };
    removalObserver.observe(root.parentNode, { childList: true });
    window.addEventListener('pagehide', dispose, { once: true });
    sessions.set(root, dispose);
}
