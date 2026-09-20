import test from 'node:test';
import assert from 'node:assert/strict';
import { initialize } from '../src/NexNovaCo.Web/wwwroot/js/projects.js';
import { initialize as initializeHome } from '../src/NexNovaCo.Web/wwwroot/js/home.js';
import { initialize as initializeAbout } from '../src/NexNovaCo.Web/wwwroot/js/about.js';
import { fixture } from './fixtures/CarouselFixture.mjs';

test('Projects initializes testimonials once, keeps listing/counters out of Owl, and disposes controls', () => {
    const f = fixture(false, 'testimonials');
    initialize(null); initialize({ isConnected: false });
    initialize(f.root); initialize(f.root);
    assert.equal(f.calls.filter(x => x === 'init').length, 1);
    assert.equal(f.root.dataset.projectsEnhanced, 'true');
    assert.equal(f.intersections.length, 0);
    assert.equal(f.instance().settings.items, 1);
    assert.equal(f.instance().settings.autoplayTimeout, 6000);
    assert.equal(f.nav.child.attributes.get('aria-label'), 'Pause testimonials');
    f.nav.child.dispatchEvent(new Event('click'));
    assert.equal(f.nav.child.attributes.get('aria-label'), 'Play testimonials');
    assert.equal(f.carousel.dataset.autoplay, 'paused');
    f.remove();
    assert.ok(f.nav.child.removed && f.mutations[0].disconnected);
    assert.equal(f.calls.filter(x => x === 'destroy.owl.carousel').length, 1);
    const count = f.calls.length;
    f.media.dispatchEvent(new Event('change')); f.nav.child.dispatchEvent(new Event('click')); initialize(f.root);
    assert.equal(f.calls.length, count);
});

test('Home → Projects → About → Projects remounts independently with reduced-motion and pagehide cleanup', () => {
    const home = fixture(); initializeHome(home.root); home.remove();
    const projects = fixture(true, 'testimonials'); initialize(projects.root);
    assert.equal(projects.carousel.dataset.autoplay, 'paused');
    assert.equal(projects.instance().settings.smartSpeed, 0);
    assert.ok(projects.nav.child.disabled && projects.animated.classes.has('aos-animate'));
    projects.browser.dispatchEvent(new Event('pagehide'));
    assert.equal(projects.calls.filter(x => x === 'destroy.owl.carousel').length, 1);
    const about = fixture(false, 'partners'); initializeAbout(about.root); about.remove();
    const next = fixture(false, 'testimonials'); initialize(next.root);
    assert.equal(next.calls.filter(x => x === 'init').length, 1);
    assert.equal(next.carousel.dataset.autoplay, 'playing');
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
