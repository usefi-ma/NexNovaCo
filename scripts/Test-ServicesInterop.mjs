// Dependency-free tests of the actual shared reveal and Services lifecycle modules.
import test from 'node:test';
import assert from 'node:assert/strict';
import { initialize } from '../src/NexNovaCo.Web/wwwroot/js/services.js';

function fixture(reduced = false) {
    const media = new EventTarget(); media.matches = reduced;
    const resizes = [], mutations = [], classes = new Set(), frames = new Map();
    const animated = {
        dataset: { aosDelay: '200' }, style: {}, offsetTop: 1000,
        classList: { add: value => classes.add(value), toggle: (value, enabled) => enabled ? classes.add(value) : classes.delete(value) },
        getBoundingClientRect: () => ({ top: 1000 })
    };
    const root = { isConnected: true, parentNode: {}, dataset: {}, querySelectorAll: selector => selector === '[data-aos]' ? [animated] : [] };
    const browser = new EventTarget();
    Object.assign(browser, { innerHeight: 900, scrollY: 0, matchMedia: () => media,
        requestAnimationFrame: callback => { frames.set(1, callback); return 1; },
        cancelAnimationFrame: id => frames.delete(id) });
    globalThis.window = browser;
    globalThis.ResizeObserver = class {
        constructor(callback) { this.callback = callback; resizes.push(this); }
        observe() {} disconnect() { this.disconnected = true; }
    };
    globalThis.MutationObserver = class {
        constructor(callback) { this.callback = callback; mutations.push(this); }
        observe() {} disconnect() { this.disconnected = true; }
    };
    return { root, animated, media, browser, resizes, mutations, classes, frames,
        flush() { const callback = frames.get(1); frames.clear(); callback?.(); } };
}

test('Services initializes once, reveals on scroll, and cleans up after removal', () => {
    const f = fixture();
    initialize(null); initialize({ isConnected: false });
    initialize(f.root); initialize(f.root);
    assert.equal(f.resizes.length, 1); assert.equal(f.mutations.length, 1);
    assert.equal(f.root.dataset.servicesEnhanced, 'true');
    assert.equal(f.classes.has('aos-animate'), false);
    f.browser.scrollY = 300;
    f.browser.dispatchEvent(new Event('scroll'));
    f.browser.dispatchEvent(new Event('scroll'));
    assert.equal(f.frames.size, 1, 'Scroll work is limited to one animation frame');
    f.flush();
    assert.equal(f.classes.has('aos-animate'), true);
    assert.equal(f.animated.style.transitionDelay, '200ms');
    f.root.isConnected = false; f.mutations[0].callback();
    assert.ok(f.resizes[0].disconnected && f.mutations[0].disconnected);
    f.media.matches = true; f.media.dispatchEvent(new Event('change'));
    assert.equal(f.root.dataset.reducedMotion, 'false', 'Detached listeners must not run');
    f.browser.dispatchEvent(new Event('scroll'));
    assert.equal(f.frames.size, 0, 'Detached scroll listener must not run');
});

test('reduced motion reveals all content; pagehide disposes and a new route initializes independently', () => {
    const f = fixture(true); initialize(f.root);
    assert.equal(f.classes.has('aos-animate'), true);
    assert.equal(f.animated.style.transitionDelay, '0ms');
    f.browser.dispatchEvent(new Event('pagehide'));
    assert.ok(f.resizes[0].disconnected && f.mutations[0].disconnected);
    const next = fixture(); initialize(next.root);
    assert.equal(next.resizes.length, 1);
    next.media.matches = true; next.media.dispatchEvent(new Event('change'));
    assert.equal(next.classes.has('aos-animate'), true);
    assert.equal(next.root.dataset.reducedMotion, 'true');
    next.root.isConnected = false; next.mutations[0].callback();
});

test('layout offsets reveal translated FAQ rows and refresh when content height changes', () => {
    const f = fixture();
    f.animated.offsetTop = 650;
    f.animated.offsetParent = { offsetTop: 100, offsetParent: null };
    // The visual rectangle is 1000, but the layout top is 750: do not hide a visible row.
    initialize(f.root);
    assert.equal(f.classes.has('aos-animate'), true);
    f.animated.offsetTop = 900;
    f.resizes[0].callback(); f.flush();
    assert.equal(f.classes.has('aos-animate'), false);
    f.animated.offsetTop = 500;
    f.resizes[0].callback(); f.flush();
    assert.equal(f.classes.has('aos-animate'), true);
    f.browser.dispatchEvent(new Event('resize'));
    f.root.isConnected = false; f.mutations[0].callback();
    assert.equal(f.frames.size, 0, 'Dispose cancels pending animation frames');
});
