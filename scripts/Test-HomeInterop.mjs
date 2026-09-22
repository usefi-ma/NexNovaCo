// Dependency-free logic/lifecycle tests. Real Owl behavior is verified separately in the browser.
import test from 'node:test';
import assert from 'node:assert/strict';
import { initialize } from '../src/NexNovaCo.Web/wwwroot/js/home.js';

import { fixture } from './fixtures/CarouselFixture.mjs';

test('CMS numbers drive CountUp on each new Home root without duplicate initialization', () => {
    for (const value of [0, 725, 9999]) {
        const f = fixture();
        f.counter.dataset.countValue = String(value);
        initialize(f.root);
        initialize(f.root);
        assert.equal(f.intersections.length, 1);
        f.intersections[0].callback([{ isIntersecting: true, target: f.counter }]);
        assert.equal(f.countInstances.length, 1);
        assert.equal(f.countInstances[0].endValue, value);
        assert.equal(f.countInstances[0].target, f.display);
        assert.equal(f.countInstances[0].starts, 1);
        f.remove();
        assert.equal(f.countInstances[0].resets, 1);
        assert.ok(f.intersections[0].disconnected);
    }
});

test('responsive carousel refresh reveals replacement loop slides and detaches the refresh listener', () => {
    const f = fixture();
    f.animated.offsetTop = 1200;
    initialize(f.root);
    assert.equal(f.animated.classes.has('aos-animate'), false);
    const replacement = f.replaceAnimated();
    f.refreshCarousel();
    assert.ok(replacement.classes.has('aos-init'));
    assert.ok(replacement.classes.has('aos-animate'), 'New visible clones must not stay transparent');
    assert.equal(replacement.style.transitionDelay, '100ms');
    assert.equal(f.animated.classes.has('aos-animate'), false, 'Discarded clones are no longer tracked');
    f.remove();
    const detached = f.replaceAnimated();
    f.root.dispatchEvent(new Event('theme:carousel-refreshed'));
    f.refreshCarousel();
    assert.equal(detached.classes.has('aos-init'), false, 'Both refresh listeners are removed');
});

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
