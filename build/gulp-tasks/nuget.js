'use strict';

import fs from 'fs-promise';
import request from 'request'
import del from 'del';
import nugetRunner from 'nuget-runner';

const nuget = nugetRunner();

function nugetPack(nuspec, config) {
  return nuget.pack({
    spec: nuspec,
    outputDirectory: `${config.dirs.dest.package}`,
    version: config.version.replace(/([0-9]+\.[0-9]+\.[0-9])(?:\-(alpha|beta)\.([0-9]+))/, '$1-$2$3'),
    nugetPath: `${config.dirs.tools}/nuget.exe`
  });
}

export function restore(gulp, $, config) {
  return function() {
    return nuget.restore({
      packages: config.files.src.solution,
      nugetPath: `${config.dirs.tools}/nuget.exe`
    });
  };
}

export function pack(gulp, $, config) {
  return function() {
    return fs.mkdirs(config.dirs.dest.package)
    .then(() =>
      Promise.all([
        Object.keys(config.files.nuspec)
          .map(nuspec => nugetPack(config.files.nuspec[nuspec], config))
      ])
    );
  };
}

export function download(gulp, $, config) {
  return function(done) {
    fs.stat(`${config.dirs.tools}/nuget.exe`)
    .then(() => done())
    .catch(() => {
      if (!fs.existsSync(config.dirs.tools)) {
        fs.mkdirSync(config.dirs.tools);
      };
      let stream = fs.createWriteStream(`${config.dirs.tools}/nuget.exe`);
      stream.on('close', done);

      request.get('https://nuget.org/nuget.exe')
      .pipe(stream);
    });
  };
}
