// Preserve the approved AOS CSS/transforms/delays without global AOS listeners.
// Caller owns lifecycle; this helper never fetches, creates, or reorders content.
export function createReveal(root, motion) {
    const animated = [...root.querySelectorAll('[data-aos]')];
    const reveal = (element, visible) => {
        element.classList.toggle('aos-animate', visible || motion.matches);
        element.style.transitionDelay = visible && !motion.matches ? `${Number(element.dataset.aosDelay) || 0}ms` : '0ms';
    };
    const animationObserver = new IntersectionObserver(entries => {
        for (const entry of entries) {
            // Like AOS mirror:false, elements remain revealed after scrolling above the viewport.
            reveal(entry.target, entry.boundingClientRect.top < window.innerHeight - 120);
        }
    }, { rootMargin: '0px 0px -120px 0px' });
    for (const element of animated) {
        element.classList.add('aos-init');
        reveal(element, element.getBoundingClientRect().top < window.innerHeight - 120);
        animationObserver.observe(element);
    }

    const applyMotion = () => {
        root.dataset.reducedMotion = String(motion.matches);
        if (motion.matches) animated.forEach(element => reveal(element, true));
    };
    motion.addEventListener('change', applyMotion);
    applyMotion();
    return () => {
        animationObserver.disconnect();
        motion.removeEventListener('change', applyMotion);
    };
}
