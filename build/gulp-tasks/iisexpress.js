
import {spawn} from 'child_process';
import path from 'path';

export function run(gulp, $, config) {
  return function() {

    let iisexpress = spawn('iisexpress', [
      `/port:${config.port}`,
      `/clr:v4.0`,
      `/path:${path.resolve(config.dirs.dest.path)}`
    ], {
      cwd: path.join(process.env.PROGRAMFILES, 'IIS Express'),
      detached: true
    });

    iisexpress.stdout.on('data', (data) => {
      let text = data.toString('utf-8');
      if(text.indexOf('Request') === 0) {
        return;
      }
      $.util.log(text);
    });

    iisexpress.stderr.on('data', (data) => {
      throw data;
    });

    iisexpress.on('error', (data) => {
      throw data;
    });

    process.on('SIGINT', () => {
      iisexpress.kill();
    });

    return iisexpress;
  };
}
