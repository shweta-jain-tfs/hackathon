# UI Component Testing Guidelines

## TL;DR

- **Test sparingly** - Only behaviors that are easy to break and hard to catch manually
- **Test behavior, not implementation** - No CSS classes or complex markup selectors
- **Tests must survive refactoring** - Component markup changes, key behaviors don't
- **bUnit ≠ Browser** - We write components for browsers, but can't test browser-specific behavior
- **When in doubt, delete it** - If manual testing catches it immediately, don't write the test

---

## What to Test ✅

- **Complex data transformations** - Number parsing, decimal handling, culture formatting, null vs zero
- **Data integrity** - Whitespace trimming, type constraints, input validation
- **Public APIs on complex components** - `ClearSelectedItem()`, methods other code depends on
- **Smoke tests for major components** - Grid renders data, selection works, doesn't crash

## What NOT to Test ❌

- **Framework behavior** - Two-way binding, parameter passing
- **Visual details** - CSS classes, layout, divs
- **The obvious** - Components render, buttons exist, text displays

## bUnit Limitations

bUnit **does not run in a browser**. You cannot rely on:
- `onchange` auto-firing on blur (must explicitly call `input.Change()` after `input.Input()`)
- Browser-enforced input restrictions (`maxlength`, `min`, `max`, etc.)

**Your tests must explicitly trigger events that browsers fire automatically.**

## Test Rules

**Single component scope** - Test one component, mock dependencies  
**Simulate user actions** - Type, click, select (not: set properties directly)  
**Markup resilience** - `cut.Find("input")` ✅ | `cut.Find("div.wrapper > div > label")` ❌

## When to Add/Delete Tests

**Add tests for:**
- Complex behavior that's easy to break
- Major components prone to silent breakage
- Non-obvious behavior worth documenting

**Delete tests that:**
- Just render simple markup
- Test things manual testing reveals immediately
- Test framework behavior, not component logic
- Break every refactoring

## Examples

**PhNumberFieldTests** - Complex parsing logic, culture handling, null/zero distinction  
**PhDataGridTests** - Renders data, selection works, `ClearSelectedItem()` API
