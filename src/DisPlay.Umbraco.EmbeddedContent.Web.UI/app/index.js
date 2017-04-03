import angular from 'angular'

import EmbdeddedContentController from './embeddedcontent.controller'
import EmbeddedContentPrevaluesController from './embeddedcontent.prevalues.controller'
import EmbeddedContentPropertyDirective from './embeddedcontentproperty.directive'
import CacheService from './cache.service'
import GroupsController from './groups.controller'
import {LabelService, resolvers as LabelResolvers} from './label.service'
import './main.css'

angular.module('umbraco')
.controller('DisPlay.Umbraco.EmbeddedContent.EmbdeddedContentController', EmbdeddedContentController)
.controller('DisPlay.Umbraco.EmbeddedContent.EmbeddedContentPrevaluesController', EmbeddedContentPrevaluesController)
.controller('DisPlay.Umbraco.EmbeddedContent.GroupsController', GroupsController)
.directive('embeddedContentProperty', EmbeddedContentPropertyDirective)
.value('DisPlay.Umbraco.EmbeddedContent.LabelResolvers', LabelResolvers)
.factory('DisPlay.Umbraco.EmbeddedContent.CacheService', CacheService.factory)
.factory('DisPlay.Umbraco.EmbeddedContent.LabelService', LabelService.factory)
.run(($injector) => {
  if (!$injector.has('formResource')) {
    return
  }

  const formResource = $injector.get('formResource')

  LabelResolvers['UmbracoForms.FormPicker'] = (property, cacheService) => {
    const fromCache = cacheService.getOrAdd('UmbracoForms.FormPicker', property.value, () => formResource.getByGuid(property.value))
    return fromCache ? fromCache.data.name : null
  }
})
