# Review report

Adapted from the `tetris-code-review` report contract for GitHub and standalone SimpleTasks reviews.

Lead with the number of unique supported findings, highest priority/finding ID, and material limitations. Briefly state repository, scope/base/head. Use **No supported findings** when appropriate, qualifying incomplete coverage.

Keep all four sections below in order in the overall review summary, even when a section has no findings. Start each with **Status: Completed**, **Status: Partial**, or **Status: Unavailable**, followed by a short assessment or limitation. Status describes coverage, not pass/fail. For assessed sections without findings, say **No supported findings**; for unavailable evidence, say **Not assessed** and why. An empty search is not proof of compliance.

## 1. Application practices

Assess relevant ownership/layering, callers, shared components, validation/state flow, and application patterns. Explain supported deviations and consequences. Similar implementations alone are not mandatory rules.

## 2. Coding practices

Report applicable naming, nullability/defaults, async/cancellation, cleanup, and standards concerns. Identify actual authority and applicability; distinguish formal standards, extracted notes, observed conventions, and preferences. Disclose when the full formal standard was unavailable.

## 3. Risk

Describe supported regressions with concrete trigger, execution path, user/system impact, and mitigation. Cross-reference an existing finding instead of repeating it. Put independently proven behavior defects here when they do not fit earlier sections. Separate unresolved validation gaps from proven defects. Identify a focused check without claiming it ran; do not invent risks to populate the section.

## 4. Past PR comments

For applicable original feedback, identify quotes or label faithful paraphrases and preserve original provenance: verified PR ID/link, historical file/line/date, and source-record location where available. A PR landing link is not an exact thread link; invent neither. Explain how the exact current change, current-project evidence, and original comment support the finding. Cross-reference earlier IDs rather than repeating defects.

The bundled `pr-comment-patterns.md` contains summaries without original records. With only this bundle, use **Status: Unavailable. Not assessed: original historical PR comments are not bundled.** Describe relevant summary patterns consulted separately; do not present them as quotes, verified comments, or a completed historical review. Current-code findings remain valid without historical support.

## Finding format

Use stable F1/F2 IDs and the priority rules in [review.md](review.md). Order by impact within each section. Explain each unique finding fully once, proportionate to complexity:

- **F1 - [P2] Concrete title**
- **Feedback and location:** current concern, linked changed file and narrow source location.
- **Why it matters:** triggering input/action, actual versus expected behavior, impact or applicable rule/consequence.
- **Evidence:** verified current implementation, caller, test, or standard.
- **Suggested change:** smallest practical correction and why it resolves the concern.

On GitHub, post each detailed finding inline on the changed code and reference its ID/link in the appropriate overall-summary section. Use repository-relative paths or verified GitHub links at the reviewed commit, never local Windows links. For a local report, use absolute local file links with rechecked current lines. Historical lines are provenance, not current locations. Suggestions do not authorize source edits.

## Coverage and checks

Finish the overall summary with this compact table, replacing alternatives with supported statuses:

| Evidence pass | Status | Evidence and limitations |
| --- | --- | --- |
| Standards/project patterns | Completed / Partial / Unavailable | Actual source checks, verified authority/patterns, extracted notes versus missing full standards |
| Historical PR feedback | Completed / Partial / Unavailable | Actual original-record retrieval/provenance and applicability checks; distinguish bundled summaries from original records |

State checks actually run and results, plus material checks not run and why. Keep source/build/test/runtime/browser evidence distinct. Source inspection is not runtime proof. Attribution confirms a skill influenced a comment; it does not establish every check or this report format was followed.
