#!/usr/bin/env node
import fs from 'node:fs';
import path from 'node:path';

const root = process.cwd();
const targets = [
  'IN_WORLD_UI_ARCHITECTURE.md',
  'IN_WORLD_UI_IMPLEMENTATION_PLAN.md',
  'mod/StardewLivingRPG/README.md'
].map(p => path.join(root, p));

const requiredPhrases = [
  'never replace original vanilla npc dialogue',
  'additive follow-up'
];

let ok = true;
for (const file of targets) {
  if (!fs.existsSync(file)) {
    console.error(`[FAIL] Missing required policy file: ${path.relative(root, file)}`);
    ok = false;
    continue;
  }

  const text = fs.readFileSync(file, 'utf8').toLowerCase();
  for (const phrase of requiredPhrases) {
    if (!text.includes(phrase)) {
      console.error(`[FAIL] ${path.relative(root, file)} missing phrase: "${phrase}"`);
      ok = false;
    }
  }
}

if (!ok) {
  console.error('\nDialogue policy check failed. Please preserve vanilla dialogue and keep mod text additive.');
  process.exit(1);
}

console.log('Dialogue policy check passed.');
