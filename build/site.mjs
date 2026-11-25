#!/usr/bin/env node
// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

// Builds, checks, serves or previews this library's site with the NCodeGroup/docs toolchain, from the main
// checkout and from any worktree of it alike. A library copies this file from fixture/build/site.mjs in
// NCodeGroup/docs into its build/ folder, and copies it again to take a change.
//
//   node build/site.mjs <build|check|dev|preview> [--toolchain <folder>] [options of scripts/site.mjs]
//
// The toolchain is the folder named by --toolchain, else by DOCS_TOOLCHAIN, else the docs folder beside the
// library's main checkout, which is found through Git so that a worktree finds the same one.
import { spawnSync } from 'node:child_process';
import { existsSync } from 'node:fs';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

// The library is the folder above this file's build/ folder.
const library = dirname(dirname(fileURLToPath(import.meta.url)));

const remaining = [];
let toolchain;
const args = process.argv.slice(2);
// A following option is not a folder, so a missing value must not swallow it.
const folderOf = (value) => {
  if (value === undefined || value === '' || value.startsWith('-')) {
    console.error('--toolchain needs a folder.');
    process.exit(2);
  }
  return value;
};
for (let index = 0; index < args.length; index++) {
  const arg = args[index];
  if (arg === '--toolchain') {
    toolchain = folderOf(args[index + 1]);
    index++;
  } else if (arg.startsWith('--toolchain=')) {
    toolchain = folderOf(arg.slice('--toolchain='.length));
  } else {
    remaining.push(arg);
  }
}

toolchain ??= process.env.DOCS_TOOLCHAIN;

if (!toolchain) {
  const output = spawnSync('git', ['-C', library, 'rev-parse', '--path-format=absolute', '--git-common-dir'], {
    encoding: 'utf8',
  });
  if (output.error || output.status !== 0) {
    // Git's own first line says why (no repository, an unsafe folder, no git on the path), which a guess would hide.
    const reason = output.error?.message ?? (output.stderr.trim().split(/\r?\n/)[0] || `git exited ${output.status}`);
    console.error(
      `git cannot name the main checkout of ${library} (${reason}), so the toolchain beside it cannot be found: name it with --toolchain <folder> or DOCS_TOOLCHAIN.`,
    );
    process.exit(1);
  }
  // The common Git folder is the main checkout's .git from the main checkout and from every worktree, so the
  // folder above it is the main checkout and the toolchain sits beside that.
  toolchain = join(dirname(dirname(output.stdout.trim())), 'docs');
}

toolchain = resolve(toolchain);
const site = join(toolchain, 'scripts', 'site.mjs');
if (!existsSync(site)) {
  console.error(
    `No NCodeGroup/docs toolchain at ${toolchain}: clone https://github.com/NCodeGroup/docs there and run "npm ci --ignore-scripts" in it, or name its folder with --toolchain <folder> or DOCS_TOOLCHAIN.`,
  );
  process.exit(1);
}

const [command, ...rest] = remaining;
// This runs Node.js, never npm, so no separator has to pass npm's PowerShell shim, and a relative path in an
// option starts from the current folder, as scripts/site.mjs resolves it.
const result = spawnSync(process.execPath, command === undefined ? [site] : [site, command, library, ...rest], {
  stdio: 'inherit',
});
process.exit(result.status ?? 1);
