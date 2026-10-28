# Docs map

This folder is split so that an agent (or a person) reads only what a task needs, never the
whole history. Before you `cat` a whole file under `docs/`, check the table below. Most files here are meant to be read by section or by
range, not in full.

| Folder | What it holds | When to read it | Size discipline |
|---|---|---|---|
| `adr/` | Durable, cross-cycle architectural decisions (why, not just what) | Before touching anything the decision governs | ~1 page each |
| `conventions/` | The rulebook detail for `instruction.md` §6-§14 and §20, one file per topic | Only when the task actually touches that topic — `instruction.md` tells you which file | Each ~50-200 lines; never all loaded at once |
| `work/` | The **active** cycle's plans and its `tasks.md` | Every session while a cycle is in progress | `tasks.md` ≤150 lines |
| `archive/cycles/NNN-name/` | Finished cycles' plans, frozen | Only when you need history for a *specific* decision or task — jump to the line the TOC gives you | No size limit; must have a `## Contents` TOC |

## Current Work

| Cycle | Area | Status | Folder |
|---|---|---|---|
| 001-016 | Audit remediation: one cycle for each audit plan (security, tests/CI, .NET 10, cleanup) | Done 2026-10-27 — archived; 012 Phase B BLOCKED | `archive/cycles/001-*` to `016-*` |
| 017 | Persian client messages, structured logs, open items from cycles 001-016 | Done 2026-10-28 — archived; results in its `tasks.md` | `archive/cycles/017-persian-messages-and-structured-logs/` |
| 018 | Fix the Blazor client's API calls; the client is the admin panel (password sign-in, `AdminsOnly`, global order stats) | In progress — done, not committed | `work/018-fix-blazor-api-calls/` |
| 019 | Adapt the API to the React app's contracts (E2E report 2026-10-28) | In progress — done, not committed | `work/019-adapt-api-to-front-contracts/` |

Cycles 001-016 come from one audit (2026-10-27). Each cycle folder has `spec.md`, `plan.md` and
`tasks.md`, like 017. `archive/audit-2026-10-27.md` keeps the notes for more than one cycle: the
order, the open operator actions and the open product decisions.

## ADR numbers

| ADR | Title | Status |
|---|---|---|
| 0001 | Currency units (rial/toman) | Open, verdict (c): provider confirmation required |
| 0002 | Keep the layered architecture | Accepted |
| 0003 | Client messages are Persian | Accepted |
| 0004 | Structured log format and correlation ID | Accepted |
| 0005 | The Blazor client is the admin panel | Accepted |

Give a new ADR the next free number.

## `instruction.md` vs `docs/conventions/`

`instruction.md` is loaded on **every** session (via `CLAUDE.md`'s `@instruction.md`). It keeps
only what every task needs: §1-§5 (project, commands, architecture, area folders, request path,
existing database) and §15-§20 (shared code, scope control, definition of done, agent behavior,
final principle, explaining output). §20 keeps its core rules in `instruction.md` because they
apply to every reply; its detail and examples are in `conventions/explaining-output.md`.

§6-§14 exist in `instruction.md` **as headings only**, so every `§N` reference still resolves.
Each heading points at the file that has the rules:

| Section | File |
|---|---|
| §6 | `conventions/ef-core.md` |
| §7 | `conventions/api-design.md` |
| §8 | `conventions/errors.md` |
| §9 | `conventions/auth-and-security.md` |
| §10 | `conventions/logging-and-observability.md` |
| §11 | `conventions/async-and-di.md` |
| §12 | `conventions/testing-and-qa.md` |
| §13 | `conventions/dotnet-and-deploy.md` |
| §14 | `conventions/git-and-code-quality.md` |
| §20 (detail) | `conventions/explaining-output.md` |

Load the specific file a task needs, not all ten. A change that never touches an endpoint has no
reason to load `api-design.md`.

## The workflow

1. **A new cycle starts** → create `docs/work/NNN-feature-name/` with `spec.md`, `plan.md`, and
   `tasks.md` (a checkbox list, one line per task, mirroring the plan's `### Task N` headings).
   A new session reads `tasks.md` first to know where the cycle stands, without opening `plan.md`.
2. **While it is in progress** → update `tasks.md` as each task's commit lands. If a decision made
   mid-cycle is durable (not specific to this feature), give it its own file in `adr/` and link it
   from the plan and `tasks.md`.
3. **When the cycle is done** (final verification checked, self-review written) → move the folder
   to `docs/archive/cycles/NNN-name/`. Keep `tasks.md`: it holds the cycle's
   status and results. Add the numbered TOC described below to `spec.md` and `plan.md`.

## The line-numbered TOC convention

Every archived (frozen) plan or spec starts with:

```markdown
# Title

## Contents

- L81 Some Heading
  - L86 A subheading
- L210 Another Heading
```

Archived files are never edited again, so these line numbers stay correct. Read the file by range
instead of in full:

```bash
sed -n '131,168p' docs/archive/cycles/012-verify-payments-server-side/plan.md
```

Generate a TOC only for a file you are about to archive:

```bash
H=$(grep -c -E '^#{2,3} ' file.md); shift=$((H+3))
grep -n -E '^#{2,3} ' file.md   # then add $shift to each line number once inserted
```

An in-progress `work/` doc does not need a TOC. Its line numbers change on every commit.

## Solution items

`Logiwall.sln` shows `docs/` under `.SolutionItems` → `docs`, with one solution folder for each
subfolder (`adr`, `conventions`, `work`, `archive` → `cycles` → one folder per cycle). When you add, move or delete a
file in `docs/`, make the same change in `Logiwall.sln`. When you start or archive a cycle, add or
move its solution folder.

## What does *not* belong here

- Anything `instruction.md` already states as a standing rule. That is the rulebook, not a
  decision record. An ADR explains *why* a specific, contestable choice was made.
- Anything specific to one feature that does not matter after its cycle is archived. That belongs
  in the cycle's own plan, not in `adr/`.
