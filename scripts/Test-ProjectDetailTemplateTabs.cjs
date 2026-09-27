// Browser verification for the isolated DetailTemplateBrowserHost.
// Usage: node scripts/Test-ProjectDetailTemplateTabs.cjs <fixture.json> <playwright-module-path>
const fs = require('node:fs');
const path = require('node:path');
const assert = require('node:assert/strict');
const { chromium } = require(process.argv[3]);

const fixture = JSON.parse(fs.readFileSync(process.argv[2], 'utf8'));
const output = path.dirname(path.resolve(process.argv[2]));
const base = 'http://localhost:5199';
const route = '/dashboard/content/projects/detail-template';
const pause = milliseconds => new Promise(resolve => setTimeout(resolve, milliseconds));
const errorsFor = page => {
  const errors = [];
  page.on('pageerror', error => errors.push(error.message));
  page.on('console', message => { if (message.type() === 'error') errors.push(message.text()); });
  return errors;
};

async function openEditor(page) {
  await page.goto(base + route);
  await page.getByRole('tab', { name: 'Hero / CTA', exact: true }).waitFor();
  await pause(600);
}

(async () => {
  const browser = await chromium.launch({ channel: 'chrome', headless: true });
  const results = [];
  try {
    const context = await browser.newContext({ viewport: { width: 1440, height: 1000 } });
    await context.addCookies(fixture.cookies);
    const page = await context.newPage();
    const errors = errorsFor(page);
    await openEditor(page);

    const expectedTabs = ['Hero / CTA', 'Breadcrumb', 'Section Headings', 'Metadata'];
    assert.deepEqual(await page.getByRole('tab').allTextContents(), expectedTabs);
    assert.equal(await page.getByRole('tab', { name: 'Hero / CTA', exact: true }).getAttribute('aria-selected'), 'true');
    const fieldsByTab = {
      'Hero / CTA': ['Hero CTA text', 'Return CTA text'],
      Breadcrumb: ['Home breadcrumb label', 'Section breadcrumb label'],
      'Section Headings': ['Project Details heading', 'Features heading'],
      Metadata: ['Client label', 'Category label', 'Date label', 'Technologies label']
    };
    for (const [tab, labels] of Object.entries(fieldsByTab)) {
      await page.getByRole('tab', { name: tab, exact: true }).click();
      await pause(400);
      assert.equal(await page.getByRole('dialog').count(), 0, 'Tab switching must not warn about unsaved changes.');
      for (const label of labels) assert.equal(await page.getByLabel(label, { exact: true }).count(), 1);
      const visibleLabels = [];
      for (const groupLabels of Object.values(fieldsByTab)) for (const label of groupLabels)
        if (await page.getByLabel(label, { exact: true }).isVisible()) visibleLabels.push(label);
      assert.deepEqual(visibleLabels, labels, `${tab} owns exactly its mapped fields.`);
    }
    assert.equal(await page.getByLabel(/breadcrumb label|CTA text|heading$|Client label|Category label|Date label|Technologies label/).count(), 10);

    await page.getByRole('tab', { name: 'Hero / CTA', exact: true }).click();
    const hero = page.getByLabel('Hero CTA text', { exact: true });
    const originalHero = await hero.inputValue();
    const suffix = String(Date.now()).slice(-6);
    const testHero = `Hero QA ${suffix}`;
    const testClient = `Client QA ${suffix}`;
    await hero.fill(testHero);
    await page.getByRole('tab', { name: 'Metadata', exact: true }).click();
    const client = page.getByLabel('Client label', { exact: true });
    const originalClient = await client.inputValue();
    const technologies = page.getByLabel('Technologies label', { exact: true });
    const originalTechnologies = await technologies.inputValue();
    await client.fill(testClient);
    assert.equal(await hero.inputValue(), testHero);

    await page.getByRole('link', { name: 'Manage Projects', exact: true }).click();
    await page.getByRole('button', { name: 'Stay', exact: true }).click();
    assert.equal(await page.getByRole('tab', { name: 'Metadata', exact: true }).getAttribute('aria-selected'), 'true');
    assert.equal(await client.inputValue(), testClient);

    await page.getByRole('button', { name: 'Save changes', exact: true }).click();
    await page.getByRole('status').filter({ hasText: 'Project Detail Template saved.' }).waitFor();
    await page.reload();
    await page.getByRole('tab', { name: 'Hero / CTA', exact: true }).waitFor();
    assert.equal(await page.getByLabel('Hero CTA text', { exact: true }).inputValue(), testHero);
    await page.getByRole('tab', { name: 'Metadata', exact: true }).click();
    assert.equal(await page.getByLabel('Client label', { exact: true }).inputValue(), testClient);

    await technologies.fill('');
    await page.getByRole('tab', { name: 'Hero / CTA', exact: true }).click();
    await page.getByRole('button', { name: 'Save changes', exact: true }).click();
    await page.waitForFunction(() => [...document.querySelectorAll('[role="tab"]')]
      .some(tab => tab.textContent.trim() === 'Metadata' && tab.getAttribute('aria-selected') === 'true'));
    assert.equal(await page.getByRole('tab', { name: 'Metadata', exact: true }).getAttribute('aria-selected'), 'true');
    assert.ok(await page.locator('.validation-message').count() > 0);
    await page.getByRole('link', { name: 'Manage Projects', exact: true }).click();
    await page.getByRole('button', { name: 'Stay', exact: true }).click();

    await page.getByLabel('Technologies label', { exact: true }).fill(originalTechnologies);
    await page.getByLabel('Client label', { exact: true }).fill(originalClient);
    await page.getByRole('tab', { name: 'Hero / CTA', exact: true }).click();
    await page.getByLabel('Hero CTA text', { exact: true }).fill(originalHero);
    await page.getByRole('button', { name: 'Save changes', exact: true }).click();
    await page.getByRole('status').filter({ hasText: 'Project Detail Template saved.' }).waitFor();

    const firstTab = page.getByRole('tab', { name: 'Hero / CTA', exact: true });
    await firstTab.focus();
    await page.keyboard.press('ArrowRight');
    await page.waitForFunction(() => document.activeElement?.getAttribute('role') === 'tab' && document.activeElement?.textContent?.trim() === 'Breadcrumb');
    assert.notEqual(await page.getByRole('tab', { name: 'Breadcrumb', exact: true }).evaluate(element => getComputedStyle(element).outlineStyle), 'none');
    await page.keyboard.press('Enter');
    await page.waitForFunction(() => [...document.querySelectorAll('[role="tab"]')]
      .some(tab => tab.textContent.trim() === 'Breadcrumb' && tab.getAttribute('aria-selected') === 'true'));

    await page.getByRole('button', { name: 'View Sample Project', exact: true }).click();
    await page.waitForURL('**/projects/**');
    await page.waitForLoadState('load');
    assert.equal(await page.getByRole('dialog').count(), 0);
    const cleanPage = await context.newPage();
    const cleanErrors = errorsFor(cleanPage);
    await openEditor(cleanPage);
    await cleanPage.getByRole('link', { name: 'Manage Projects', exact: true }).click();
    await cleanPage.waitForURL('**/dashboard/content/shared-projects');
    assert.equal(await cleanPage.getByRole('dialog').count(), 0);
    assert.deepEqual(errors, []);
    assert.deepEqual(cleanErrors, []);
    results.push({ type: 'functional', result: 'PASS', checks: 'four tabs/ten fields, multi-tab save/reload, invalid-tab activation, dirty Stay, clean links, keyboard' });
    await context.close();

    for (const width of [1440, 1024, 768, 390]) {
      const responsiveContext = await browser.newContext({ viewport: { width, height: 1000 } });
      await responsiveContext.addCookies(fixture.cookies);
      const responsivePage = await responsiveContext.newPage();
      const responsiveErrors = errorsFor(responsivePage);
      await openEditor(responsivePage);
      const tabList = responsivePage.getByRole('tablist');
      assert.equal(await tabList.count(), 1);
      assert.equal(await responsivePage.getByRole('tab').count(), 4);
      assert.ok((await responsivePage.getByRole('tab').allTextContents()).every(label => expectedTabs.includes(label)));
      assert.ok(await responsivePage.getByRole('tab').evaluateAll(tabs => tabs.every(tab => tab.scrollWidth <= tab.clientWidth)));
      const tabRects = await responsivePage.getByRole('tab').evaluateAll(tabs => tabs.map(tab => {
        const rect = tab.getBoundingClientRect(); return { left: rect.left, right: rect.right };
      }));
      assert.ok(tabRects.every((rect, index) => index === 0 || tabRects[index - 1].right <= rect.left + 0.5));
      for (const tab of expectedTabs) {
        await responsivePage.getByRole('tab', { name: tab, exact: true }).click();
        await pause(400);
        assert.equal(await responsivePage.getByRole('tab', { name: tab, exact: true }).getAttribute('aria-selected'), 'true');
      }
      assert.ok(await responsivePage.getByRole('button', { name: 'View Sample Project', exact: true }).isVisible());
      assert.ok(await responsivePage.getByRole('button', { name: 'Save changes', exact: true }).isVisible());
      assert.equal(await responsivePage.evaluate(() => document.documentElement.scrollWidth - innerWidth), 0);
      assert.deepEqual(responsiveErrors, []);
      await responsivePage.screenshot({ path: path.join(output, `project-template-tabs-${width}.png`), fullPage: true });
      results.push({ type: 'responsive', width, result: 'PASS', overflow: 0 });
      await responsiveContext.close();
    }
  } finally {
    await browser.close();
    fs.writeFileSync(path.join(output, 'results.json'), JSON.stringify(results, null, 2));
  }
})().catch(error => { console.error(error); process.exitCode = 1; });
