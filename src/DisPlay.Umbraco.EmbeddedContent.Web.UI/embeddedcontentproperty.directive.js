(function() {
'use strict';

function EmbeddedContentPropertyDirective(fileManager) {
  return {
    restrict: 'E',
    template: '<umb-property property="property" ng-if="ready"><umb-property-editor model="property"></umb-property-editor></umb-property>',
    scope: {
      setFiles: '&',
      property: '=',
      embeddedContentItem: '='
    },
    link(scope) {

      let alias = scope.property.alias;
      let id = scope.property.id;

      // return the original alias and id when the property is serialized
      scope.property.toJSON = function() {
        return _.extend({}, this, { alias: alias, id: id });
      };

      // make sure the property alias and id is unique
      scope.property.alias = scope.property.id = `item-${scope.embeddedContentItem.key}-${alias}`;

      scope.ready = true;
      scope.$on('filesSelected', (e, args) => {
        let filesArray = [].slice.call(args.files).map(file => _.extend(file, { propertyAlias: scope.property.alias }));
        scope.property.selectedFiles = filesArray.map(file => file.name);
        fileManager.setFiles(scope.property.alias, []);
        scope.setFiles({ files: filesArray });
      });
    }
  };
}
angular.module('umbraco')
.directive('embeddedContentProperty', EmbeddedContentPropertyDirective);

})();
