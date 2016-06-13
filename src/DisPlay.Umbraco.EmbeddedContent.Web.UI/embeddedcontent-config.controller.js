(function() {
'use strict';

class EmbeddedContentConfigController {
  constructor($scope, $timeout, $interpolate, localizationService, contentTypeResource) {
    this.$scope = $scope;
    this.$interpolate = $interpolate;
    this.localizationService = localizationService;

    this.hasSettings = false;

    contentTypeResource.getAll()
    .then(data => {
      this.documentTypes = data;
      this.model = this.$scope.model.value.map(this.init.bind(this)).filter(item => item);

      $scope.$watch(() => this.model, () =>{ this.updateModel(); }, true);
      let unsubscribe = $scope.$on('formSubmitting', () => this.updateModel());
      $scope.$on('$destroy', () => { unsubscribe(); });

      this.ready = true;
    });
  }

  updateModel() {
    this.$scope.model.value = this.model;
  }

  init(item) {
    let documentType = this.documentTypes.find(docType => docType.alias === item.documentTypeAlias);
    if(!documentType) {
      return;
    }
    return {
      documentTypeAlias: documentType.alias,
      name: documentType.name,
      icon: documentType.icon,
      nameTemplate : item.nameTemplate
    };
  }

  add(item) {
    let docType = this.init({
      documentTypeAlias: item.alias,
    });

    this.model.push(docType);
    this.model.sort((a, b) => a.name.localeCompare(b.name));

    this.editSettings(docType);
  }

  remove(index) { this.model.splice(index, 1); }
  togglePrompt(item) { item.deletePrompt = !item.deletePrompt; }
  hidePrompt(item) { item.deletePrompt = false; }

  editSettings(item, event) {
    let properties = [{
      label: 'Name template',
      description: '',
      alias: 'nameTemplate',
      config: {},
      validation: {
        mandatory: false,
        pattern: null
      },
      value: item.nameTemplate,
      view: 'textbox'
    }];

    this.editSettingsOverlay = {
      view: '/App_Plugins/EmbeddedContent/embeddedcontent-settings-overlay.html',
      title: this.localizationService.localize('embeddedContent_settings'),
      subtitle: item.documentTypeAlias,
      settings: properties,
      event: event,
      show: true,
      submit: (model) => {
        model.settings.forEach(property => {
          item[property.alias] = property.value;
        });

        item.active = false;
        this.editSettingsOverlay.show = false;
        this.editSettingsOverlay = null;
      },
      close: () => {
        item.active = false;
        this.editSettingsOverlay.show = false;
        this.editSettingsOverlay = null;
      }
    };
  }

  openContentTypeOverlay(event) {
    let availableItems = this.documentTypes
    .filter(docType => !this.model.find(item => item.documentTypeAlias === docType.alias))
    .map(docType => {
      return {
        alias: docType.alias,
        name: docType.name,
        description: docType.description,
        icon: docType.icon
      };
    });

    this.contentTypeOverlay = {
      view: 'itempicker',
      filter: true,
      title: this.localizationService.localize('embeddedContent_addDocumentType'),
      availableItems: availableItems,
      event: event,
      show: true,
      submit: (model) => {
        this.add(model.selectedItem);
        this.contentTypeOverlay.show = false;
        this.contentTypeOverlay = null;
      }
    };
  }
}

angular.module('umbraco')
.controller('DisPlay.Umbraco.EmbeddedContent.EmbeddedContentConfigController', EmbeddedContentConfigController);

})();
