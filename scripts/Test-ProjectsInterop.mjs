import test from 'node:test';
import assert from 'node:assert/strict';
import { initialize } from '../src/NexNovaCo.Web/wwwroot/js/projects.js';
import { initialize as initializeHome } from '../src/NexNovaCo.Web/wwwroot/js/home.js';
import { initialize as initializeAbout } from '../src/NexNovaCo.Web/wwwroot/js/about.js';
import { fixture } from './fixtures/CarouselFixture.mjs';

test('Projects initializes testimonials once without controls, looping or autoplay', () => {
    const f = fixture(false, 'testimonials');
    initialize(null); initialize({ isConnected: false });
    initialize(f.root); initialize(f.root);
    assert.equal(f.calls.filter(x => x === 'init').length, 1);
    assert.equal(f.root.dataset.projectsEnhanced, 'true');
    assert.equal(f.intersections.length, 0);
    assert.equal(f.instance().settings.items, 1);
    assert.equal(f.instance().settings.autoplayTimeout, 6000);
    assert.equal(f.instance().settings.nav, false);
    assert.equal(f.instance().settings.dots, false);
    assert.equal(f.instance().settings.loop, false);
    assert.equal(f.instance().settings.autoplay, false);
    assert.equal(f.nav.child, undefined);
    assert.equal(f.carousel.dataset.autoplay, 'paused');
    f.remove();
    assert.ok(f.mutations[0].disconnected);
    assert.equal(f.calls.filter(x => x === 'destroy.owl.carousel').length, 1);
    const count = f.calls.length;
    f.media.dispatchEvent(new Event('change')); initialize(f.root);
    assert.equal(f.calls.length, count);
});

test('Home → Projects → About → Projects remounts independently with reduced-motion and pagehide cleanup', () => {
    const home = fixture(); initializeHome(home.root); home.remove();
    const projects = fixture(true, 'testimonials'); initialize(projects.root);
    assert.equal(projects.carousel.dataset.autoplay, 'paused');
    assert.equal(projects.instance().settings.smartSpeed, 0);
    assert.equal(projects.nav.child, undefined);
    assert.ok(projects.animated.classes.has('aos-animate'));
    projects.browser.dispatchEvent(new Event('pagehide'));
    assert.equal(projects.calls.filter(x => x === 'destroy.owl.carousel').length, 1);
    const about = fixture(false, 'partners'); initializeAbout(about.root); about.remove();
    const next = fixture(false, 'testimonials'); initialize(next.root);
    assert.equal(next.calls.filter(x => x === 'init').length, 1);
    assert.equal(next.carousel.dataset.autoplay, 'paused');
    const clone = next.replaceAnimated(); next.refreshCarousel();
    assert.ok(clone.classes.has('aos-animate'));
    next.remove();
});

test('Projects content remains revealed when Owl is unavailable', () => {
    const f = fixture(false, 'testimonials'); delete f.browser.jQuery;
    initialize(f.root);
    assert.equal(f.calls.length, 0);
    assert.ok(f.animated.classes.has('aos-animate'));
    f.remove();
    assert.ok(f.mutations[0].disconnected);
});

test('Home and Projects testimonials stay paused after refresh, hover, focus, visibility and motion changes', () => {
    for (const initializePage of [initializeHome, initialize]) {
        const f = fixture(false, 'testimonials'); initializePage(f.root);
        f.refreshCarousel();
        for (const event of ['mouseenter', 'mouseleave', 'focusin', 'focusout']) f.carousel.dispatchEvent(new Event(event));
        document.hidden = true; document.dispatchEvent(new Event('visibilitychange'));
        document.hidden = false; document.dispatchEvent(new Event('visibilitychange'));
        f.media.matches = true; f.media.dispatchEvent(new Event('change'));
        f.media.matches = false; f.media.dispatchEvent(new Event('change'));
        assert.equal(f.instance().settings.autoplay, false);
        assert.equal(f.carousel.dataset.autoplay, 'paused');
        assert.equal(f.nav.child, undefined);
        assert.equal(f.calls.includes('play.owl.autoplay'), false);
        f.remove();
    }
});

test('Home and Projects safely initialize and dispose with no testimonial carousel root', () => {
    for (const initializePage of [initializeHome, initialize]) {
        const f = fixture(false, 'testimonials');
        const query = f.root.querySelectorAll;
        // Mirrors the existing Razor Count > 0 guard for an intentionally empty collection.
        f.root.querySelectorAll = selector => selector === '[data-carousel-kind]' ? [] : query(selector);
        assert.doesNotThrow(() => { initializePage(f.root); initializePage(f.root); });
        assert.equal(f.calls.filter(x => x === 'init').length, 0);
        assert.doesNotThrow(() => f.remove());
        assert.equal(f.calls.filter(x => x === 'destroy.owl.carousel').length, 0);
        assert.ok(f.mutations[0].disconnected);
    }
});
