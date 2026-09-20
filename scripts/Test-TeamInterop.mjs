import test from 'node:test';
import assert from 'node:assert/strict';
import { initialize } from '../src/NexNovaCo.Web/wwwroot/js/team.js';
import { initialize as initializeHome } from '../src/NexNovaCo.Web/wwwroot/js/home.js';
import { fixture } from './fixtures/CarouselFixture.mjs';

test('Team/profile reveals initialize once without carousel/counter plugins and dispose on detach', () => {
    const f = fixture();
    initialize(null); initialize({ isConnected: false });
    initialize(f.root); initialize(f.root);
    assert.equal(f.root.dataset.teamEnhanced, 'true');
    assert.equal(f.mutations.length, 1);
    assert.equal(f.calls.length, 0);
    assert.equal(f.countInstances.length, 0);
    assert.ok(f.animated.classes.has('aos-animate'));
    f.remove();
    assert.ok(f.mutations[0].disconnected);
    f.media.matches = true; f.media.dispatchEvent(new Event('change'));
    assert.equal(f.root.dataset.reducedMotion, 'false');
});

test('Home → Team → Member → Team → another Member remounts independent visual lifecycles', () => {
    const home = fixture(); initializeHome(home.root); home.remove();
    for (const route of ['team', 'emilyjohnson', 'team', 'lenaalvarez']) {
        const f = fixture(); initialize(f.root);
        assert.equal(f.mutations.length, 1, route);
        assert.equal(f.calls.length, 0, route);
        f.remove();
        assert.ok(f.mutations[0].disconnected, route);
    }
});

test('Team/profile reduced motion and pagehide work without jQuery or other visual plugins', () => {
    const f = fixture(true); delete f.browser.jQuery;
    initialize(f.root);
    assert.equal(f.root.dataset.reducedMotion, 'true');
    assert.equal(f.animated.style.transitionDelay, '0ms');
    f.browser.dispatchEvent(new Event('pagehide'));
    assert.ok(f.mutations[0].disconnected);
    f.media.matches = false; f.media.dispatchEvent(new Event('change'));
    assert.equal(f.root.dataset.reducedMotion, 'true');
});
