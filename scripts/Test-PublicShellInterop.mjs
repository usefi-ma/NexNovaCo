import test from 'node:test';
import assert from 'node:assert/strict';
import { initialize, dispose } from '../src/NexNovaCo.Web/wwwroot/js/public-shell.js';

test('Skip link focuses main without navigating against the root base URL; repeated init cleans listeners', () => {
    const skip = new EventTarget();
    const top = new EventTarget();
    let focus = 0, scroll = 0, topBehavior;
    const main = { focus() { focus++; }, scrollIntoView(options) { scroll++; assert.equal(options.behavior, 'instant'); } };
    top.classList = { toggle() {} };
    globalThis.window = Object.assign(new EventTarget(), { scrollY: 150, matchMedia: () => ({ matches: true }), scrollTo: options => topBehavior = options.behavior });
    globalThis.document = {
        querySelector: selector => selector === '.skip-link' ? skip : selector === '.footer .gotop' ? top : null,
        getElementById: id => id === 'main-content' ? main : null
    };
    initialize(); initialize();
    const event = new Event('click', { cancelable: true });
    skip.dispatchEvent(event);
    assert.ok(event.defaultPrevented);
    assert.equal(focus, 1); assert.equal(scroll, 1);
    top.dispatchEvent(new Event('click', { cancelable: true }));
    assert.equal(topBehavior, 'instant');
    assert.equal(focus, 2);
    dispose();
    skip.dispatchEvent(new Event('click')); top.dispatchEvent(new Event('click'));
    assert.equal(focus, 2);
});
