# Full two-pass review

Adapted from `tetris-code-review/references/review.md` for SimpleTasks and GitHub review.

## Establish the change

- Resolve the repository and applicable instructions. For a PR, record base/head revisions and inspect the complete diff, including deletions, renames, and tests. For a branch, use its requested base and merge base. For local changes, include staged, unstaged, and relevant untracked source. Do not invent a diff when the base is ambiguous.
- Read the application's README (`SimpleTasks/README.md` in hackathon), affected components, services, models, configuration, tests, and callers. Analogous Tetris implementations do not determine current SimpleTasks behavior.

## Pass 1: source standards and project patterns

Read the [standards notes](software-coding-standards-205463-notes.md) and relevant [security checks](security-checklist.md). Search current source, scoped documentation, configuration such as .editorconfig/analyzers, and representative implementations.

- Establish authority and applicability before calling something a violation. Bundled notes are an extract, not the full Doc 205463. Disclose that distinction; do not claim complete formal-standard coverage. Similar code shows a pattern, not a mandatory rule.
- Derive concerns from changed responsibilities: ownership/layering, validation/state, identity, null/default handling, async/cancellation, event wiring/cleanup, shared components, accessibility, localization, and tests as applicable.
- Trace event handlers to service operations and rendered state. Check title validation, identity-based toggle/delete, filters, counts, Razor encoding, and circuit-scoped state when affected. Session-only reset and the absence of authentication/database/Pharos components are intentional unless requirements change.
- Inspect guards, callers, framework requirements, generated code, and intentional exceptions. Discard unsupported candidates. Retain concrete trigger, changed location, current evidence, impact, and smallest correction. Missing evidence is a coverage gap, not a finding.
- Establish Application practices, Coding practices, and Risk coverage before proceeding directly to pass 2. State progress when the review interface supports it; no approval pause is needed.

## Pass 2: historical PR feedback

Now read [historical PR patterns](pr-comment-patterns.md). These are summaries, not original PR-comment records. With this bundle alone, original historical-feedback coverage is **Unavailable**; say **Not assessed** for original comments while describing any summary patterns checked.

- If original records are already accessible in the authorized environment, search concise concerns from changed behavior and read complete matching records. Do not restrict historical analogues to the current PR ID.
- Preserve original wording or label a faithful paraphrase. Retain verified PR/file/line/date/source-record provenance where available; disclose missing fields and never invent links.
- Check the same trigger, responsibility, layer, and framework apply. Try to disprove each candidate against current callers, tests, guards, exceptions, and the intended fix. Discard stale, duplicate, unrelated, or pre-existing concerns not introduced or made reachable by the change.
- Findings attributed to original historical feedback need three anchors: the exact current change, verified current-project evidence, and the original comment. Summary patterns cannot replace the third anchor. Independently proven issues remain current-code findings even without an original analogue.

## Report

Use [report-format.md](report-format.md) for all four sections and distinct coverage statuses. Explain each unique finding once and cross-reference duplicates.

Use stable F1/F2 IDs and priorities: P0 immediate broadly blocking failure; P1 major defect; P2 ordinary defect or material applicable standards violation; P3 minor issue. Do not inflate preferences or invent findings to fill sections. If GitHub supplies High/Medium/Low UI severity, retain the P priority in the finding text without claiming the scales are identical.

Say **No supported findings** when none survive, qualifying incomplete coverage. Keep source reasoning, build/test evidence, and runtime/browser proof distinct. Do not claim a check ran without evidence.
