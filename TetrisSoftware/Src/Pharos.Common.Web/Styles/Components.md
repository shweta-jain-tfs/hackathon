# Writing SCSS components

## TL;DR

- Atomic components.
- Use BEM.
- Flat structure (low specificity).
- Position and box styles first.

## Rules of thumb

- Keep components small.
- One component per file.
- Never reference a component in another component file.
- Most components should behave like a block (fill the width of its parent and take up its own height).
  - The exception is inline components.
- Don't set an outer margin for block components.
  - Let the parent element (layout or another component) set the margin or use a utility class in the markup where the component is used.
- Avoid fixed heights and widths.
  - Use gap/margin/padding where applicable.
- A component should not know anything about its parent or siblings.
- A SCSS component does not have to map 1:1 to a blazor component. One blazor component can be composed of several SCSS components.

### Example - combining SCSS components

```html
<div class="ph-dropdown">
  <button class="ph-icon-button">Select</button>
  <div class="ph-dropdown__panel">
   <ul class="ph-link-list">
      <li><a class="ph-link-list__link">link</a></li>
   </ul>
  </div>
</div>
```

Responsibility of each component in the example:

- dropdown: a wrapper with a panel that can be closed or expanded. The panel has a max height and scrolls. The content of the panel is unknown to this component.
- icon button: a styled button with an icon.
- link list: a vertical list of links with a specific style.

The dropdown doesn't need to know that a link list exists in this example.

## Syntax

- Use BEM (https://getbem.com).
- Don't use underscores in the component name. Use a single dash instead.

### Example - BEM syntax

```scss
.ph-my-component { // component (Block)
  display: flex;
 
  &:focus-within { // pseudo-class
    background: red;
  }

  &__button { // sub-component (Element)
    border: 1px solid black;
  }

  &__result { // sub-component
    color: black;
  }

  &--big { // variant, e.g. dark or small (Modifier)
    padding: 30px;
  }

  &--disabled { // state, e.g. open or closed
   background: gray;
   pointer-events: none;
  }
}
```

Tip: Copy and paste the SCSS style above into https://sass-lang.com/playground/ to see the resulting CSS.

### CSS rule order

1. Base style (the default styling)
1. Pseudo-classes (hover)
1. Sub-elements
1. Variants & states
1. Media queries last

### CSS property order

Use the following order for each CSS rule:

1. Positioning (position, z-index, top)
1. Display & box model (display, flex styling, height, width, margin, padding, border)
1. Other (text, color)

### General SCSS rules

- Avoid deep nesting. Split into separate SCSS components if the component becomes too big.
- Avoid selecting nested elements without using a child selector (>).
- Mobile first when using media queries.
- Try to keep the specificity of the rules low (https://specificity.keegan.st/).

## Tips

- Use gap instead of margin whenever possible.
- Use flex and grid for layout.

## Advanced

### CSS variables

Use `--ph-*` for global/shared design system variables.

Use `--ph-my-component-*` for component-scoped variables that may be set from outside the component (e.g. inline styles).

You may also use `--_*` for internal component variables. These must be initialized and should not be set from outside.

### Overriding styles in sub-components

- Use CSS variables in variants to avoid repeating class names when overriding specific styles.
- Use `$root: &` to avoid repeating the component name when targeting sub-components from a variant.

### Example – overriding sub-component style using CSS variables

```scss
.ph-component {
  --ph-component-color: red; // component-scoped variable
  // --_color: red;          // internal variable (alternative)

  &__sub-component {
    color: var(--ph-component-color);
  }

  &--blue {
    --ph-component-color: blue;
  }
}
```

### Example - overriding sub-component nested in variant

```scss
.ph-component {
  $root: &; // ".ph-component"

  &__sub-component {
    display: none;
  }

  &--open {
    #{$root}__sub-component {
      display: block;
    }
  }
}
```