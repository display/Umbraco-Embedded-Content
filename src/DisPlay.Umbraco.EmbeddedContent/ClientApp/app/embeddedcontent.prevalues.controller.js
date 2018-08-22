import settingsOverlay from './overlays/settings.html'
import groupsPropertyEditor from './groups.html'
import './embeddedcontent.prevalues.html'

// TODO: Needs optimisation

export default class EmbeddedContentPrevaluesController {
  constructor ($scope, $timeout, $interpolate, angularHelper, localizationService, contentTypeResource) {
    this.$scope = $scope
    this.$interpolate = $interpolate
    this.localizationService = localizationService
    this.contentTypeResource = contentTypeResource

    this.currentForm = angularHelper.getCurrentForm($scope)

    if (!$scope.model.value) {
      $scope.model.value = {
        enableCollapsing: '1',
        allowEditingName: '0',
        documentTypes: []
      }
    }

    contentTypeResource.getAll()
      .then(data => {
        this.documentTypes = data
        this.$scope.model.value.documentTypes = this.$scope.model.value.documentTypes.map(this.init.bind(this)).filter(item => item)
        this.ready = true
      })
  }

  hasSettings () {
    return this.$scope.model.value.minItems ||
      this.$scope.model.value.maxItems ||
      this.$scope.model.value.enableCollapsing !== '1'
  }

  init (item) {
    const documentType = this.documentTypes.find(docType => docType.alias === item.documentTypeAlias)
    if (!documentType) {
      return
    }

    return {
      documentTypeId: documentType.id,
      documentTypeAlias: documentType.alias,
      name: documentType.name,
      icon: documentType.icon,
      nameTemplate: item.nameTemplate,
      allowEditingName: item.allowEditingName,
      maxInstances: item.maxInstances,
      group: item.group,
      settingsTab: item.settingsTab
    }
  }

  activate (item) {
    if (item.tabs) {
      item.active = true
      item.loaded = true
      return
    }
    this.contentTypeResource.getById(item.documentTypeId)
      .then((response) => {
        item.tabs = response.groups.map((g) => {
          return {
            id: g.id,
            name: g.name
          }
        })
        item.active = true
        item.loaded = true
      })
  }

  deactivate (item) { item.active = false }

  toggle (item) {
    if (this.deletePromptChanged) {
      delete this.deletePromptChanged
      return
    }
    if (item.active) {
      this.deactivate(item)
    } else {
      this.activate(item)
    }
  }

  add (item) {
    const docType = this.init({
      documentTypeAlias: item.alias
    })

    this.$scope.model.value.documentTypes.push(docType)
    this.$scope.model.value.documentTypes.sort((a, b) => a.name.localeCompare(b.name))

    this.activate(docType)

    this.currentForm.$setDirty()
  }

  remove (index) {
    this.$scope.model.value.documentTypes.splice(index, 1)
    this.currentForm.$setDirty()
  }

  togglePrompt (item, event) {
    event.stopPropagation()
    item.deletePrompt = !item.deletePrompt
  }

  hidePrompt (item, event) {
    item.deletePrompt = false

    // Hack: umb-confirm-action => on-cancel does not expose $event, se we set deletePropmtChanged to true so we can stop the item toggle
    this.deletePromptChanged = true
  }

  editSettings (event) {
    const properties = [{
      label: 'Minimum number of items',
      alias: 'minItems',
      view: 'integer',
      value: this.$scope.model.value.minItems
    }, {
      label: 'Maximum number of items',
      alias: 'maxItems',
      view: 'integer',
      value: this.$scope.model.value.maxItems
    }, {
      label: 'Enable collapsing',
      alias: 'enableCollapsing',
      view: 'boolean',
      value: this.$scope.model.value.enableCollapsing
    }, {
      label: 'Enable filtering',
      alias: 'enableFiltering',
      view: 'boolean',
      value: this.$scope.model.value.enableFiltering
    }, {
      label: 'Groups',
      alias: 'groups',
      view: groupsPropertyEditor, // '/App_Plugins/EmbeddedContent/groups.html',
      value: this.$scope.model.value.groups,
      config: {
        max: 0
      }
    }]

    this.editSettingsOverlay = {
      view: settingsOverlay, // '/App_Plugins/EmbeddedContent/embeddedcontent.settings.overlay.html',
      title: this.localizationService.localize('embeddedContent_settings'),
      settings: properties,
      event: event,
      show: true,
      submit: (model) => {
        model.settings.forEach(property => {
          this.$scope.model.value[property.alias] = property.value
        })

        this.currentForm.$setDirty()

        this.editSettingsOverlay.show = false
        this.editSettingsOverlay = null
      },
      close: () => {
        this.editSettingsOverlay.show = false
        this.editSettingsOverlay = null
      }
    }
  }

  openContentTypeOverlay (event) {
    const availableItems = this.documentTypes
      .filter(docType => !this.$scope.model.value.documentTypes.find(item => item.documentTypeAlias === docType.alias))
      .map(docType => {
        return {
          alias: docType.alias,
          name: docType.name,
          description: docType.description,
          icon: docType.icon
        }
      })

    this.contentTypeOverlay = {
      view: 'itempicker',
      filter: true,
      title: this.localizationService.localize('embeddedContent_addDocumentType'),
      availableItems: availableItems,
      event: event,
      show: true,
      submit: (model) => {
        this.add(model.selectedItem)
        this.contentTypeOverlay.show = false
        this.contentTypeOverlay = null
      }
    }
  }

  static $inject = ['$scope', '$timeout', '$interpolate', 'angularHelper', 'localizationService', 'contentTypeResource']
}
