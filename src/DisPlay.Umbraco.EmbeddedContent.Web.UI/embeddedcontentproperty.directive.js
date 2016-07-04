(function() {
'use strict';

function EmbeddedContentPropertyDirective(fileManager) {
  return {
    require: '^ngModel',
    scope: {
      setFiles: '&',
      embeddedContentItem: '='
    },
    link(scope) {

      let alias = scope.model.alias;
      let id = scope.model.id;

      // return the original alias and id when the property is serialized
      scope.model.toJSON = function() {
        return _.extend({}, this, { alias: alias, id: id });
      };

      // make sure the property alias and id is unique
      scope.model.alias = scope.model.id = `item-${scope.embeddedContentItem.key}-${alias}`;

      scope.$on('filesSelected', (e, args) => {
        let filesArray = [].slice.call(args.files).map(file => _.extend(file, { propertyAlias: scope.model.alias }));
        scope.model.selectedFiles =filesArray.map(file => file.name);
        fileManager.setFiles(scope.model.alias, []);
        scope.setFiles({ files: filesArray });
      });
    }
  };
}
angular.module('umbraco')
.directive('embeddedContentProperty', EmbeddedContentPropertyDirective);

})();
