'use strict';

import del from 'del';
import gulp from 'gulp';
import gulpLoadPlugins from 'gulp-load-plugins';
import {argv} from 'yargs';

import * as dotnet from './build/gulp-tasks/dotnet';
import * as frontend from './build/gulp-tasks/frontend';
import * as iisexpress from './build/gulp-tasks/iisexpress'
import * as release from './build/gulp-tasks/release';
import * as nuget from './build/gulp-tasks/nuget';
import * as umbraco from './build/gulp-tasks/umbraco';

import pkg from './package.json';

const $ = gulpLoadPlugins();
const srcDir = 'src';
const destDir = argv.target || 'dist';

const config = {
  // Only use the last part of the plugin name as the folder name.
  // e.g. DisPlay.Umbraco.EmbeddedContent becomes EmbeddedContent
  cleanedName: pkg.name.replace(/(?:.*)+\.(.*)/, '$1'),
  version: pkg.version,
  debug: argv.debug || false,
  port: 8080
}

config.dirs = {
  src: {
    path: srcDir,
    frontend: `${srcDir}/${pkg.name}.Web.UI`,
    dotnet: `${srcDir}/${pkg.name}`
  },
  dest: {
    path: destDir,
    frontend: `${destDir}/App_Plugins/${config.cleanedName}`,
    dotnet: `${destDir}/bin`,
    package: 'dist/pkg',
  },
  build: 'build',
  tools: 'tools',
  dist: 'dist'
};

config.files = {
  nuspec: {
    web: `${config.dirs.build}/nuspec/${pkg.name}.nuspec`
  },
  src: {
    solution: `${pkg.name}.sln`,
    dotnet: `${config.dirs.src.dotnet}/**/*.cs`,
    assemblyinfo: `${config.dirs.src.dotnet}/**/AssemblyInfo.cs`,
    dll: `${config.dirs.src.dotnet}/bin/Release/${pkg.name}.{dll,pdb,xml}`,
    stylesheet: `${config.dirs.src.frontend}/**/*.css`,
    javascript: `${config.dirs.src.frontend}/**/*.js`,
    views: `${config.dirs.src.frontend}/**/*.html`,
    assets: [`${config.dirs.src.frontend}/**/*`, `!${config.dirs.src.frontend}/**/*.{js,css,html}`]
  },
  dest: {
    stylesheet: `${config.cleanedName}.min.css`,
    javascript: `${config.cleanedName}.min.js`
  }
};

// nuget tasks
gulp.task('nuget-download', nuget.download(gulp, $, config));
gulp.task('nuget-package', gulp.series('nuget-download', nuget.pack(gulp, $, config)));
gulp.task('nuget-restore', gulp.series('nuget-download', nuget.restore(gulp, $, config)));

// dotnet tasks
gulp.task('dotnet-assemblyinfo', dotnet.assemblyinfo(gulp, $, config));
gulp.task('dotnet-copy', dotnet.copy(gulp, $, config));
gulp.task('dotnet-clean', dotnet.clean(gulp, $, config));
gulp.task('dotnet-build', gulp.series('nuget-restore', dotnet.build(gulp, $, config), 'dotnet-copy'));
gulp.task('dotnet-watch', (done) => {
//  gulp.watch(config.files.src.dotnet, gulp.series('dotnet-build'));
  done();
});

// frontend tasks
gulp.task('frontend-stylesheet', frontend.stylesheet(gulp, $, config));
gulp.task('frontend-javascript', frontend.javascript(gulp, $, config));
gulp.task('frontend-views', frontend.views(gulp, $, config));
gulp.task('frontend-assets', frontend.assets(gulp, $, config));
gulp.task('frontend-clean', frontend.clean(gulp, $, config));
gulp.task('frontend-build', gulp.parallel('frontend-javascript', 'frontend-stylesheet', 'frontend-views', 'frontend-assets'));
gulp.task('frontend-watch', () => {
  gulp.watch(config.files.src.javascript, gulp.series('frontend-javascript'));
  gulp.watch(config.files.src.stylesheet, gulp.series('frontend-stylesheet'));
  gulp.watch(config.files.src.views, gulp.series('frontend-views'));
  gulp.watch(config.files.src.assets, gulp.series('frontend-assets'));
});

// release-clean tasks
gulp.task('release-clean', release.clean(gulp, $, config));

// umbraco tasks
gulp.task('umbraco-package', umbraco.pack(gulp, $, config));

// serve tasks
gulp.task('serve', iisexpress.run(gulp, $, config));

// general tasks
gulp.task('clean', gulp.parallel('dotnet-clean', 'frontend-clean', 'release-clean', (done) => del(config.dirs.dist, done)));

gulp.task('watch', gulp.parallel('dotnet-watch', 'frontend-watch'));
gulp.task('build', gulp.series('clean', gulp.parallel('dotnet-build', 'frontend-build')));
gulp.task('package', gulp.series('clean', 'dotnet-assemblyinfo', gulp.parallel('dotnet-build', 'frontend-build'), gulp.parallel('nuget-package', 'umbraco-package')));
gulp.task('default', gulp.series('build', gulp.parallel('watch', 'serve')));

// release tasks
gulp.task('release', gulp.series('release-clean', release.release(gulp, $, config), 'package'));
