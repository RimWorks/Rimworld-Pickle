import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import { createRequire } from 'node:module';

const require = createRequire(import.meta.url);
const { chromium } = require(process.env.PICKLE_PLAYWRIGHT_MODULE || 'playwright');
const html = await readFile(new URL('../dist/index.html', import.meta.url), 'utf8');
const scenario = (name, index, tags = []) => ({ name, index, tags, selected: true, visible: true, line: index + 3, outcome: 'Pending', durationMs: 0, failureMessage: null, logTail: [], attachments: [], stateDumps: [], attempts: 1, failedAttempts: [], tickCost: null, steps: [{ keyword: 'Given', text: 'the save "test-colony" is loaded', status: 'Pending', durationMs: 0, failureMessage: null }] });
let snap = {
  status: 'idle', feature: '', scenario: '', step: '', passed: 0, failed: 0, cancelRequested: false,
  watch: true, breakOnFailure: true, includeWip: false, showRunPill: true, controllable: true, search: '', modFilter: null, tagFilters: [],
  features: [
    { name: 'Pawn steps', mod: 'Pickle', path: '/mods/Pickle/pawn.feature', tags: [], scenarios: [scenario('A pawn walks', 0, ['@smoke']), scenario('A pawn sleeps', 1)] },
    { name: 'Map steps', mod: 'Example mod', path: '/mods/Example/map.feature', tags: [], scenarios: [scenario('A pawn walks', 2)] },
  ],
};
let catalog = { canSave: true, busy: false, suites: [{ id: '/mods/Pickle/Fixtures', mod: 'Pickle', directory: '/recorded/Pickle', fixtures: [{ name: 'test-colony', path: '/recorded/Pickle/test-colony.rws', recorded: true, shadowedPath: '/mods/Pickle/Fixtures/test-colony.rws', sizeBytes: 2048, modified: '2026-09-05 12:00', gameVersion: '1.6', scenarioName: 'Crashlanded' }] }] };
const stepCatalogue = { steps: [
  { pattern: 'the save {string} is loaded', kind: 'Given', source: 'Pickle engine' },
  { pattern: '{string} is drafted', kind: 'Then', source: 'PawnSteps' },
] };
let stepResult = {
  keyword: 'When', text: '"Soldier" is drafted', status: 'Failed', durationMs: 34,
  failureMessage: "pawn 'Soldier' should be drafted; actual job: Wait_Wander",
  skeleton: null, stateDumps: [{ source: 'ColonistSteps.Colonists', content: 'Soldier: undrafted' }],
};
const commands = [];
const browser = await chromium.launch({ headless: true, args: ['--no-sandbox'] });
try {
  const page = await browser.newPage({ viewport: { width: 1440, height: 1000 } });
  const errors = [];
  page.on('pageerror', error => errors.push(error.message));
  await page.route('http://pickle.test/**', async route => {
    const request = route.request();
    const url = new URL(request.url());
    if (url.pathname === '/') return route.fulfill({ contentType: 'text/html', body: html });
    if (url.pathname === '/state') return route.fulfill({ json: snap });
    if (url.pathname === '/fixtures') return route.fulfill({ json: catalog });
    if (url.pathname === '/report') return route.fulfill({ contentType: 'text/html', body: '<h1>Report</h1>' });
    if (url.pathname === '/steps') return route.fulfill({ json: stepCatalogue });
    commands.push(url);
    assert.equal(request.method(), 'POST');
    if (url.pathname === '/select') {
      for (const feature of snap.features) for (const row of feature.scenarios) {
        if (url.searchParams.has('mod') && feature.mod !== url.searchParams.get('mod')) continue;
        if (url.searchParams.has('path') && feature.path !== url.searchParams.get('path')) continue;
        if (url.searchParams.has('index') && row.index !== Number(url.searchParams.get('index'))) continue;
        row.selected = url.searchParams.get('on') === 'true';
      }
    }
    if (url.pathname === '/filter') {
      if (url.searchParams.has('search')) snap.search = url.searchParams.get('search');
      if (url.searchParams.has('mod')) snap.modFilter = url.searchParams.get('mod') || null;
      if (url.searchParams.has('tag')) {
        const tag = url.searchParams.get('tag');
        snap.tagFilters = snap.tagFilters.includes(tag) ? [] : [tag];
      }
      for (const feature of snap.features) for (const row of feature.scenarios) {
        row.visible = (!snap.modFilter || feature.mod === snap.modFilter)
          && `${row.name} ${feature.name}`.toLowerCase().includes(snap.search.toLowerCase())
          && snap.tagFilters.every(tag => row.tags.includes(tag));
        if (url.searchParams.has('tag')) row.selected = row.visible;
      }
    }
    if (url.pathname === '/step') return route.fulfill({ json: stepResult });
    if (url.pathname === '/pill') snap.showRunPill = url.searchParams.get('on') === 'true';
    if (url.pathname === '/continue') snap.status = 'running';
    if (url.pathname === '/fixture') {
      const action = url.searchParams.get('action');
      if (action === 'rename') return route.fulfill({ status: 400, json: { error: 'The target fixture already exists.' } });
      if (action === 'delete') catalog.suites[0].fixtures = [];
      return route.fulfill({ json: catalog });
    }
    return route.fulfill({ json: { ok: true } });
  });
  await page.goto('http://pickle.test');
  await page.getByRole('checkbox', { name: 'Select Pickle', exact: true }).click();
  await page.waitForFunction(() => !document.querySelector('input[aria-label="Select A pawn sleeps"]').checked);
  await page.getByRole('checkbox', { name: 'Select Pawn steps', exact: true }).click();
  await page.waitForFunction(() => document.querySelector('input[aria-label="Select A pawn sleeps"]').checked);
  await page.getByRole('checkbox', { name: 'Select A pawn sleeps', exact: true }).click();
  await page.waitForFunction(() => document.querySelector('input[aria-label="Select Pawn steps"]').indeterminate);
  await page.getByRole('button', { name: '@smoke', exact: true }).click();
  await page.waitForFunction(() => !document.querySelector('input[aria-label="Select A pawn sleeps"]'));
  assert.equal(await page.getByRole('checkbox', { name: 'Select A pawn walks', exact: true }).count(), 1);
  await page.getByRole('button', { name: '@smoke', exact: true }).click();
  await page.getByRole('searchbox').fill('sleeps');
  await page.waitForFunction(() => !document.querySelector('input[aria-label="Select A pawn walks"]'));
  await page.getByRole('searchbox').fill('');
  await page.getByRole('combobox', { name: 'Filter by mod' }).selectOption('Example mod');
  await page.waitForFunction(() => !document.querySelector('input[aria-label="Select A pawn sleeps"]'));
  await page.getByRole('combobox', { name: 'Filter by mod' }).selectOption('');
  await page.getByRole('button', { name: 'Fixtures', exact: true }).click();
  await page.getByRole('button', { name: 'Load', exact: true }).click();
  await page.getByRole('status').filter({ hasText: 'load finished' }).waitFor();
  await page.getByRole('button', { name: 'Rename', exact: true }).click();
  await page.getByRole('textbox', { name: 'New fixture name' }).fill('other-colony');
  await page.getByRole('button', { name: 'Rename', exact: true }).click();
  await page.getByRole('alert').filter({ hasText: 'already exists' }).waitFor();
  await page.getByRole('button', { name: 'Cancel', exact: true }).click();
  page.once('dialog', dialog => dialog.dismiss());
  await page.getByRole('button', { name: 'Delete', exact: true }).click();
  assert.equal(commands.filter(url => url.searchParams.get('action') === 'delete').length, 0);
  page.once('dialog', dialog => dialog.accept());
  await page.getByRole('button', { name: 'Delete', exact: true }).click();
  await page.getByRole('status').filter({ hasText: 'delete finished' }).waitFor();
  await page.getByRole('button', { name: 'Back to results' }).click();

  const runPill = page.getByRole('checkbox', { name: 'Show run pill' });
  await runPill.uncheck();
  assert.equal(commands.at(-1).pathname, '/pill');
  assert.equal(commands.at(-1).searchParams.get('on'), 'false');
  assert.equal(await runPill.isChecked(), false);

  await page.getByRole('button', { name: 'Step console', exact: true }).click();
  await page.getByText('2 steps registered').waitFor();
  await page.getByRole('textbox', { name: 'Step to run' }).fill('"Soldier" is drafted');
  await page.getByRole('button', { name: 'Run step', exact: true }).click();
  await page.getByText('actual job: Wait_Wander').waitFor();
  assert.equal(commands.at(-1).searchParams.get('text'), '"Soldier" is drafted');
  await page.getByRole('group').filter({ hasText: 'ColonistSteps.Colonists' }).click();
  await page.getByText('Soldier: undrafted').waitFor();
  // The input clears on success, and the up arrow puts the last step back for a re-run.
  assert.equal(await page.getByRole('textbox', { name: 'Step to run' }).inputValue(), '');
  await page.getByRole('textbox', { name: 'Step to run' }).press('ArrowUp');
  assert.equal(await page.getByRole('textbox', { name: 'Step to run' }).inputValue(), '"Soldier" is drafted');
  stepResult = { ...stepResult, text: 'I wibble the wobble', status: 'Undefined', failureMessage: null, skeleton: '[When("I wibble the wobble")]', stateDumps: [] };
  await page.getByRole('textbox', { name: 'Step to run' }).fill('I wibble the wobble');
  await page.getByRole('button', { name: 'Run step', exact: true }).click();
  await page.getByText('[When("I wibble the wobble")]').waitFor();
  await page.getByRole('button', { name: 'Reset context', exact: true }).click();
  await page.getByRole('status').filter({ hasText: 'Context reset' }).waitFor();
  assert.equal(commands.at(-1).pathname, '/step/reset');
  await page.getByRole('button', { name: 'Back to results' }).click();

  // A flake reads as a flake: badge on the row, earlier failures in the detail pane.
  snap.features[0].scenarios[0].outcome = 'Passed';
  snap.features[0].scenarios[0].attempts = 3;
  snap.features[0].scenarios[0].failedAttempts = [{ attempt: 1, message: 'lost the race' }, { attempt: 2, message: null }];
  await page.waitForFunction(() => document.body.innerText.includes('flaky'));
  await page.getByRole('button', { name: 'A pawn walks' }).first().click();
  await page.getByText('Flaky: passed on attempt 3').waitFor();
  await page.getByText('1: lost the race').waitFor();
  await page.getByText('2: Scenario failed').waitFor();
  snap.features[0].scenarios[0].outcome = 'Pending';
  snap.features[0].scenarios[0].attempts = 1;
  snap.features[0].scenarios[0].failedAttempts = [];

  snap = { ...snap, status: 'paused', feature: 'Map steps', scenario: 'A pawn walks' };
  snap.features[1].scenarios[0].outcome = 'Running';
  await page.getByRole('button', { name: 'Continue run', exact: true }).click();
  assert.equal(commands.at(-1).pathname, '/continue');
  await page.waitForFunction(() => document.querySelector('[data-pickle-selected="true"]')?.textContent.includes('A pawn walks'));
  assert.match(await page.locator('main').innerText(), /Example mod \/ map.feature:5/);
  assert.deepEqual(errors, []);
  await page.screenshot({ path: '/tmp/pickle-dashboard-desktop.png' });
  await page.setViewportSize({ width: 390, height: 844 });
  await page.screenshot({ path: '/tmp/pickle-dashboard-mobile.png' });
  assert.equal(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth), true);
  console.log('Browser parity checks passed: filters, group selection, fixtures, step console, flaky retries, errors, delete confirmation, pause/resume, duplicate names, responsive layout.');
} finally {
  await browser.close();
}
