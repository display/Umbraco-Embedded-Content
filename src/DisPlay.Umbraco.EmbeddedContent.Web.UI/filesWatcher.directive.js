(function() {
'use strict';

function EmbeddedContentFilesWatcherDirective(fileManager) {
  return {
    require: '^ngModel',
    scope: {
      setFiles: '&'
    },
    link(scope) {
      scope.$on('filesSelected', (e, args) => {
        let filesArray = [].slice.call(args.files).map(file => Object.assign(file, { propertyAlias: scope.model.alias }));
        scope.model.selectedFiles =filesArray.map(file => file.name);
        fileManager.setFiles(scope.model.alias, []);
        scope.setFiles({ files: filesArray });
      });
    }
  };
}
angular.module('umbraco')
.directive('embeddedContentFilesWatcher', EmbeddedContentFilesWatcherDirective);

})();
