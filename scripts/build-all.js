#!/usr/bin/env node
/**
 * Run build in each workspace from its directory to avoid Windows
 * "The input line is too long" when using pnpm --filter from root.
 */
const { spawnSync } = require('child_process');
const path = require('path');
const root = path.resolve(__dirname, '..');

const workspaces = [
  'client/packages/contracts',
  'client/packages/design',
  'client/apps/builder',
  'client/apps/dashboard',
  'client/apps/runtime',
];

for (const ws of workspaces) {
  const cwd = path.join(root, ws);
  console.log(`\n>> Building ${ws}...`);
  const r = spawnSync('pnpm', ['run', 'build'], {
    cwd,
    stdio: 'inherit',
    shell: true,
    env: { ...process.env, NODE_PATH: undefined },
  });
  if (r.status !== 0) {
    process.exit(r.status ?? 1);
  }
}
console.log('\n>> All builds completed.\n');
