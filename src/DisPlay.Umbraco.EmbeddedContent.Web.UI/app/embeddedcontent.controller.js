import $ from 'jquery'

import contentTypePickerOverlay from './overlays/contenttypepicker.html'
import './embeddedcontent.html'

function endsWith (str, searchString) {
  const position = str.length - searchString.length
  const lastIndex = str.indexOf(searchString, position)
  return lastIndex !== -1 && lastIndex === position
}

function trimEnd (str, strToRemove) {
  if (endsWith(str, strToRemove)) {
    return str.substring(0, str.length - strToRemove.length)
  }
  return str
}

export default class EmbdeddedContentController {
  constructor ($scope, $timeout, $interpolate, $window, angularHelper, fileManager, editorState,
    localizationService, contentResource, contentTypeResource, serverValidationManager,
    embeddedContentLabelService) {
    this.$scope = $scope
    this.$timeout = $timeout
    this.$interpolate = $interpolate
    this.fileManager = fileManager
    this.editorState = editorState
    this.localizationService = localizationService
    this.contentResource = contentResource
    this.embeddedContentLabelService = embeddedContentLabelService

    if ($scope.preview) {
      this.label = 'Embedded content'
      this.contentReady = true
      return
    }

    this.currentForm = angularHelper.getCurrentForm($scope)
    const currentForm = this.currentForm

    let draggedRteSettings = {}
    this.sortableOptions = {
      distance: 10,
      cursor: 'move',
      placeholder: 'embedded-content__sortable-placeholder',
      handle: '.embedded-content__control-bar',
      helper: 'clone',
      forcePlaceholderSize: true,
      tolerance: 'pointer',
      zIndex: 999999999999999999,
      scrollSensitivity: 100,
      cursorAt: {
        top: 40,
        left: 60
      },
      sort: function (event, ui) {
        /* prevent vertical scroll out of the screen */
        const max = $('.embedded-content').width() - 150
        if (parseInt(ui.helper.css('left')) > max) {
          ui.helper.css({ 'left': max + 'px' })
        }
        if (parseInt(ui.helper.css('left')) < 20) {
          ui.helper.css({ 'left': 20 })
        }
      },
      start: function (e, ui) {
        // Fade out row when sorting
        ui.item.context.style.display = 'block'
        ui.item.context.style.opacity = '0.5'

        draggedRteSettings = {}
        ui.item.parents('.embedded-content').find('.umb-rte textarea').each(function () {
          // remove all RTEs in the dragged row and save their settings
          const id = $(this).attr('id')
          const editor = $window.tinyMCE.editors.find(editor => editor.id === id)
          if (editor) {
            draggedRteSettings[id] = editor.settings
            $window.tinyMCE.execCommand('mceRemoveEditor', false, id)
          }
        })
      },
      stop: function (e, ui) {
        // Fade in row when sorting stops
        ui.item.context.style.opacity = '1'

        // reset all RTEs affected by the dragging
        ui.item.parents('.embedded-content').find('.umb-rte textarea').each(function () {
          const id = $(this).attr('id')
          draggedRteSettings[id] = draggedRteSettings[id] || $window.tinyMCE.editors.find(editor => editor.id === id).settings
          $window.tinyMCE.execCommand('mceRemoveEditor', false, id)
          $window.tinyMCE.init(draggedRteSettings[id])
        })

        currentForm.$setDirty()
      }
    }

    this.localizationService.localize('embeddedContent_groupOther').then(label => {
      this.groupOther = label
    })

    this.hasSettings = false

    this.label = $scope.model.label
    this.description = $scope.model.description
    this.config = $scope.model.config.embeddedContentConfig

    if ($scope.model.value.length === 0 && this.config.minItems === 1 && this.allowedDocumentTypes.length === 1) {
      this.add(this.allowedDocumentTypes[0])
    }

    $scope.$watch('model.value', () => {
      this.$scope.model.value.forEach(this.init.bind(this))


      delete this.active
    })

    $scope.$on('formSubmitting', () => {
      $scope.$broadcast('ecSync', { scope: $scope })
      this.validate.bind(this)

      const active = this.$scope.model.value.find(x => x.active)
      if (active) {
        this.active = active.key
      }
    })

    $scope.$on('formSubmitted', () => {
      this.fileManager.setFiles(this.$scope.model.alias, [])
    })

    $scope.$on('valStatusChanged', (evt, args) => {
      // this is very ugly, but it works for now
      if (args.form.$invalid) {
        const errors = serverValidationManager.items.filter((error) => error.propertyAlias === this.$scope.model.alias)
        for (let i = 0; i < errors.length; i++) {
          const error = errors[i]
          const splits = error.fieldName.split('item-').filter(item => item)
          const id = error.fieldName.substr('item-'.length, 'f7ad912c-2907-4416-9b43-a38059953a80'.length)
          const item = this.$scope.model.value.find(item => item.key === id)
          let errorPropertyName = trimEnd(error.fieldName, '-value')
          let errorFieldName = ''

          item.loaded = true

          if (splits.length > 1) {
            errorPropertyName = trimEnd(`item-${splits[0]}`, '-')
            errorFieldName = `item-${splits.splice(1).join('item-')}`
          }

          serverValidationManager.addPropertyError(errorPropertyName, errorFieldName, error.errorMsg)
        }
      }
    })

    this.contentReady = true
  }

