# SimpleTasks security checklist

Apply checks to the changed behavior and its reachable callers. Findings need a concrete failure path in current code.

- **Input validation:** Keep service-side validation of null, empty, whitespace-only, and overlong titles; client maxlength alone is insufficient. Check trimming and the shared maximum length when validation changes.
- **Rendering:** Keep task titles encoded by Razor. Investigate new MarkupString, raw HTML, JavaScript interpolation, or DOM insertion paths that could execute user-supplied content.
- **State isolation:** Review changes to TaskService registration and storage lifetime. A shared singleton or static mutable store could expose one circuit's tasks to others. In-memory reset on refresh is intentional.
- **Request protection:** If request handling changes, check that antiforgery protection remains appropriate for affected forms/endpoints and that production exception details are not exposed. Preserve appropriate HTTPS configuration.
- **Secrets and logging:** Flag committed credentials or newly logged sensitive data. Do not reproduce secret values in review comments.
- **New persistence or APIs:** If introduced, check access to task identifiers, authorization boundaries, injection-safe queries, and error handling against the new requirements. Do not demand authentication or database controls for the current session-only starter without a relevant change.
- **Dependencies:** Assess changed dependencies using concrete advisory and resolved-version evidence when available. Do not invent vulnerability findings from package names or unverified version assumptions.
