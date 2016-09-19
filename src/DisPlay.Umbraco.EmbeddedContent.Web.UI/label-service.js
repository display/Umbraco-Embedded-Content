(function() {
'use strict';

let converters = {
  'Umbraco.MultipleMediaPicker': (cacheService, property) => {
    const ids = property.value.split(',');

    return ids.map(id => cacheService.getEntityById(+id, 'Media'))
    .filter(entity => entity)
    .map(entity => entity.name)
    .join(', ');
  },
  'Umbraco.MultiNodeTreePicker': (cacheService, property) => {
    const ids = property.value.split(',');
    let type = 'Document';

    switch(property.config.startNode.type) {
      case 'media':
        type = 'Media';
        break;
      case 'member':
        type = 'Member';
        break;
    }

    return ids.map(id => cacheService.getEntityById(+id, type))
    .filter(entity => entity)
    .map(entity => entity.name)
    .join(', ');
  },
  'Umbraco.TinyMCEv3': (CacheService, property) => {
    return property.value ? String(property.value).replace(/<[^>]+>/gm, '') : '';
  },
  'RJP.MultiUrlPicker': (CacheService, property) => {
    return property.value ? property.value.map(link => link.name).join(', ') : '';
  }
};

class LabelService {
  constructor(cacheService) {
    this.cacheService = cacheService;
  }

  getPropertyLabel(property) {
    const converter = converters[property.propertyEditorAlias];

    if(converter) {
      return converter(this.cacheService, property);
    }

    return property.value;
  }

  static factory(cacheService) {
    return new LabelService(cacheService);
  }
}

LabelService.factory.$inject = ['DisPlay.Umbraco.EmbeddedContent.CacheService'];

angular.module('umbraco')
.factory('DisPlay.Umbraco.EmbeddedContent.LabelService', LabelService.factory);

})();
