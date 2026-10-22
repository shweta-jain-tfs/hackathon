# Software Coding Standards Notes (Doc 205463, Ver 7.0)

Source extracted from: `205463 - Software Coding Standards.pdf` (56 pages).

## Core Fundamentals

- KISS: avoid unnecessary complexity.
- YAGNI: avoid speculative future-proofing.
- DRY: avoid repeated logic unless justified tradeoff.
- SOLID: single responsibility, interface segregation, dependency inversion, etc.
- Minimize side effects.
- Least surprise: follow established conventions.
- Comment sensibly: comment non-obvious behavior, avoid trivial commentary.
- Code/comments in English.

## C# Programming Rules 2021 (Section 15)

- Applies to projects started in 2021 or later.
- Rules should be documented and configured in IDE/editorconfig where possible.
- File names: PascalCase and same as class name.
- File header: optional.
- File structure: one class/interface implementation per file.
- Class member order:
  1. Private constants
  2. Public constants
  3. Private read-only variables
  4. Private variables
  5. Constructors
  6. Public properties
  7. Protected properties
  8. Public methods
  9. Protected methods
  10. Events
  11. Private methods
- Max one blank line.
- 4-space indentation.
- Braces always required, even one-line blocks.
- Braces on new line, except property get/set.
- No partial classes (except generated/tool code).
- Visibility rule: prefer private, then protected, then public.
- Do not use virtual unless necessary and overridden.
- Naming:
  - Public members: PascalCase
  - Private members: _camelCase
  - Parameters: camelCase
  - Interfaces start with I
  - Const uses normal member naming (no special const style)
- Always use var when possible.
- Avoid this keyword.
- Prefix rules: no prefix.

## Review Use

When reviewing C# code, treat this file as the standards baseline.
If a repo-local convention conflicts, call out the conflict explicitly in the finding.
