'use strict';

import path from 'path';
import fs from 'fs-promise';
import buffer from 'buffer';
import archiver from 'archiver';
import uuid from 'node-uuid';
import xml from 'js2xmlparser';

function getFilesRecursive(directory) {
  return new Promise((resolve, reject) => {
    let result = [];
    let checked = 0;
    fs.readdir(directory)
    .then(names => {
      names.forEach(name => {
        let p = path.join(directory, name);

        fs.stat(p)
        .then(stats => {
          if(stats.isFile()) {
            checked++;
            result.push({ dir: directory, name: name });
            if(checked === names.length) {
              resolve(result);
            }
          } else if(stats.isDirectory()) {
            getFilesRecursive(p)
            .then(res => {
              result.push(...res);
              checked++;
              if(checked === names.length) {
                resolve(result);
              }
            }).catch(reject);
          };
        }).catch(reject);
      });
    }).catch(reject);
  });
}

export function pack(gulp, $, config) {
  return function(done) {
    let pkg = require('../../package.json');

    let umbracoVersion = semver(pkg.umbracoPackage.umbracoVersion);

    let data = {
      info: {
        package: {
          name: pkg.umbracoPackage.name,
          version: pkg.version,
          license: {
            '@': { url: pkg.umbracoPackage.license },
            '#': pkg.umbracoPackage.licenseUrl
          },
          url: pkg.umbracoPackage.url,
          requirements: {
            major: umbracoVersion.major,
            minor: umbracoVersion.minor,
            patch: umbracoVersion.patch
          }
        },
        author: {
          name: pkg.umbracoPackage.author.name,
          website: pkg.umbracoPackage.author.website
        },
        readme: pkg.umbracoPackage.readme
      },
      DocumentTypes: [],
      Templates: [],
      Stylesheets: [],
      Macros: [],
      DictionaryItems: [],
      Languages: [],
      DataTypes: [],
      files: [],
    };

    return fs.mkdirs(config.dirs.dest.package)
    .then(() => {
      return Promise.all([
        getFilesRecursive(config.dirs.dest.frontend),
        getFilesRecursive(config.dirs.dest.dotnet)
      ])
    })
    .then(result => {
      let files = result[0].concat(result[1]);
      let archive = archiver.create('zip');
      let outFile = path.join(config.dirs.dest.package, `${pkg.name}.${pkg.version}.zip`);

      archive.on('error', done);
      archive.pipe(fs.createWriteStream(outFile));

      if(files.length > 0) {
        data.files.file = [];

        files.forEach(file => {
          let fileName = `${uuid.v4()}${path.extname(file.name)}`;

          archive.file(path.join(file.dir, file.name), { name: fileName });

          data.files.push({
            guid: fileName,
            orgPath: `/${path.relative(config.dirs.dest.path, file.dir).replace(/\\/g,'/')}`,
            orgName: file.name
          });
        });
      };

      archive.append(xml('umbPackage', data, {
        arrayMap: {
          files: 'file'
        }
      }),{ name: 'package.xml' });

      archive.finalize();
      done();
    });

  };
}
