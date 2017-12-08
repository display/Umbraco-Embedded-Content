import angular from 'angular'

export default function EmbeddedContentPropertyDirective (fileManager) {
  return {
    restrict: 'E',
    template: '<umb-property property="property" ng-if="ready"><umb-property-editor model="editProperty"></umb-property-editor></umb-property>',
    scope: {
      setFiles: '&',
      property: '=',
      embeddedContentItem: '='
    },

    link (scope) {
      scope.editProperty = angular.copy(scope.property)
      const alias = scope.editProperty.alias
      const id = scope.editProperty.id

      // return the original alias and id when the property is serialized
      scope.editProperty.toJSON = function () { return Object.assign({}, this, { alias: alias, id: id }) }

      // make sure the property alias and id is unique
      scope.editProperty.alias = scope.editProperty.id = `item-${scope.embeddedContentItem.key}-${alias}`

      scope.ready = true
      scope.$on('filesSelected', (e, args) => {
        const filesArray = [].slice.call(args.files).map(file => Object.assign(file, { propertyAlias: scope.property.alias }))
        scope.property.selectedFiles = filesArray.map(file => file.name)
        fileManager.setFiles(scope.property.alias, [])
        scope.setFiles({ files: filesArray })
      })

      scope.$on('formSubmitting', () => {
        scope.property.value = scope.editProperty.value
      })

      scope.$watch('property.value', (newVal, oldVal) => {
        if (newVal !== oldVal) {
          scope.editProperty.value = newVal
        }
      })

      // scope.$on('ecValueSync', (e, { items }) => {
      //   if (items) {
      //     items.forEach(item => {
      //       if (item.key === scope.embeddedContentItem.key) {
      //         [].concat(...item.tabs.map(t => t.properties))
      //           .forEach(p => {
      //             const propertyAlias = `item-${item.key}-${p.alias}`
      //             if (propertyAlias === scope.property.alias) {
      //               scope.property.value = p.value
      //             }
      //           })
      //       }
      //     })
      //   }
      //   console.log(scope.editProperty.value)
      //   console.log(scope.property.value)
      // })
    }
  }
}

EmbeddedContentPropertyDirective.$inject = ['fileManager']
