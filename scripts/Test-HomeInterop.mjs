// Dependency-free logic/lifecycle tests. Real Owl behavior is verified separately in the browser.
import test from 'node:test';
import assert from 'node:assert/strict';
import { initialize } from '../src/NexNovaCo.Web/wwwroot/js/home.js';

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

function fixture(reduced = false) {
    const media = new EventTarget();
    media.matches = reduced;
    const intersections = [], mutations = [], calls = [], countInstances = [];
    const animated = new Element();
    animated.dataset.aosDelay = '100';
    const display = new Element(), counter = new Element();
    counter.dataset.countValue = '3000';
    counter.querySelector = () => display;
    const carousel = new Element(), nav = new Element(), stage = new Element();
    carousel.dataset.carouselKind = 'projects';
    carousel.querySelector = selector => selector === '.owl-nav' ? nav : selector === '.owl-stage-outer' ? stage : null;
    const root = new Element();
    root.isConnected = true;
    root.parentNode = {};
    root.querySelectorAll = selector => ({
        '[data-carousel-kind]': [carousel], '[data-aos]': [animated], '[data-count-value]': [counter]
    }[selector] ?? []);
    let instance;
    const wrapper = {
        on() { return this; }, off() { calls.push('off'); return this; },
        owlCarousel(options) { calls.push('init'); instance = { settings: { ...options, items: 3 }, items: () => Array(5) }; return this; },
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
            constructor() { this.starts = 0; this.resets = 0; countInstances.push(this); }
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
    return { root, carousel, nav, stage, animated, media, counter, display, calls, intersections, mutations, countInstances,
        instance: () => instance,
        remove() { root.isConnected = false; mutations[0].callback(); } };
}

test('initialization is idempotent; detach destroys the plugin and disconnects observers', () => {
    const f = fixture();
    initialize(f.root);
    initialize(f.root);
    assert.equal(f.calls.filter(x => x === 'init').length, 1);
    assert.equal(f.intersections.length, 1);
    assert.equal(f.mutations.length, 1);
    assert.equal(f.root.dataset.homeEnhanced, 'true');
    assert.equal(f.carousel.dataset.autoplay, 'playing');
    f.intersections[0].callback([{ isIntersecting: true, target: f.counter }]);
    assert.equal(f.countInstances[0].starts, 1);
    f.remove();
    assert.equal(f.calls.filter(x => x === 'destroy.owl.carousel').length, 1);
    assert.ok([...f.intersections, ...f.mutations].every(x => x.disconnected));
    assert.equal(f.countInstances[0].resets, 1);
    assert.equal(f.nav.child.removed, true);
    const callCount = f.calls.length;
    f.media.dispatchEvent(new Event('change'));
    f.nav.child.dispatchEvent(new Event('click'));
    assert.equal(f.calls.length, callCount, 'Detached media/control listeners must not run');
    initialize(f.root);
    assert.equal(f.calls.length, callCount, 'Detached roots must not initialize');
});

test('reduced motion disables autoplay/counting and shows final readable content', () => {
    const f = fixture(true);
    initialize(f.root);
    assert.equal(f.instance().settings.smartSpeed, 0);
    assert.equal(f.carousel.dataset.autoplay, 'paused');
    assert.equal(f.nav.child.disabled, true);
    assert.equal(f.root.dataset.reducedMotion, 'true');
    assert.equal(f.display.textContent, '3,000');
    assert.ok(f.animated.classes.has('aos-animate'));
    f.intersections[0].callback([{ isIntersecting: true, target: f.counter }]);
    assert.equal(f.countInstances.length, 0);
    f.media.matches = false;
    f.media.dispatchEvent(new Event('change'));
    assert.equal(f.carousel.dataset.autoplay, 'playing');
    assert.equal(f.instance().settings.smartSpeed, 250);
    f.instance().settings.items = 5;
    f.media.dispatchEvent(new Event('change'));
    assert.equal(f.carousel.dataset.autoplay, 'paused', 'A full row with hidden Owl controls must not autoplay');
    f.instance().settings.items = 3;
    f.media.dispatchEvent(new Event('change'));
    f.nav.child.dispatchEvent(new Event('click'));
    assert.equal(f.carousel.dataset.autoplay, 'paused');
    assert.equal(f.nav.child.attributes.get('aria-pressed'), 'true');
    f.remove();
});
