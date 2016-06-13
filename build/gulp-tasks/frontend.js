'use strict';

import del from 'del';
import cssnext from 'postcss-cssnext';
import cssnano from 'cssnano';

export function stylesheet(gulp, $, config) {
  return function() {
    return gulp
    .src(config.files.src.stylesheet)
    .pipe($.plumber({ errorHandler: $.notify.onError() }))
    .pipe($.stylelint({
      reporters: [{
        formatter: 'string',
        console: true
      }]
    }))
    .pipe($.sourcemaps.init() )
    .pipe($.concat(config.files.dest.stylesheet))
    .pipe($.postcss([
      cssnext({ browsers: ['last 2 version'], warnForDuplicates: false }),
      cssnano()
    ]))
    .pipe($.sourcemaps.write('.'))
    .pipe(gulp.dest(config.dirs.dest.frontend));
  };
}

export function javascript(gulp, $, config) {
  return function() {
    return gulp
    .src(config.files.src.javascript)
    .pipe($.plumber({ errorHandler: $.notify.onError() }))
    .pipe($.jshint())
    .pipe($.jshint.reporter('default'))
    .pipe($.jshint.reporter('fail'))
    .pipe($.sourcemaps.init())
    .pipe($.babel({ presets: ['es2015'] }))
    .pipe($.ngAnnotate())
    .pipe($.concat(config.files.dest.javascript))
    .pipe($.uglify())
    .pipe($.sourcemaps.write('.'))
    .pipe(gulp.dest(config.dirs.dest.frontend));
  };
}

export function views(gulp, $, config) {
  return function() {
    return gulp
    .src(config.files.src.views)
    .pipe(gulp.dest(config.dirs.dest.frontend));
  };
}

export function assets(gulp, $, config) {
  return function() {
    return gulp
    .src(config.files.src.assets)
    .pipe(gulp.dest(config.dirs.dest.frontend));
  };
}

export function clean(gulp, $, config) {
  return function() {
    return del(config.dirs.dest.frontend);
  };
}
