---
name: code-review
description: Review SimpleTasks pull requests and code changes for behavioral regressions, security, accessibility, and applicable C# standards using bundled standards notes and historical review patterns.
---

# SimpleTasks code review

Adapted from the existing local code-review skill. All review references are bundled here; no local archive, database, workstation path, or extra skill is required.

Before reviewing, read:
- [Coding standards notes](references/software-coding-standards-205463-notes.md).
- [Historical PR review patterns](references/pr-comment-patterns.md).
- [SimpleTasks security checklist](references/security-checklist.md).

The standards notes and historical patterns originate from Thermo IDS/Pharos review guidance. They are review candidates, not proof that SimpleTasks has adopted every rule. Apply current repository requirements and conventions first. Report a standards violation only when applicability is established; do not require Pharos components, BEM, localization infrastructure, MVVM, private-field renaming, or broad formatting changes merely because an imported example uses them.

## Review workflow

1. Establish the PR diff and read the current README and applicable repository instructions. Inspect the complete selected diff, then read affected components, services, models, configuration, and relevant callers.
2. Review correctness against current project behavior before consulting historical patterns for additional concerns. Trace event handlers to service operations and rendered state; check null/default handling, title validation, identity-based toggle/delete operations, filtering, and counts where affected.
3. Treat historical patterns as hypotheses. Check whether the changed code actually creates the failure, inspect guards and intentional exceptions, and discard unsupported concerns. Cite a historical pattern only when it materially supports a finding; never imply that a bundled summary is a verified original PR comment.
4. Check relevant security and UI behavior. Preserve the documented circuit-scoped, session-only model unless requirements change. Request tests or manual checks only when they would expose a concrete regression.
5. Keep the review read-only unless implementation is explicitly requested. Never modify unrelated changes as part of a review.

## Review priorities

Order findings by impact: correctness and security; state/ownership boundaries and nullability; accessibility and user-visible behavior; performance; applicable standards and maintainability. Avoid style-only comments unless they violate an explicit project requirement or hide a concrete risk.

## Output

Report actionable findings first, highest severity first. Each finding should include:
- A concise title and severity (High, Medium, or Low).
- A changed file and narrow line location.
- The triggering input or action, actual behavior, and expected behavior grounded in project evidence.
- A practical suggested fix and any material assumptions.

Keep each concern in one comment. If no supported findings remain, say so. State what validation was performed and any relevant gaps; distinguish source reasoning, build/test results, and browser verification. Do not claim that a skill influenced a Copilot review unless review attribution or session evidence confirms it.
