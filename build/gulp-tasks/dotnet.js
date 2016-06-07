'use strict';

import del from 'del';
import through from 'through';

function msbuild(gulp, $, config) {
  return gulp.src(config.solution)
  .pipe($.plumber({ errorHandler: $.notify.onError() }))
  .pipe($.msbuild({
    targets: config.targets,
    toolsVersion: 14,
    stdout: true,
    errorOnFail: true,
    verbosity: 'quiet',
    maxCpuCount: 4,
    properties: {
      Configuration: config.debug ? 'Debug' : 'Release',
      WarningLevel: 2,
      NoWarn: 1607
    }
  }));
}

export function build(gulp, $, config) {
  return function() {
    return msbuild(gulp, $, {
      debug: config.debug,
      solution: config.files.src.solution,
      targets: ['Build']
    });
  };
}

export function assemblyinfo(gulp, $, config) {
  return function() {
    return gulp.src(config.files.src.assemblyinfo)
    .pipe($.plumber({ errorHandler: $.notify.onError() }))
    .pipe($.dotnetAssemblyInfo({
      configuration: config.debug ? 'Debug' : 'Release',
      version: value => (config.version.indexOf('-') > -1 ? config.version.substring(0, config.version.indexOf('-')) : config.version) + '.*',
      informationalVersion: value => config.version,
    }))
    .pipe(gulp.dest(config.dirs.src.dotnet));
  };
}

export function copy(gulp, $, config) {
  return function() {
    return gulp
    .src(config.files.src.dll, { buffer: false })
    .pipe(gulp.dest(config.dirs.dest.dotnet));
  };
}

export function clean(gulp, $, config) {
  return function() {
    return Promise.all([
      del(config.dirs.dest.dotnet),
      msbuild(gulp, $, {
        debug: config.debug,
        solution: config.files.src.solution,
        targets: ['Clean']
      })
    ]);
  };
}
