# 0021. Tooling is written in Node.js, then Go, then PowerShell

- **Status:** Accepted
- **Date:** 2026-11-15

## Context

A repository runs tooling besides its product: generators, checks, and helpers for CI, hooks and releases. Each
language that tooling is written in is one more runtime that every PC, CI runner and cloud session installs, pins and
keeps current ([ADR-0008](0008-external-inputs-are-pinned-and-kept-current.md)), and one more language a reader has to
follow. The template already provides two runtimes: Node.js, which nvs selects from `.node-version` in every
environment, and PowerShell 7, in which every workflow command runs. Go builds a tool into one binary that needs no
runtime at all. Python would be a fourth runtime, with a version and package managers (pip, uv) of its own, and it isn't
one of the languages the team prefers for tooling.

## Decision

Tooling is written in the first of these languages that fits the job:

1. **Node.js.** nvs and `.node-version` already provide it in a project that answers `uses_node`, and a project that
   doesn't use Node.js yet turns that answer on to get them. A tool is an ES module (`.mjs`) that uses Node's built-in
   modules where they do the job. A package it needs is pinned exactly in a `package.json` with a committed lock file
   and installed with `npm ci --ignore-scripts`.
2. **Go**, for a tool that must ship as one self-contained binary, or that Node.js can't run fast enough.
3. **PowerShell 7**, for glue around other commands, such as a gate that runs a build and its tests, and for the
   scripts that set up the toolchain itself or run before it can, such as the nvs setup scripts and
   `.template/Update-Project.ps1`.

Python isn't on the list. It's a fine language, but the toolchain stays with the runtimes the template already provides
and the languages the team prefers: new tooling isn't written in Python, and a Python tool a repository already has is
ported to the first language that fits when it is next changed, or listed as a follow-up to port.

The rule covers tooling, not the product. A project's own code and its stack's toolchain, such as pytest in a Python
project, stay in the stack's language. A tool that is itself written in Python, such as Copier, which renders and
updates the template, or pre-commit, runs as it is, pinned like any other dependency.

## Options considered

- Python first, as many repositories do. Rejected: it adds a fourth runtime and a second family of package managers,
  and the team prefers the three languages above.
- PowerShell for everything, since every command already runs in it. Rejected: its package ecosystem is thin for work
  such as parsing YAML, rendering templates or drawing images, and the team ranks it last for tooling.
- Go first. Rejected: it adds a compile step and a toolchain that the template doesn't otherwise install, where Node.js
  is already selected.
- Node.js, then Go, then PowerShell 7 (chosen).

## Consequences

- An agent or a person who writes a script, a check or a CI helper picks its language from this list, and a plan says
  which one and why when it isn't Node.js.
- `/plan-template-update` searches a project's own tooling for a language this record doesn't list and adds a
  follow-up to port each such tool.
- The template's own repository follows it: its brand kit generator and its tests' YAML and Jinja checks run on
  Node.js; Copier and pre-commit, Python programs it runs, stay.
- A project whose first Node.js tool arrives turns on `uses_node` with `.template/Update-Project.ps1`, which adds
  `.node-version`, the nvs scripts and, on GitHub, `actions/setup-node` in CI.
