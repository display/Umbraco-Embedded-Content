(function() {
'use strict';

class EmbdeddedContentController {
  constructor($scope, $timeout, $interpolate, $routeParams, localizationService, contentResource, contentTypeResource) {
    this.$scope = $scope;
    this.$interpolate = $interpolate;
    this.$routeParams = $routeParams;
    this.localizationService = localizationService;
    this.contentResource = contentResource;

    if($scope.preview) {
      this.label = 'Embedded content';
      this.contentReady = true;
      return;
    }

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
      }
    };

    this.hasSettings = false;

    this.label = $scope.model.label;
    this.allowedDocumentTypes = $scope.model.config.embeddedContentConfig;

    $scope.model.value.forEach(item => {
      let documentType = this.allowedDocumentTypes.find(docType => docType.documentTypeAlias == item.contentTypeAlias);
      item.nameTemplateExpression = this.$interpolate(documentType.nameTemplate || 'Item {{$index}}');
    });

    this.items = $scope.model.value;

    $scope.$watch(() => this.items, () =>{ this.updateModel(); }, true);
    let unsubscribe = $scope.$on('formSubmitting', () => this.updateModel());
    $scope.$on('$destroy', () => { unsubscribe(); });

    $timeout(() => this.contentReady = true, 0);
  }

  updateModel() {
    this.$scope.model.value = this.items.map((item, i) => {
      let properties = {};
      item.properties.forEach(property => {
        properties[property.alias] = property.value;
      });

      let name = item.nameTemplateExpression(Object.assign({}, properties, { $index: i + 1 }));

      // Shouldn't be done here, but.. yeah..
      item.name = name;

      return {
        key: item.key,
        contentTypeAlias: item.contentTypeAlias,
        published: item.published,
        name: name,
        properties: properties
      };
    });
  }

  add(documentType) {
    this.contentResource.getScaffold(this.$routeParams.id, documentType.documentTypeAlias)
    .then(data => {
      this.items.push({
        key: data.key,
        contentTypeAlias: data.contentTypeAlias,
        contentTypeName: data.contentTypeName,
        active: true,
        icon: documentType.icon,
        published: true,
        name: documentType.name,
        nameTemplateExpression: this.$interpolate(documentType.nameTemplate || 'Item {{$index}}'),
        properties: data.tabs[0].properties
      });
    });
  }

  remove(index) { this.items.splice(index, 1); }
  activate(item) { item.active = true; }
  deactivate(item) { item.active = false; }
  togglePublished(item) { item.published = !item.published; }
  togglePrompt(item) { item.deletePrompt = !item.deletePrompt; }
  hidePrompt(item) { item.deletePrompt = false; }

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
        let documentType = this.allowedDocumentTypes.find(docType => docType.documentTypeAlias === model.selectedItem.alias);
        this.add(documentType);
        this.contentTypeOverlay.show = false;
        this.contentTypeOverlay = null;
      }
    };
  }
}

angular.module('umbraco')
.controller('DisPlay.Umbraco.EmbeddedContent.EmbdeddedContentController', EmbdeddedContentController);

})();
