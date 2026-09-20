import test from 'node:test';
import assert from 'node:assert/strict';
import { initialize } from '../src/NexNovaCo.Web/wwwroot/js/project-detail.js';
import { initialize as initializeProjects } from '../src/NexNovaCo.Web/wwwroot/js/projects.js';
import { fixture } from './fixtures/CarouselFixture.mjs';

test('Project Detail initializes reveals once without Owl or counters and cleans up on detach', () => {
    const f = fixture();
    initialize(null); initialize({ isConnected: false });
    initialize(f.root); initialize(f.root);
    assert.equal(f.root.dataset.projectDetailEnhanced, 'true');
    assert.equal(f.mutations.length, 1);
    assert.equal(f.calls.length, 0, 'The Blazor gallery must not initialize an Owl carousel');
    assert.equal(f.countInstances.length, 0);
    assert.ok(f.animated.classes.has('aos-animate'));
    f.remove();
    assert.ok(f.mutations[0].disconnected);
    f.media.matches = true; f.media.dispatchEvent(new Event('change'));
    assert.equal(f.root.dataset.reducedMotion, 'false', 'No detached reveal listeners');
});

test('Projects → Detail → Projects → another Detail leaves no duplicate visual handlers', () => {
    const listing = fixture(false, 'testimonials'); initializeProjects(listing.root); listing.remove();
    const detail = fixture(); initialize(detail.root); detail.remove();
    const nextListing = fixture(false, 'testimonials'); initializeProjects(nextListing.root);
    assert.equal(nextListing.calls.filter(x => x === 'init').length, 1);
    nextListing.remove();
    const nextDetail = fixture(); initialize(nextDetail.root);
    assert.equal(nextDetail.mutations.length, 1);
    assert.equal(nextDetail.calls.length, 0);
    nextDetail.remove();
});

test('Project Detail respects reduced motion and pagehide cleanup without vendor plugins', () => {
    const f = fixture(true); delete f.browser.jQuery;
    initialize(f.root);
    assert.equal(f.root.dataset.reducedMotion, 'true');
    assert.equal(f.animated.style.transitionDelay, '0ms');
    f.browser.dispatchEvent(new Event('pagehide'));
    assert.ok(f.mutations[0].disconnected);
    f.media.matches = false; f.media.dispatchEvent(new Event('change'));
    assert.equal(f.root.dataset.reducedMotion, 'true');
});
