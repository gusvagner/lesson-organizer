import assert from 'node:assert/strict';
import { mkdtempSync, rmSync, utimesSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { test } from 'node:test';
import { latestRunReports, summarize } from './coverage-summary.mjs';

const report = (lines) =>
  `<coverage><packages><package name="LessonOrganizer.Domain"><classes>` +
  `<class name="A" filename="a.cs"><lines>${lines}</lines></class>` +
  `</classes></package></packages></coverage>`;

test('two test projects of one run → lines merged by file and line', () => {
  const modules = summarize([
    report('<line number="1" hits="1" /><line number="2" hits="0" />'),
    report('<line number="2" hits="3" /><line number="3" hits="0" />'),
  ]);
  assert.deepEqual(modules, [{ name: 'LessonOrganizer.Domain', covered: 2, valid: 3 }]);
});

test('reports from an older run in the folder → ignored', () => {
  const folder = mkdtempSync(join(tmpdir(), 'lo-coverage-'));
  try {
    writeFileSync(join(folder, 'old.cobertura.xml'), report('<line number="9" hits="1" />'));
    writeFileSync(join(folder, 'new-a.cobertura.xml'), report('<line number="1" hits="1" />'));
    writeFileSync(join(folder, 'new-b.cobertura.xml'), report('<line number="2" hits="0" />'));
    const oneHourAgo = new Date(Date.now() - 60 * 60 * 1000);
    utimesSync(join(folder, 'old.cobertura.xml'), oneHourAgo, oneHourAgo);
    assert.deepEqual(latestRunReports(folder).map((path) => path.split(/[\\/]/).pop()).sort(), [
      'new-a.cobertura.xml',
      'new-b.cobertura.xml',
    ]);
  } finally {
    rmSync(folder, { recursive: true, force: true });
  }
});

test('module with no lines → 0 valid, no NaN', () => {
  const modules = summarize([
    '<coverage><packages><package name="Empty"><classes></classes></package></packages></coverage>',
  ]);
  assert.deepEqual(modules, [{ name: 'Empty', covered: 0, valid: 0 }]);
});
