export const resolvers = {
  'Umbraco.MultipleMediaPicker': (property, cacheService) => {
    const ids = property.value.split(',')

    return ids.map(id => cacheService.getEntityById(+id, 'Media'))
      .filter(entity => entity)
      .map(entity => entity.name)
      .join(', ')
  },
  'Umbraco.MultiNodeTreePicker': (property, cacheService) => {
    const ids = property.value.split(',')
    let type = 'Document'

    switch (property.config.startNode.type) {
      case 'media':
        type = 'Media'
        break
      case 'member':
        type = 'Member'
        break
    }

    return ids.map(id => cacheService.getEntityById(+id, type))
      .filter(entity => entity)
      .map(entity => entity.name)
      .join(', ')
  },
  'Umbraco.TinyMCEv3': (property, cacheService) => String(property.value).replace(/<[^>]+>/gm, ''),
  'RJP.MultiUrlPicker': (property, cacheService) => property.value.map(link => link.name).join(', ')
}

export class LabelService {
  constructor (cacheService, resolvers) {
    this.cacheService = cacheService
    this.resolvers = resolvers
  }

  getPropertyLabel (property) {
    if (!property.value) {
      return ''
    }

    const resolver = this.resolvers[property.editor]

    if (resolver) {
      return resolver(property, this.cacheService)
    }

    return property.value
  }

  static factory (cacheService, resolvers) {
    return new LabelService(cacheService, resolvers)
  }
}

LabelService.factory.$inject = ['DisPlay.Umbraco.EmbeddedContent.CacheService', 'DisPlay.Umbraco.EmbeddedContent.LabelResolvers']
