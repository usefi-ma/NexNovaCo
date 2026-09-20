import test from 'node:test';
import assert from 'node:assert/strict';
import { initialize } from '../src/NexNovaCo.Web/wwwroot/js/contact-page.js';
import { fixture } from './fixtures/CarouselFixture.mjs';

test('Contact initializes visual reveals once without plugins or form handlers, then detaches', () => {
    const f = fixture();
    initialize(null); initialize(f.root); initialize(f.root);
    assert.equal(f.root.dataset.contactEnhanced, 'true');
    assert.equal(f.mutations.length, 1);
    assert.equal(f.calls.length, 0);
    assert.equal(f.countInstances.length, 0);
    assert.ok(f.animated.classes.has('aos-animate'));
    f.remove();
    assert.ok(f.mutations[0].disconnected);
});

test('Contact supports reduced motion and independent remount after pagehide', () => {
    const first = fixture(true); delete first.browser.jQuery;
    initialize(first.root);
    assert.equal(first.root.dataset.reducedMotion, 'true');
    assert.equal(first.animated.style.transitionDelay, '0ms');
    first.browser.dispatchEvent(new Event('pagehide'));
    assert.ok(first.mutations[0].disconnected);
    const second = fixture(); initialize(second.root);
    assert.equal(second.mutations.length, 1);
    second.remove();
    assert.ok(second.mutations[0].disconnected);
});
