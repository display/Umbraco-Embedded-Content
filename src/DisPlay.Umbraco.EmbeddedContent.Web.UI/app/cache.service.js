export default class CacheService {
  constructor (entityResource) {
    this.entityResource = entityResource

    this.cache = {}
    this.isLoading = false
  }

  getEntityById (id, type) {
    return this.getOrAdd('entity', id, () => this.entityResource.getById(id, type))
  }

  getOrAdd (type, key, valueFactory) {
    const cache = this.cache[type] || (this.cache[type] = [])
    const fromCache = cache.filter(item => item.key === key)

    if (fromCache.length === 1) {
      return fromCache[0].value
    }

    if (!this.isLoading) {
      this.isLoading = true

      valueFactory(key).then(result => {
        cache.push({ key: key, value: result })
        this.isLoading = false
      })
    }

    return null
  }

  static factory (entityResource) {
    return new CacheService(entityResource)
  }
}

CacheService.factory.$inject = ['entityResource']
