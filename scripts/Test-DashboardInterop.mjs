import test from 'node:test';
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';

const source = await readFile(new URL('../src/NexNovaCo.Web/wwwroot/js/dashboard-ui.js', import.meta.url), 'utf8');
const { submitLogout } = await import(`data:text/javascript;base64,${Buffer.from(source).toString('base64')}`);

test('Dashboard menu delegates logout to the existing native form submission', () => {
    let submissions = 0;
    submitLogout({ requestSubmit() { submissions++; } });
    assert.equal(submissions, 1);
});
