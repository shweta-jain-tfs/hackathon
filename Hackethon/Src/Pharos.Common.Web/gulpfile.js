/// <binding ProjectOpened='watch' />

// Thin wrapper around npm scripts for use with Visual Studio Task Runner Explorer.
// Build and watch logic should live in package.json, not here.

const { task } = require('gulp');
const { spawn } = require('child_process');

function npm(script) {
    return spawn(`npm run ${script}`, {
        stdio: 'inherit',
        shell: true
    });
}

task('build', () => npm('build-css'));
task('build-unminified', () => npm('build-css-unminified'));
task('watch', () => npm('watch'));
task('watch-unminified', () => npm('watch-unminified'));