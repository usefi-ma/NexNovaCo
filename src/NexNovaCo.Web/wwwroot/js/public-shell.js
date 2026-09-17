// Only shared-shell behavior. Legacy page plugins remain dormant until Phase 2.
let cleanup;

export function initialize() {
    dispose();
    const header = document.querySelector('.main_menu .header_top');
    const backToTop = document.querySelector('.footer .gotop');
    const update = () => {
        header?.classList.toggle('fixed', window.scrollY > 100);
        backToTop?.classList.toggle('is-visible', window.scrollY > 100);
    };
    const scrollToTop = (event) => {
        event.preventDefault();
        document.getElementById('main-content')?.focus({ preventScroll: true });
        window.scrollTo({ top: 0, behavior: window.matchMedia('(prefers-reduced-motion: reduce)').matches ? 'instant' : 'smooth' });
    };
    window.addEventListener('scroll', update, { passive: true });
    backToTop?.addEventListener('click', scrollToTop);
    update();
    cleanup = () => {
        window.removeEventListener('scroll', update);
        backToTop?.removeEventListener('click', scrollToTop);
    };
}

export function dispose() {
    cleanup?.();
    cleanup = undefined;
}
