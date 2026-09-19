import { createReveal } from './reveal.js';

// Transitional Home enhancement. All entity content is rendered by Razor, never fetched/generated here.
// ThemeCarousel freezes its rendered subtree while Owl temporarily wraps/clones it.
const sessions = new WeakMap();
let owlPrepared = false;

function prepareOwl($) {
    if (owlPrepared) return;
    // This unused Owl 2.3.4 plugin registers anonymous window load/resize handlers that its destroy
    // method does not remove. None of Home's carousels uses autoHeight. Avoid registering the leak.
    delete $.fn.owlCarousel.Constructor.Plugins.AutoHeight;
    owlPrepared = true;
}

function createCarousel(element, motion) {
    const $ = window.jQuery;
    const carousel = $(element);
    const kind = element.dataset.carouselKind;
    const timeout = kind === 'testimonials' ? 6000 : 3000;
    const options = {
        nav: true, loop: true, autoplay: false, autoplayTimeout: timeout,
        autoplayHoverPause: false, smartSpeed: motion.matches ? 0 : 250,
        navText: ['<span aria-hidden="true">‹</span>', '<span aria-hidden="true">›</span>']
    };
    if (kind === 'projects') Object.assign(options, { margin: 20, dots: false,
        responsive: { 0: { items: 1 }, 600: { items: 2 }, 1000: { items: 3 } } });
    if (kind === 'partners') Object.assign(options, { margin: 20,
        responsive: { 0: { items: 1 }, 400: { items: 2 }, 700: { items: 3 }, 900: { items: 4 }, 1000: { items: 4 }, 1200: { items: 6 } } });
    if (kind === 'testimonials') options.items = 1;

    let userPaused = false;
    let hovering = false;
    let focused = false;
    const events = new AbortController();
    const labelControls = () => {
        element.querySelector('.owl-prev')?.setAttribute('aria-label', `Previous ${kind}`);
        element.querySelector('.owl-next')?.setAttribute('aria-label', `Next ${kind}`);
        element.querySelectorAll('.owl-dot').forEach((dot, index) => {
            dot.setAttribute('aria-label', `Go to ${kind} page ${index + 1}`);
            dot.setAttribute('aria-current', dot.classList.contains('active') ? 'true' : 'false');
            dot.setAttribute('type', 'button');
        });
        const instance = carousel.data('owl.carousel');
        element.querySelectorAll('.owl-item').forEach((slide, index) => {
            const active = slide.classList.contains('active');
            slide.setAttribute('aria-hidden', String(!active));
            slide.inert = !active;
            slide.setAttribute('role', 'group');
            slide.setAttribute('aria-roledescription', 'slide');
            if (instance) slide.setAttribute('aria-label', `${instance.relative(index) + 1} of ${instance.items().length}`);
        });
    };
    carousel.on('initialized.owl.carousel.home refreshed.owl.carousel.home translated.owl.carousel.home', labelControls);
    carousel.owlCarousel(options);

    const pause = document.createElement('button');
    pause.type = 'button';
    pause.className = 'carousel-pause';
    element.querySelector('.owl-nav').append(pause);
    const updatePlayback = () => {
        const instance = carousel.data('owl.carousel');
        if (!instance) return;
        // Owl hides controls when every item fits. Do not animate an unpausable full row.
        const allVisible = instance.items().length <= instance.settings.items;
        const blocked = allVisible || motion.matches || userPaused || hovering || focused || document.hidden;
        instance.settings.autoplay = !blocked;
        instance.settings.smartSpeed = motion.matches ? 0 : 250;
        carousel.trigger(blocked ? 'stop.owl.autoplay' : 'play.owl.autoplay', [timeout]);
        pause.textContent = userPaused || motion.matches ? '▶' : 'Ⅱ';
        pause.disabled = motion.matches;
        pause.setAttribute('aria-label', motion.matches ? `Autoplay disabled for ${kind} (reduced motion)` : `${userPaused ? 'Play' : 'Pause'} ${kind}`);
        pause.setAttribute('aria-pressed', String(userPaused || motion.matches));
        element.dataset.autoplay = blocked ? 'paused' : 'playing';
        element.querySelector('.owl-stage-outer')?.setAttribute('aria-live', blocked ? 'polite' : 'off');
    };
    carousel.on('refreshed.owl.carousel.home', updatePlayback);
    const listen = (target, event, handler) => target.addEventListener(event, handler, { signal: events.signal });
    listen(pause, 'click', () => { userPaused = !userPaused; updatePlayback(); });
    listen(element, 'mouseenter', () => { hovering = true; updatePlayback(); });
    listen(element, 'mouseleave', () => { hovering = false; updatePlayback(); });
    listen(element, 'focusin', () => { focused = true; updatePlayback(); });
    listen(element, 'focusout', event => { focused = element.contains(event.relatedTarget); updatePlayback(); });
    listen(document, 'visibilitychange', updatePlayback);
    listen(element, 'keydown', event => {
        if (!['ArrowLeft', 'ArrowRight'].includes(event.key) || /INPUT|TEXTAREA|SELECT/.test(event.target.tagName)) return;
        event.preventDefault();
        carousel.trigger(event.key === 'ArrowRight' ? 'next.owl.carousel' : 'prev.owl.carousel', [motion.matches ? 0 : 250]);
    });
    motion.addEventListener('change', updatePlayback);
    labelControls();
    updatePlayback();
    element.dataset.carouselInitialized = 'true';

    return () => {
        events.abort();
        motion.removeEventListener('change', updatePlayback);
        carousel.trigger('stop.owl.autoplay');
        carousel.off('.home');
        if (carousel.data('owl.carousel')) carousel.trigger('destroy.owl.carousel');
        pause.remove();
        delete element.dataset.carouselInitialized;
    };
}

export function initialize(root) {
    if (!root?.isConnected || sessions.has(root)) return;
    const motion = window.matchMedia('(prefers-reduced-motion: reduce)');
    const cleanup = [];
    const counters = [];
    const $ = window.jQuery;
    if ($?.fn.owlCarousel) {
        prepareOwl($);
        root.querySelectorAll('[data-carousel-kind]').forEach(element => cleanup.push(createCarousel(element, motion)));
    }

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
        cleanup.forEach(remove => remove());
        sessions.delete(root);
    };
    removalObserver.observe(root.parentNode, { childList: true });
    window.addEventListener('pagehide', dispose, { once: true });
    sessions.set(root, dispose);
}
