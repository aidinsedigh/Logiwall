# Logiwall API

This is the project-level instruction entry point for Claude Code.

Read and follow:

@instruction.md

## Mandatory project rules

Before implementing code:

1. Check the available skills.
2. Use `codebase-memory-mcp` for project/code search.
3. Follow the relevant skill when one exists (for example the `dotnet-*` plugin skills).
4. For complicated tasks, write a plan before implementation (the `spec-driven` skill), and
   keep it in `docs/work/` (see below).
5. Follow the architecture, testing, logging, exception, security, and Git conventions in
   `instruction.md`. For topics §6-§14 cover, `instruction.md`'s heading points at the detail in
   `docs/conventions/`; load only the file the task actually touches.
6. Explain every output to the user by `instruction.md` §20: Simplified Technical English always,
   a diagram when the answer has a structure, an HTML page when it is too large for the terminal.

Do not blindly modify the codebase without first understanding the existing implementation and conventions.

## Feature/cycle docs

`docs/` follows the structure and workflow in `docs/README.md` — read that file before creating
or archiving any spec, plan or ADR. In short:

* `docs/work/NNN-name/` — the **active** cycle's plan files and its `tasks.md`. Check
  `tasks.md` first when resuming a cycle; it is the small file.
* `docs/adr/` — durable, cross-cycle architectural decisions. Check these before touching
  anything they govern (project layers, error shape, currency units, ...).
* `docs/archive/cycles/NNN-name/` — finished cycles' frozen plans, each with a line-numbered
  `## Contents` TOC. Read these by line range, never in full.
* `docs/conventions/*.md` — the per-topic rulebook detail that `instruction.md`'s §6-§14
  headings point at (EF Core, API design, errors, auth/security, logging, async/DI, testing/QA,
  .NET/deploy, Git/code quality, explaining output). Load on demand, not all at once.
* When a cycle finishes, move its `docs/work/` folder into `docs/archive/cycles/`, add the TOC,
  and promote any durable decision made along the way into its own `docs/adr/` file.