  setFiles (newFiles) {
    const files = this.fileManager.getFiles()
      .filter(item => item.alias === this.$scope.model.alias)
      .map(item => item.file)
      .concat(newFiles)

    this.fileManager.setFiles(this.$scope.model.alias, files)
  }

  validate () {
    if (this.config.minItems && this.config.minItems > this.$scope.model.value.length) {
      this.currentForm.minItems.$setValidity('minItems', false)
    } else {
      this.currentForm.minItems.$setValidity('minItems', true)
    }

    if (this.config.maxItems && this.config.maxItems < this.$scope.model.value.length) {
      this.currentForm.maxItems.$setValidity('maxItems', false)
    } else {
      this.currentForm.maxItems.$setValidity('maxItems', true)
    }
  }

  add (documentType) {
    this.contentResource.getScaffold(this.editorState.current.id || -1, documentType.documentTypeAlias)
      .then(data => {
        const item = this.init({
          key: data.key,
          allowEditingName: documentType.allowEditingName === '1',
          contentTypeAlias: data.contentTypeAlias,
          contentTypeName: data.contentTypeName,
          icon: documentType.icon,
          published: true,
          name: documentType.allowEditingName === '1' ? '' : documentType.name,
          parentId: this.editorState.current.id,
          // filter out Generic Propeties tab
          tabs: data.tabs.filter(tab => tab.alias !== 'Generic properties')
        })
        this.$scope.model.value.push(item)
        this.currentForm.$setDirty()
        this.activate(item)
      })
  }

  init (item) {
    if (!item.allowEditingName) {
      const documentType = this.config.documentTypes.find(docType => docType.documentTypeAlias === item.contentTypeAlias)

      if (!documentType.nameExpression) {
        documentType.nameExpression = this.$interpolate(documentType.nameTemplate || 'Item {{$index}}')
      }

      const nameExpression = documentType.nameExpression

      delete item.name

      item.toJSON = function () { return Object.assign({ name: this.name }, this) }

      Object.defineProperty(item, 'name', {
        get: () => {
          let index = this.$scope.model.value.indexOf(item)

          if (index === -1) {
            index = this.$scope.model.value.length + 1
          }

          const properties = item.tabs
            .reduce((cur, tab) => cur.concat(tab.properties), [])
            .reduce((obj, property) => {
              let alias = property.alias
              if (alias.indexOf(`item-${item.key}-`) === 0) {
                alias = alias.substring(`item-${item.key}-`.length)
              }

              obj[alias] = this.embeddedContentLabelService.getPropertyLabel(property)
              return obj
            }, {})

          return nameExpression(Object.assign({}, properties, { '$index': index + 1 }))
        }
      })
    }

    if (this.active === item.key) {
      this.activate(item)
    }

    return item
  }

  remove (index) {
    this.$scope.model.value.splice(index, 1)
    this.currentForm.$setDirty()
  }

  activate (item) {
    item.active = true
    item.loaded = true

    this.$timeout(() => {
      // not sure where this undefined error comes from
      // but we need to set it to true, or else the form won't
      // submit after a validation error
      this.currentForm.$setValidity(undefined, true)
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

  togglePublished (item) { item.published = !item.published }

  togglePrompt (item, event) {
    event.stopPropagation()
    item.deletePrompt = !item.deletePrompt
  }

  hidePrompt (item, event) {
    item.deletePrompt = false

    // Hack: umb-confirm-action => on-cancel does not expose $event, se we set deletePropmtChanged to true so we can stop the item toggle
    this.deletePromptChanged = true
  }

  canAdd () {
    if (this.$scope.preview) {
      return true
    }
    if (this.config.maxItems && this.$scope.model.value.length >= this.config.maxItems) {
      return false
    }
    return this.allowedDocumentTypes.length > 0
  }

  openContentTypeOverlay (event) {
    if (this.allowedDocumentTypes.length === 1) {
      this.add(this.allowedDocumentTypes[0])
      return
    }

    const availableItems = this.allowedDocumentTypes.map(docType => {
      return {
        alias: docType.documentTypeAlias,
        name: docType.name,
        group: docType.group,
        description: docType.description,
        icon: docType.icon
      }
    }).reduce((prev, cur) => {
      const group = cur.group || this.groupOther
      if (!prev[group]) {
        prev[group] = []
      }
      prev[group].push(cur)
      return prev
    }, {})

    this.contentTypeOverlay = {
      view: contentTypePickerOverlay, // '/App_Plugins/EmbeddedContent/contenttypepicker.overlay.html',
      filter: this.config.enableFiltering === '1',
      title: this.localizationService.localize('embeddedContent_chooseContentType'),
      availableItems: availableItems,
      event: event,
      show: true,
      submit: (model) => {
        const documentType = this.config.documentTypes.find(docType => docType.documentTypeAlias === model.selectedItem.alias)
        this.add(documentType)
        this.contentTypeOverlay.show = false
        this.contentTypeOverlay = null
      }
    }
  }

  get allowedDocumentTypes () {
    return this.config.documentTypes
      .filter(docType => {
        if (!docType.maxInstances || docType.maxInstances < 1) {
          return true
        }
        return this.$scope.model.value.filter(item => item.contentTypeAlias === docType.documentTypeAlias).length < docType.maxInstances
      })
  }

  static $inject = [
    '$scope', '$timeout', '$interpolate', '$window', 'angularHelper', 'fileManager',
    'editorState', 'localizationService', 'contentResource', 'contentTypeResource',
    'serverValidationManager', 'DisPlay.Umbraco.EmbeddedContent.LabelService'
  ]
}
