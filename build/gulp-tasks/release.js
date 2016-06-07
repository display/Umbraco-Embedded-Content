'use strict';

import fs from 'fs-promise';
import path from 'path';
import del from 'del';
import inquirer from 'inquirer';
import semver from 'semver';

export function release(gulp, $, config) {

  const prompts = [{
    type: 'list',
    name: 'release',
    message: 'Increment version by',
    choices: [
      {name: `Major (${semver.inc(config.version, 'major')})`, value: 'major'},
      {name: `Pre major (${semver.inc(config.version, 'premajor')})`, value: 'premajor'},
      {name: `Minor (${semver.inc(config.version, 'minor')})`, value: 'minor'},
      {name: `Pre minor (${semver.inc(config.version, 'preminor')})`, value: 'preminor'},
      {name: `Patch (${semver.inc(config.version, 'patch')})`, value: 'patch'},
      {name: `Pre patch (${semver.inc(config.version, 'prepatch')})`, value: 'prepatch'},
      {name: `Pre release (${semver.inc(config.version, 'prerelease')})`, value: 'prerelease'},
      new inquirer.Separator(),
      {name: 'Manuel', value: 'manuel'},
      {name: 'Package only', value: 'package'},
      {name: 'Abort', value: 'abort'},
      new inquirer.Separator()
    ],
  }, {
    when: response => response.release === 'manuel',
    type: 'input',
    name: 'number',
    message: `Enter the version number`,
    default: semver.inc(config.version, 'minor'),
    validate: function (input) {
      if(semver.valid(input)) {
        return true;
      } else {
        return 'Please enter a valid version number';
      }
    }
  }, {
    when: response => response.release.indexOf('pre') === 0
      && !(response.release === 'prerelease' && semver.parse(config.version).prerelease.length),
    type: 'list',
    name: 'preid',
    message: 'Choose preid',
    choices: function(answers) {
      return [
        {name: `Alpha (${semver.inc(config.version, answers.release, 'alpha')})`, value: 'alpha'},
        {name: `Beta (${semver.inc(config.version, answers.release, 'beta')})`, value: 'beta'}
      ]
    }
  }];

  return function(done) {
    return inquirer.prompt(prompts)
      .then(answers => {
        let release = answers.release;
        if(release === 'abort'){
          done('User aborted...');
        }

        if(release !== 'package') {
          let pkg = require('../../package.json');
          if(answers.number) {
            pkg.version = config.version = answers.number;
          } else {
            pkg.version = config.version = semver.inc(config.version, release, answers.preid);
          }
          fs.writeFile('./package.json', JSON.stringify(pkg, null, 2)+'\n', done);
        }
      });
  }
}

export function clean(gulp, $, config) {
  return function() {
    return del(config.dirs.dest.package);
  };
}
