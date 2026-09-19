// Preserve the approved AOS CSS/transforms/delays without global AOS listeners.
// Caller owns lifecycle; this helper never fetches, creates, or reorders content.
export function createReveal(root, motion) {
    let animated = [];
    const reveal = (element, visible) => {
        element.classList.toggle('aos-animate', visible || motion.matches);
        element.style.transitionDelay = visible && !motion.matches ? `${Number(element.dataset.aosDelay) || 0}ms` : '0ms';
    };
    // AOS measures layout offsets, not transformed intersection rectangles. Observing the
    // fading element itself can leave a visible FAQ row hidden behind its own 100px transform.
    const layoutTop = element => {
        let top = 0;
        for (let node = element; node; node = node.offsetParent) top += node.offsetTop;
        return top - window.scrollY;
    };
    let frame = 0;
    const refresh = () => {
        root.dataset.reducedMotion = String(motion.matches);
        // Like AOS mirror:false, content above the viewport remains revealed.
        for (const element of animated) reveal(element, layoutTop(element) < window.innerHeight - 120);
    };
    const schedule = () => {
        if (!frame) frame = window.requestAnimationFrame(() => { frame = 0; refresh(); });
    };
    const refreshTargets = () => {
        // Owl replaces loop clones at responsive breakpoints, even when root height is unchanged.
        animated = [...root.querySelectorAll('[data-aos]')];
        animated.forEach(element => element.classList.add('aos-init'));
        refresh();
    };
    root.addEventListener('theme:carousel-refreshed', refreshTargets);
    window.addEventListener('scroll', schedule, { passive: true });
    window.addEventListener('resize', schedule);
    motion.addEventListener('change', refresh);
    // Handles font loading and FAQ expansion without an unbounded mutation/refresh loop.
    const resizeObserver = new ResizeObserver(schedule);
    resizeObserver.observe(root);
    refreshTargets();
    return () => {
        window.cancelAnimationFrame(frame);
        resizeObserver.disconnect();
        root.removeEventListener('theme:carousel-refreshed', refreshTargets);
        window.removeEventListener('scroll', schedule);
        window.removeEventListener('resize', schedule);
        motion.removeEventListener('change', refresh);
    };
}
