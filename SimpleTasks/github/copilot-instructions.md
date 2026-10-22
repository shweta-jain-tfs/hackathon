# SimpleTasks review guidance

When reviewing pull requests, use the [code-review skill](skills/code-review/SKILL.md) and read its referenced Markdown checklists.

Review the changed code and relevant callers. Report actionable regressions with a changed file/line, severity, concrete failure scenario, and suggested fix. Verify suspected issues against current code and avoid speculative or repeated findings.

SimpleTasks is a .NET 10 Blazor Web App with Interactive Server rendering. TaskService is scoped to each Blazor circuit; in-memory tasks intentionally reset on a full refresh or new session. The starter intentionally has no database, authentication, external services, localization framework, or Pharos shared components. Do not flag these intentional choices as missing features unless the PR changes the requirements.

Prioritize task behavior, validation, state isolation, safe Razor rendering, accessibility, and responsive UI. Follow current repository conventions when imported standards or historical examples do not apply. State validation limitations; do not claim builds, tests, or Copilot skill attribution without evidence.
