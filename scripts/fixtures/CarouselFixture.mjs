class Element extends EventTarget {
    offsetTop = 100;
    dataset = {};
    style = {};
    attributes = new Map();
    classes = new Set();
    textContent = '';
    classList = {
        add: value => this.classes.add(value),
        toggle: (value, enabled) => enabled ? this.classes.add(value) : this.classes.delete(value)
    };
    setAttribute(name, value) { this.attributes.set(name, value); }
    getBoundingClientRect() { return { top: 100 }; }
    querySelectorAll() { return []; }
    append(child) { this.child = child; }
    remove() { this.removed = true; }
    contains(node) { return node === this; }
}

export function fixture(reduced = false, kind = 'projects') {
    const media = new EventTarget();
    media.matches = reduced;
    const intersections = [], mutations = [], calls = [], countInstances = [];
    const animated = new Element();
    animated.dataset.aosDelay = '100';
    let animatedNodes = [animated];
    const display = new Element(), counter = new Element();
    counter.dataset.countValue = '3000';
    counter.querySelector = () => display;
    const carousel = new Element(), nav = new Element(), stage = new Element();
    carousel.dataset.carouselKind = kind;
    carousel.querySelector = selector => selector === '.owl-nav' ? nav : selector === '.owl-stage-outer' ? stage : null;
    const root = new Element();
    root.isConnected = true;
    root.parentNode = {};
    root.querySelectorAll = selector => ({
        '[data-carousel-kind]': [carousel], '[data-aos]': animatedNodes, '[data-count-value]': [counter]
    }[selector] ?? []);
    let instance;
    const handlers = new Map();
    const wrapper = {
        on(names, callback) {
            for (const name of names.split(' ')) handlers.set(name, [...(handlers.get(name) ?? []), callback]);
            return this;
        },
        off() { calls.push('off'); handlers.clear(); return this; },
        owlCarousel(options) { calls.push('init'); instance = { settings: { ...options, items: kind === 'partners' ? 4 : kind === 'testimonials' ? 1 : 3 }, items: () => Array(kind === 'partners' ? 6 : kind === 'testimonials' ? 2 : 5) }; return this; },
        data() { return instance; },
        trigger(name) { calls.push(name); if (name === 'destroy.owl.carousel') instance = null; return this; }
    };
    const jquery = () => wrapper;
    jquery.fn = { owlCarousel: { Constructor: { Plugins: { AutoHeight: {} } } } };
    const windowMock = new EventTarget();
    Object.assign(windowMock, {
        jQuery: jquery, innerHeight: 900, scrollY: 0, matchMedia: () => media,
        requestAnimationFrame: () => 1, cancelAnimationFrame: () => {},
        countUp: { CountUp: class {
            constructor(target, endValue, options) { this.target = target; this.endValue = endValue; this.options = options; this.starts = 0; this.resets = 0; countInstances.push(this); }
            start() { this.starts++; }
            reset() { this.resets++; }
        } }
    });
    const documentMock = new EventTarget();
    documentMock.hidden = false;
    documentMock.createElement = () => new Element();
    globalThis.window = windowMock;
    globalThis.document = documentMock;
    globalThis.IntersectionObserver = class {
        constructor(callback) { this.callback = callback; intersections.push(this); }
        observe() {} unobserve() {}
        disconnect() { this.disconnected = true; }
    };
    globalThis.MutationObserver = class {
        constructor(callback) { this.callback = callback; mutations.push(this); }
        observe() {}
        disconnect() { this.disconnected = true; }
    };
    globalThis.ResizeObserver = class {
        observe() {} disconnect() {}
    };
    return { browser: windowMock, root, carousel, nav, stage, animated, media, counter, display, calls, intersections, mutations, countInstances,
        replaceAnimated() {
            const replacement = new Element(); replacement.dataset.aosDelay = '100';
            animatedNodes = [replacement]; return replacement;
        },
        refreshCarousel() { for (const callback of handlers.get('refreshed.owl.carousel.home') ?? []) callback(); },
        instance: () => instance,
        remove() { root.isConnected = false; mutations[0].callback(); } };
}
