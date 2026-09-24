// Only shared-shell behavior; each page module owns its scoped enhancements.
let cleanup;

export function initialize() {
    dispose();
    const header = document.querySelector('.main_menu .header_top');
    const backToTop = document.querySelector('.footer .gotop');
    const skipLink = document.querySelector('.skip-link');
    const skipToMain = event => {
        event.preventDefault();
        const main = document.getElementById('main-content');
        main?.focus({ preventScroll: true });
        main?.scrollIntoView({ block: 'start', behavior: 'instant' });
    };
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
    skipLink?.addEventListener('click', skipToMain);
    update();
    cleanup = () => {
        window.removeEventListener('scroll', update);
        backToTop?.removeEventListener('click', scrollToTop);
        skipLink?.removeEventListener('click', skipToMain);
    };
}

export function dispose() {
    cleanup?.();
    cleanup = undefined;
}
