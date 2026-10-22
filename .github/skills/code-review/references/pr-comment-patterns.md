# PR Comment Patterns (Repo-Calibrated)

Source inputs:
- `README_CODE_REVIEW_AGENT.md`
- `review-comments-only.txt`

## Repeated Reviewer Expectations

- Remove unnecessary files and unrelated edits from PRs.
- Keep logic in the correct layer (UI vs VM, handler vs mapper, domain boundary).
- Reuse existing implementation patterns before introducing new ones.
- Simplify conditions and eliminate duplicate nullable/default handling.
- Localize all user-facing text and error messages.
- Follow design-system practices (BEM, shared utility/tokens, avoid broad shared-style edits).
- Prefer event-driven refresh/state updates over timer polling unless clearly justified.
- Ask for tests on behavior changes and edge cases.

## Typical Finding Shapes

- Scope/ownership: "Is this needed in this file/layer?"
- Correctness: "Condition/default handling seems wrong for this edge case."
- Localization: "This UI string/error should be localized."
- CSS/system: "This introduces cross-page style side effects."
- Performance/eventing: "Polling/timer work seems unnecessary for event-driven state."

## Suggested Comment Tone

- Be concrete and direct.
- Explain impact and likely regression mode.
- Suggest alignment with nearby existing patterns.
- Ask clarifying questions where intent is unclear.
- Avoid style-only nitpicks unless they hide real risk.

## Review Ordering

1. Correctness/regressions
2. Architecture/layering
3. Nullability/default/state safety
4. Localization/accessibility
5. CSS/design-system consistency
6. Performance/eventing
7. Minor maintainability issues
