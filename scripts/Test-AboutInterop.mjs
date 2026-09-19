import test from 'node:test';
import assert from 'node:assert/strict';
import { initialize } from '../src/NexNovaCo.Web/wwwroot/js/about.js';
import { initialize as initializeHome } from '../src/NexNovaCo.Web/wwwroot/js/home.js';
import { fixture } from './fixtures/CarouselFixture.mjs';

test('About initializes one partners carousel without counters and removes its handlers on detach', () => {
    const f = fixture(false, 'partners');
    initialize(f.root); initialize(f.root);
    assert.equal(f.calls.filter(x => x === 'init').length, 1);
    assert.equal(f.root.dataset.aboutEnhanced, 'true');
    assert.equal(f.root.dataset.homeEnhanced, undefined);
    assert.equal(f.intersections.length, 0, 'About must not initialize Home counters');
    assert.equal(f.nav.child.attributes.get('aria-label'), 'Pause partners');
    assert.equal(f.instance().settings.responsive[700].items, 3);
    assert.equal(f.instance().settings.responsive[1200].items, 6);
    f.nav.child.dispatchEvent(new Event('click'));
    assert.equal(f.carousel.dataset.autoplay, 'paused');
    f.remove();
    assert.equal(f.calls.filter(x => x === 'destroy.owl.carousel').length, 1);
    assert.ok(f.nav.child.removed && f.mutations[0].disconnected);
    const count = f.calls.length;
    f.media.dispatchEvent(new Event('change'));
    f.nav.child.dispatchEvent(new Event('click'));
    initialize(f.root);
    assert.equal(f.calls.length, count, 'Detached controls/listeners must not run');
});

test('Home to About to About remounts independently, with reduced motion and pagehide cleanup', () => {
    const home = fixture(); initializeHome(home.root); home.remove();
    const about = fixture(true, 'partners'); initialize(about.root);
    assert.equal(about.instance().settings.smartSpeed, 0);
    assert.equal(about.carousel.dataset.autoplay, 'paused');
    assert.ok(about.nav.child.disabled && about.animated.classes.has('aos-animate'));
    about.browser.dispatchEvent(new Event('pagehide'));
    assert.equal(about.calls.filter(x => x === 'destroy.owl.carousel').length, 1);
    const next = fixture(false, 'partners'); initialize(next.root);
    assert.equal(next.carousel.dataset.autoplay, 'playing');
    assert.equal(next.calls.filter(x => x === 'init').length, 1);
    next.instance().settings.items = 6;
    next.media.dispatchEvent(new Event('change'));
    assert.equal(next.carousel.dataset.autoplay, 'paused', 'A fully visible row must not autoplay');
    next.remove();
});

test('About reveal enhancement and cleanup still work when Owl is unavailable', () => {
    const f = fixture(false, 'partners'); delete f.browser.jQuery;
    initialize(null); initialize({ isConnected: false }); initialize(f.root);
    assert.equal(f.calls.length, 0);
    assert.equal(f.root.dataset.aboutEnhanced, 'true');
    assert.ok(f.animated.classes.has('aos-animate'));
    f.remove();
    assert.ok(f.mutations[0].disconnected);
});
