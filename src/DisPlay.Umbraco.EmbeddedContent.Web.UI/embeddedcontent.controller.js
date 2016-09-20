(function() {
'use strict';

function endsWith(str, searchString) {
  let position = str.length - searchString.length;
  let lastIndex = str.indexOf(searchString, position);
  return lastIndex !== -1 && lastIndex === position;
}

function trimEnd(str, strToRemove) {
  if (endsWith(str, strToRemove)) {
    return str.substring(0, str.length - strToRemove.length);
  }
  return str;
}

class EmbdeddedContentController {
  constructor($scope, $timeout, $interpolate, angularHelper, fileManager, editorState,
    localizationService, contentResource, contentTypeResource, serverValidationManager,
    embeddedContentLabelService) {

    this.$scope = $scope;
    this.$timeout = $timeout;
    this.$interpolate = $interpolate;
    this.fileManager = fileManager;
    this.editorState = editorState;
    this.localizationService = localizationService;
    this.contentResource = contentResource;
    this.embeddedContentLabelService = embeddedContentLabelService;

    if($scope.preview) {
      this.label = 'Embedded content';
      this.contentReady = true;
      return;
    }

    this.currentForm = angularHelper.getCurrentForm($scope);
    let currentForm = this.currentForm;

    let draggedRteSettings = {};
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
        let max = $('.embedded-content').width() - 150;
        if (parseInt(ui.helper.css('left')) > max) {
          ui.helper.css({ 'left': max + 'px' });
        }
        if (parseInt(ui.helper.css('left')) < 20) {
          ui.helper.css({ 'left': 20 });
        }
      },
      start: function (e, ui) {
        // Fade out row when sorting
        ui.item.context.style.display = 'block';
        ui.item.context.style.opacity = '0.5';

        draggedRteSettings = {};
        ui.item.parents('.embedded-content').find('.umb-rte textarea').each(function () {
          // remove all RTEs in the dragged row and save their settings
          let id = $(this).attr('id');
          let editor = _.findWhere(tinyMCE.editors, { id: id });
          if(editor) {
            draggedRteSettings[id] = editor.settings;
            tinyMCE.execCommand('mceRemoveEditor', false, id);
          }
        });
      },
      stop: function (e, ui) {
        // Fade in row when sorting stops
        ui.item.context.style.opacity = '1';

        // reset all RTEs affected by the dragging
        ui.item.parents('.embedded-content').find('.umb-rte textarea').each(function () {
          let id = $(this).attr('id');
          draggedRteSettings[id] = draggedRteSettings[id] || _.findWhere(tinyMCE.editors, { id: id }).settings;
          tinyMCE.execCommand('mceRemoveEditor', false, id);
          tinyMCE.init(draggedRteSettings[id]);
        });

        currentForm.$setDirty();
      }
    };

    this.hasSettings = false;

    this.label = $scope.model.label;
    this.description = $scope.model.description;
    this.config = $scope.model.config.embeddedContentConfig;

    if($scope.model.value.length === 0 && this.config.minItems === 1 && this.allowedDocumentTypes.length === 1) {
      this.add(this.allowedDocumentTypes[0]);
    }

    $scope.$watch('model.value', () => {
      this.$scope.model.value.forEach(this.init.bind(this));
    });

    $scope.$on('formSubmitting', this.validate.bind(this));

    $scope.$on('formSubmitted', () => {
      this.fileManager.setFiles(this.$scope.model.alias, []);
    });

    $scope.$on('valStatusChanged', (evt, args) => {
      // this is very ugly, but it works for now
      if (args.form.$invalid) {
        let errors = serverValidationManager.items.filter((error) => error.propertyAlias === this.$scope.model.alias);
        for(let i = 0; i < errors.length; i++) {
          let error = errors[i];
          let splits = error.fieldName.split('item-').filter(item => item);
          let errorPropertyName = trimEnd(error.fieldName, '-value');
          let errorFieldName = '';
          let id = error.fieldName.substr('item-'.length, 'f7ad912c-2907-4416-9b43-a38059953a80'.length);
          let item = _.findWhere(this.$scope.model.value, { key: id });

          item.loaded = true;

          if(splits.length > 1) {
            errorPropertyName = trimEnd(`item-${splits[0]}`, '-');
            errorFieldName = `item-${splits.splice(1).join('item-')}`;
          }

          serverValidationManager.addPropertyError(errorPropertyName, errorFieldName, error.errorMsg);
        }
      }
    });

    this.contentReady = true;
  }

  setFiles(newFiles) {
    let files = this.fileManager.getFiles()
      .filter(item => item.alias === this.$scope.model.alias)
      .map(item => item.file)
      .concat(newFiles);

    this.fileManager.setFiles(this.$scope.model.alias, files);
  }

  validate() {
    if(this.config.minItems && this.config.minItems > this.$scope.model.value.length) {
      this.currentForm.minItems.$setValidity('minItems', false);
    } else {
      this.currentForm.minItems.$setValidity('minItems', true);
    }

    if(this.config.maxItems && this.config.maxItems < this.$scope.model.value.length) {
      this.currentForm.maxItems.$setValidity('maxItems', false);
    } else {
      this.currentForm.maxItems.$setValidity('maxItems', true);
    }
  }

  add(documentType) {
    this.contentResource.getScaffold(this.editorState.current.id || -1, documentType.documentTypeAlias)
    .then(data => {
      let item = this.init({
        key: data.key,
        allowEditingName: documentType.allowEditingName === '1',
        contentTypeAlias: data.contentTypeAlias,
        contentTypeName: data.contentTypeName,
        icon: documentType.icon,
        published: true,
        name: documentType.allowEditingName === '1' ? '' : documentType.name,
        parentId: this.editorState.current.id,
        // filter out Generic Propeties tab
        tabs: data.tabs.filter(tab => tab.alias !== "Generic properties")
      });
      this.$scope.model.value.push(item);
      this.currentForm.$setDirty();
      this.activate(item);
    });
  }

  init(item) {
    if(!item.allowEditingName) {
      let documentType = _.find(this.config.documentTypes, docType => docType.documentTypeAlias == item.contentTypeAlias);

      if(!documentType.nameExpression) {
        documentType.nameExpression = this.$interpolate(documentType.nameTemplate || 'Item {{$index}}');
      }

      let nameExpression = documentType.nameExpression;
      let alias = item.alias;

      delete item.name;

      item.toJSON = function() {
        return _.extend({ name: this.name }, this);
      };

      Object.defineProperty(item, 'name', {
        get: () => {
          let index = this.$scope.model.value.indexOf(item);

          if(index === -1) {
            index = $scope.model.value.length + 1;
          }

          let properties = item.tabs
          .reduce((cur, tab) => cur.concat(tab.properties), [])
          .reduce((obj, property) => {
            let alias = property.alias;
            if(alias.indexOf(`item-${item.key}-`) === 0){
              alias = alias.substring(`item-${item.key}-`.length);
            }

            obj[alias] = this.embeddedContentLabelService.getPropertyLabel(property);
            return obj;
          }, {});

          return nameExpression(_.extend({}, properties, { '$index' : index + 1 }));
        }
      });
    }

    return item;
  }

  remove(index) {
    this.$scope.model.value.splice(index, 1);
    this.currentForm.$setDirty();
  }

  activate(item) {
    item.active = true;
    item.loaded = true;

    this.$timeout(() => {
      // not sure where this undefined error comes from
      // but we need to set it to true, or else the form won't
      // submit after a validation error
      this.currentForm.$setValidity(undefined, true);
    });
  }

  deactivate(item) { item.active = false; }

  toggle(item) {
    if(item.active) {
      this.deactivate(item);
    } else {
      this.activate(item);
    }
  }

  togglePublished(item) { item.published = !item.published; }

  togglePrompt(item, event) {
    event.stopPropagation();
    item.deletePrompt = !item.deletePrompt;
  }

  hidePrompt(item, $event) {
    event.stopPropagation();
    item.deletePrompt = false;
  }

  canAdd() {
    if(this.$scope.preview) {
      return true;
    }
    if(this.config.maxItems && this.$scope.model.value.length >= this.config.maxItems) {
      return false;
    }
    return this.allowedDocumentTypes.length > 0;
  }

  openContentTypeOverlay(event) {
    if(this.allowedDocumentTypes.length === 1) {
      this.add(this.allowedDocumentTypes[0]);
      return;
    }

    let availableItems = this.allowedDocumentTypes.map(docType => {
      return {
        alias: docType.documentTypeAlias,
        name: docType.name,
        description: docType.description,
        icon: docType.icon
      };
    });

    this.contentTypeOverlay = {
      view: 'itempicker',
      filter: false,
      title: this.localizationService.localize('embeddedContent_chooseContentType'),
      availableItems: availableItems,
      event: event,
      show: true,
      submit: (model) => {
        let documentType = _.find(this.config.documentTypes, docType => docType.documentTypeAlias === model.selectedItem.alias);
        this.add(documentType);
        this.contentTypeOverlay.show = false;
        this.contentTypeOverlay = null;
      }
    };
  }

  get allowedDocumentTypes() {
    return this.config.documentTypes
    .filter(docType => {
      if(!docType.maxInstances || docType.maxInstances < 1) {
        return true;
      }
      return this.$scope.model.value.filter(item => item.contentTypeAlias === docType.documentTypeAlias).length < docType.maxInstances;
    });
  }
}

EmbdeddedContentController.$inject = [
  '$scope', '$timeout', '$interpolate',  'angularHelper', 'fileManager',
  'editorState', 'localizationService', 'contentResource', 'contentTypeResource',
  'serverValidationManager', 'DisPlay.Umbraco.EmbeddedContent.LabelService'
];

angular.module('umbraco')
.controller('DisPlay.Umbraco.EmbeddedContent.EmbdeddedContentController', EmbdeddedContentController);

})();
