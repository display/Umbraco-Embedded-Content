(function() {
'use strict';

class EmbeddedContentPrevaluesController {
  constructor($scope, $timeout, $interpolate, angularHelper, localizationService, contentTypeResource) {
    this.$scope = $scope;
    this.$interpolate = $interpolate;
    this.localizationService = localizationService;

    this.currentForm = angularHelper.getCurrentForm($scope);

    if(!$scope.model.value) {
      $scope.model.value = {
        enableCollapsing: '1',
        allowEditingName: '0',
        documentTypes: []
      };
    }

    contentTypeResource.getAll()
    .then(data => {
      this.documentTypes = data;
      this.$scope.model.value.documentTypes = this.$scope.model.value.documentTypes.map(this.init.bind(this)).filter(item => item);

      this.ready = true;
    });
  }

  hasSettings() {
    return this.$scope.model.value.minItems
      || this.$scope.model.value.maxItems
      || this.$scope.model.value.enableCollapsing !== '1';
  }

  init(item) {
    let documentType = _.find(this.documentTypes, docType => docType.alias === item.documentTypeAlias);
    if(!documentType) {
      return;
    }
    return {
      documentTypeAlias: documentType.alias,
      name: documentType.name,
      icon: documentType.icon,
      nameTemplate : item.nameTemplate,
      allowEditingName: item.allowEditingName,
      maxInstances: item.maxInstances,
      hasConfig: item.nameTemplate || item.allowEditingName === '1' || item.maxInstances
    };
  }

  add(item) {
    let docType = this.init({
      documentTypeAlias: item.alias,
    });

    this.$scope.model.value.documentTypes.push(docType);
    this.$scope.model.value.documentTypes.sort((a, b) => a.name.localeCompare(b.name));

    this.editItemSettings(docType);

    this.currentForm.$setDirty();
  }

  remove(index) {
    this.$scope.model.value.documentTypes.splice(index, 1);
    this.currentForm.$setDirty();
  }

  togglePrompt(item) { item.deletePrompt = !item.deletePrompt; }
  hidePrompt(item) { item.deletePrompt = false; }

  editSettings(event) {
    let properties = [{
      label: 'Minimum number of items',
      alias: 'minItems',
      view: 'integer',
      value: this.$scope.model.value.minItems
    },{
      label: 'Maximum number of items',
      alias: 'maxItems',
      view: 'integer',
      value: this.$scope.model.value.maxItems
    },{
      label: 'Enable collapsing',
      alias: 'enableCollapsing',
      view: 'boolean',
      value: this.$scope.model.value.enableCollapsing
    }];

    this.editSettingsOverlay = {
      view: '/App_Plugins/EmbeddedContent/embeddedcontent-settings-overlay.html',
      title: this.localizationService.localize('embeddedContent_settings'),
      settings: properties,
      event: event,
      show: true,
      submit: (model) => {
        model.settings.forEach(property => {
          this.$scope.model.value[property.alias] = property.value;
        });

        this.currentForm.$setDirty();

        this.editSettingsOverlay.show = false;
        this.editSettingsOverlay = null;
      },
      close: () => {
        this.editSettingsOverlay.show = false;
        this.editSettingsOverlay = null;
      }
    };
  }

  editItemSettings(item, event) {
    let properties = [{
      label: 'Allow editing name',
      description: 'If checked, a mandatory name property is added and the name template won\'t be used.',
      alias: 'allowEditingName',
      value: item.allowEditingName,
      view: 'boolean'
    },{
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
    },{
      label: 'Maximum number of instances',
      description: 'How many instances of this item can be added?',
      alias: 'maxInstances',
      view: 'integer',
      value: item.maxInstances
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

        item.hasConfig = item.nameTemplate || item.allowEditingName === '1' || item.maxInstances;

        this.currentForm.$setDirty();

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
    .filter(docType => !_.find(this.$scope.model.value.documentTypes, item => item.documentTypeAlias === docType.alias))
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
.controller('DisPlay.Umbraco.EmbeddedContent.EmbeddedContentPrevaluesController', EmbeddedContentPrevaluesController);

})();
