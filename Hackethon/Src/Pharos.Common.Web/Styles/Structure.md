# SCSS file and folder structure
## Naming
All `scss` files except `main.scss` should start with an underscore to indicate that it's a partial file.

Use `@use` instead of `@import` (deprecated).

## Folders

All folders except `abstracts` have an `_index.scss` file used for including all styling from that folder into the main stylesheet.

When adding new `.scss` files, include them with `@use` in the respective index file.

### `abstracts/`
For Sass-variables and mixins.

Files in this folder should **not** emit any CSS rules (including animation frames and `:root` styles).

Use variables from the files in this folder by including them in your component with `@use`.

### `base/`
Contains global styles, font faces, normalize, preflight and blazor specific styles. Keep minimal.

### `components/`
One file per SCSS component. The file name is based on the CSS class name.

The file name should be the same as the class name, e.g. `.ph-my-component` -> `_ph-my-component.scss`.

Only use sub-folders if absolutely necessary.

Include variables and mixins from `abstracts` by using `@use "../abstracts/typography";` for the files you need. The default namespace is the filename without underscore.

### `layout/`
SCSS components that only handle the placement and size of other components/content. A layout doesn't have any colors. When in doubt, create an SCSS component in the `components` folder instead.

### `themes/`
For overriding CSS variables depending on a theme marker (class or attribute), e.g. instrument, light theme, chinese (different font family).

### `utilities/`
Utility classes that can be used instead of an SCSS component for generic uses, e.g. margin, gap, display and font size.

Example:
```html
<div class="flex gap-xl mt-lg"><h1 class="t-h1">Heading</h1></div>
```


