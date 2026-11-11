import { readFileSync, readdirSync, statSync } from 'node:fs';
import { join } from 'node:path';
import { pathToFileURL } from 'node:url';

const SAME_RUN_WINDOW_MS = 10 * 60 * 1000;

export function latestRunReports(folder) {
  const reports = readdirSync(folder)
    .filter((name) => name.endsWith('.cobertura.xml'))
    .map((name) => ({ path: join(folder, name), modified: statSync(join(folder, name)).mtimeMs }));
  const newest = Math.max(...reports.map((report) => report.modified));
  return reports.filter((report) => newest - report.modified <= SAME_RUN_WINDOW_MS).map((report) => report.path);
}

export function summarize(reportXmls) {
  const modules = new Map();
  for (const xml of reportXmls) {
    collectLines(modules, xml);
  }
  return [...modules]
    .sort(([a], [b]) => a.localeCompare(b))
    .map(([name, lines]) => ({ name, covered: [...lines.values()].filter(Boolean).length, valid: lines.size }));
}

function collectLines(modules, xml) {
  for (const packageMatch of xml.matchAll(/<package [^>]*name="([^"]+)"[^>]*>([\s\S]*?)<\/package>/g)) {
    if (!modules.has(packageMatch[1])) {
      modules.set(packageMatch[1], new Map());
    }
    const lines = modules.get(packageMatch[1]);
    for (const classMatch of packageMatch[2].matchAll(/<class [^>]*filename="([^"]+)"[^>]*>([\s\S]*?)<\/class>/g)) {
      recordClassLines(lines, classMatch[1], classMatch[2]);
    }
  }
}

function recordClassLines(lines, fileName, classXml) {
  for (const lineMatch of classXml.matchAll(/<line number="(\d+)" hits="(\d+)"/g)) {
    const key = `${fileName}:${lineMatch[1]}`;
    lines.set(key, (lines.get(key) ?? false) || Number(lineMatch[2]) > 0);
  }
}

function percent(covered, valid) {
  return valid === 0 ? 'n/a' : `${((covered / valid) * 100).toFixed(1)}%`;
}

function printTable(modules) {
  const covered = modules.reduce((sum, module) => sum + module.covered, 0);
  const valid = modules.reduce((sum, module) => sum + module.valid, 0);
  console.log('| Module | Lines covered | Line coverage |');
  console.log('|---|---|---|');
  for (const module of modules) {
    console.log(`| ${module.name} | ${module.covered} / ${module.valid} | ${percent(module.covered, module.valid)} |`);
  }
  console.log(`| **Total** | ${covered} / ${valid} | **${percent(covered, valid)}** |`);
}

function main(folder) {
  const reports = readdirSync(folder).some((name) => name.endsWith('.cobertura.xml')) ? latestRunReports(folder) : [];
  if (reports.length === 0) {
    console.error(`No *.cobertura.xml in "${folder}". Run dotnet test with --coverage first.`);
    process.exit(1);
  }
  printTable(summarize(reports.map((path) => readFileSync(path, 'utf8'))));
}

if (import.meta.url === pathToFileURL(process.argv[1]).href) {
  main(process.argv[2] ?? 'TestResults');
}
