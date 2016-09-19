(function() {
'use strict';

class CacheService {
  constructor(entityResource) {
    this.entityResource = entityResource;

    this.entityCache = [];
    this.isEntityLoading = false;
  }

  getEntityById(id, type) {
    const foundEntities = this.entityCache.filter(entity => entity.id === id);

    if(foundEntities.length === 1) {
      return foundEntities[0];
    }

    if(!this.isEntityLoading) {
      this.isEntityLoading = true;

      this.entityResource.getById(id, type).then(entity => {
        this.entityCache.push(entity);
        this.isEntityLoading = false;
      });
    }

    return null;
  }

  static factory(entityResource) {
    return new CacheService(entityResource);
  }
}

angular.module('umbraco')
.factory('DisPlay.Umbraco.EmbeddedContent.CacheService', CacheService.factory);

})();
