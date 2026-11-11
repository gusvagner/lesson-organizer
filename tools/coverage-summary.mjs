import { readFileSync, readdirSync } from 'node:fs';
import { join } from 'node:path';

const resultsDirectory = process.argv[2] ?? 'TestResults';
const reports = readdirSync(resultsDirectory).filter((name) => name.endsWith('.cobertura.xml'));

if (reports.length === 0) {
  console.error(`No *.cobertura.xml in "${resultsDirectory}". Run dotnet test with --coverage first.`);
  process.exit(1);
}

const modules = new Map();

for (const report of reports) {
  collectLines(readFileSync(join(resultsDirectory, report), 'utf8'));
}

printTable();

function collectLines(xml) {
  for (const packageMatch of xml.matchAll(/<package [^>]*name="([^"]+)"[^>]*>([\s\S]*?)<\/package>/g)) {
    const lines = linesOf(packageMatch[1]);
    for (const classMatch of packageMatch[2].matchAll(/<class [^>]*filename="([^"]+)"[^>]*>([\s\S]*?)<\/class>/g)) {
      recordClassLines(lines, classMatch[1], classMatch[2]);
    }
  }
}

function linesOf(moduleName) {
  if (!modules.has(moduleName)) {
    modules.set(moduleName, new Map());
  }
  return modules.get(moduleName);
}

function recordClassLines(lines, fileName, classXml) {
  for (const lineMatch of classXml.matchAll(/<line number="(\d+)" hits="(\d+)"/g)) {
    const key = `${fileName}:${lineMatch[1]}`;
    const covered = Number(lineMatch[2]) > 0;
    lines.set(key, (lines.get(key) ?? false) || covered);
  }
}

function printTable() {
  let totalCovered = 0;
  let totalValid = 0;
  console.log('| Module | Lines covered | Line coverage |');
  console.log('|---|---|---|');
  for (const [moduleName, lines] of [...modules].sort(([a], [b]) => a.localeCompare(b))) {
    const covered = [...lines.values()].filter(Boolean).length;
    totalCovered += covered;
    totalValid += lines.size;
    console.log(`| ${moduleName} | ${covered} / ${lines.size} | ${percent(covered, lines.size)} |`);
  }
  console.log(`| **Total** | ${totalCovered} / ${totalValid} | **${percent(totalCovered, totalValid)}** |`);
}

function percent(covered, valid) {
  return `${((covered / valid) * 100).toFixed(1)}%`;
}
