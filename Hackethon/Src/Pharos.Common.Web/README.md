# Components project

## Prerequisites

Node.js is required. See [the readme in the solution folder for details](../../README.md).

## Building CSS

### Automatic build

NPM packages are installed and the CSS files are built automatically when building this project if the CSS is older than the SCSS files or if the Git branch was switched.
Also, Task Runner Explorer in Visual Studio has bindings for the `watch` task from `gulpfile.js`.

When `package.json` is updated (after `git pull`) the NPM packages will be updated when you build this project if you haven't updated them manually (below).

### Manual build

If you need to rebuild styles manually outside Visual Studio, do the following:
- Make sure that the path in your terminal window is this folder (`Src/Pharos.Common.Web`).
- Run `npm ci` if you haven't built this project.
- Run `npm run build` to build the CSS files.

### Watcher

Is you are not working from Visual Studio, and want CSS to auto-rebuild as you make changes
in the SCSS files you need to start the watcher manually.
- Make sure that the path in your terminal window is this folder (`Src/Pharos.Common.Web`).
- Run `npm run watch` to start the watcher.

## Working with SCSS

- Use VSCode with [SCSS IntelliSense](https://marketplace.visualstudio.com/items?itemName=mrmlnc.vscode-scss).
  - This is recommended because Visual Studio has poor support for Dart Sass which is the primary implementation of Sass.
- [SCSS folders and filenames (what goes where)](./Styles/Structure.md).
- [How to structure SCSS components (what a component is)](./Styles/Components.md).
