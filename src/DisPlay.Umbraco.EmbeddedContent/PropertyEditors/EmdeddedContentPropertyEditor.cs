namespace DisPlay.Umbraco.EmbeddedContent.PropertyEditors
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    using global::Umbraco.Core;
    using global::Umbraco.Core.Dictionary;
    using global::Umbraco.Core.Logging;
    using global::Umbraco.Core.Models;
    using global::Umbraco.Core.Models.Editors;
    using global::Umbraco.Core.PropertyEditors;
    using global::Umbraco.Core.Services;
    using global::Umbraco.Web;
    using global::Umbraco.Web.Dictionary;
    using global::Umbraco.Web.Models.ContentEditing;
    using global::Umbraco.Web.PropertyEditors;
    using global::Umbraco.Web.Security;

    using Models;

    [PropertyEditorAsset(ClientDependency.Core.ClientDependencyType.Css, "~/App_Plugins/EmbeddedContent/EmbeddedContent.min.css")]
    [PropertyEditorAsset(ClientDependency.Core.ClientDependencyType.Javascript, "~/App_Plugins/EmbeddedContent/EmbeddedContent.min.js")]
    [PropertyEditor(EmbeddedContent.Constants.PropertyEditorAlias, "Embedded Content", "JSON", "~/App_Plugins/EmbeddedContent/embeddedcontent.html", Group = "Rich content", HideLabel = true, Icon = "icon-item-arrangement")]
    public class EmbeddedContentPropertyEditor : PropertyEditor
    {
        private readonly IContentTypeService _contentTypeService;
        private readonly IDataTypeService _dataTypeService;
        private readonly PropertyEditorResolver _propertyEditorResolver;
        private readonly Func<WebSecurity> _securityFactory;
        private readonly ICultureDictionary _cultureDictionary;
        private readonly ProfilingLogger _profilingLogger;

        public EmbeddedContentPropertyEditor(
            IContentTypeService contentTypeService,
            IDataTypeService dataTypeService,
            ICultureDictionary cultureDictionary,
            ProfilingLogger profilingLogger,
            PropertyEditorResolver propertyEditorResolver,
            Func<WebSecurity> securityFactory)
        {
            _contentTypeService = contentTypeService;
            _dataTypeService = dataTypeService;
            _cultureDictionary = cultureDictionary;
            _profilingLogger = profilingLogger;
            _propertyEditorResolver = propertyEditorResolver;
            _securityFactory = securityFactory;
        }

        public EmbeddedContentPropertyEditor() : this(
            ApplicationContext.Current.Services.ContentTypeService,
            ApplicationContext.Current.Services.DataTypeService,
            new DefaultCultureDictionary(),
            ApplicationContext.Current.ProfilingLogger,
            PropertyEditorResolver.Current,
            () => UmbracoContext.Current == null ? null : UmbracoContext.Current.Security)
        {

        }

        protected override PreValueEditor CreatePreValueEditor()
        {
            return new EmbeddedContentPreValueEditor(_contentTypeService, _cultureDictionary);
        }

        protected override PropertyValueEditor CreateValueEditor()
        {
            return new EmbeddedContentValueEditor(
                base.CreateValueEditor(),
                _contentTypeService,
                _dataTypeService,
                _cultureDictionary,
                _profilingLogger,
                _propertyEditorResolver,
                _securityFactory()
            );
        }

        internal class EmbeddedContentPreValueEditor : PreValueEditor
        {
            private readonly IContentTypeService _contentTypeService;
            private readonly ICultureDictionary _cultureDictionary;

            public EmbeddedContentPreValueEditor(IContentTypeService contentTypeService,
                ICultureDictionary cultureDictionary)
            {
                _contentTypeService = contentTypeService ?? throw new ArgumentNullException(nameof(contentTypeService));
                _cultureDictionary = cultureDictionary ?? throw new ArgumentNullException(nameof(cultureDictionary));
            }

            [PreValueField("embeddedContentConfig", "Config", "/App_Plugins/EmbeddedContent/embeddedcontent.prevalues.html", HideLabel = true)]
            public EmbeddedContentConfig EmbeddedContentConfig { get; set; }

            public override IDictionary<string, object> ConvertDbToEditor(IDictionary<string, object> defaultPreVals, PreValueCollection persistedPreVals)
            {
                if (persistedPreVals.PreValuesAsDictionary.TryGetValue("embeddedContentConfig",
                        out PreValue preValue) && false == string.IsNullOrEmpty(preValue.Value))
                {
                    List<IContentType> contentTypes = _contentTypeService.GetAllContentTypes().ToList();
                    EmbeddedContentConfig config = JsonConvert.DeserializeObject<EmbeddedContentConfig>(preValue.Value);
                    var configDisplay = new EmbeddedContentConfigDisplay
                    {
                        EnableCollapsing = config.EnableCollapsing,
                        EnableFiltering = config.EnableFiltering,
                        DocumentTypes = from item in config.DocumentTypes
                                        let contentType =
                                            contentTypes.FirstOrDefault(x => x.Alias == item.DocumentTypeAlias)
                                        where contentType != null
                                        select new EmbeddedContentConfigDocumentTypeDisplay
                                        {
                                            AllowEditingName = item.AllowEditingName,
                                            Description = UmbracoDictionaryTranslate(contentType.Description),
                                            DocumentTypeAlias = item.DocumentTypeAlias,
                                            Group = item.Group,
                                            Icon = contentType.Icon,
                                            MaxInstances = item.MaxInstances,
                                            Name = UmbracoDictionaryTranslate(contentType.Name),
                                            NameTemplate = item.NameTemplate,
                                            SettingsTab =
                                                item.SettingsTabKey.HasValue
                                                    ? contentType.CompositionPropertyGroups
                                                        .FirstOrDefault(x => x.Key == item.SettingsTabKey)?.Id
                                                    : null
                                        },
                        Groups = config.Groups,
                        MaxItems = config.MaxItems,
                        MinItems = config.MinItems
                    };

                    preValue.Value = JsonConvert.SerializeObject(configDisplay);
                }

                return base.ConvertDbToEditor(defaultPreVals, persistedPreVals);
            }

            public override IDictionary<string, PreValue> ConvertEditorToDb(IDictionary<string, object> editorValue, PreValueCollection currentValue)
            {
                if (editorValue.TryGetValue("embeddedContentConfig", out object value))
                {
                    List<IContentType> contentTypes = _contentTypeService.GetAllContentTypes().ToList();
                    EmbeddedContentConfigDisplay configDisplay = JsonConvert.DeserializeObject<EmbeddedContentConfigDisplay>(value.ToString());
                    var config = new EmbeddedContentConfig
                    {
                        EnableCollapsing = configDisplay.EnableCollapsing,
                        EnableFiltering = configDisplay.EnableFiltering,
                        DocumentTypes = from item in configDisplay.DocumentTypes
                                        let contentType = contentTypes.FirstOrDefault(x => x.Alias == item.DocumentTypeAlias)
                                        where contentType != null
                                        select new EmbeddedContentConfigDocumentType
                                        {
                                            AllowEditingName = item.AllowEditingName,
                                            DocumentTypeAlias = item.DocumentTypeAlias,
                                            Group = item.Group,
                                            MaxInstances = item.MaxInstances,
                                            NameTemplate = item.NameTemplate,
                                            SettingsTabKey = item.SettingsTab.HasValue ? contentType.CompositionPropertyGroups.FirstOrDefault(x => x.Id == item.SettingsTab)?.Key : null
                                        },
                        Groups = configDisplay.Groups,
                        MaxItems = configDisplay.MaxItems,
                        MinItems = configDisplay.MinItems
                    };

                    editorValue["embeddedContentConfig"] = JsonConvert.SerializeObject(config);
                }
                return base.ConvertEditorToDb(editorValue, currentValue);
            }

            private string UmbracoDictionaryTranslate(string text)
            {
                if (text == null || text.StartsWith("#") == false)
                {
                    return text;
                }

                text = text.Substring(1);
                return _cultureDictionary[text].IfNullOrWhiteSpace(text);
            }
        }


        internal class EmbeddedContentValueEditor : PropertyValueEditorWrapper
        {
            private readonly IContentTypeService _contentTypeService;
            private readonly IDataTypeService _dataTypeService;
            private readonly ICultureDictionary _cultureDictionary;
            private readonly PropertyEditorResolver _propertyEditorResolver;
            private readonly WebSecurity _security;
            private readonly ProfilingLogger _profilingLogger;

            public EmbeddedContentValueEditor(
                PropertyValueEditor wrapped,
                IContentTypeService contentTypeService,
                IDataTypeService dataTypeService,
                ICultureDictionary cultureDictionary,
                ProfilingLogger profilingLogger,
                PropertyEditorResolver propertyEditorResolver,
                WebSecurity security) : base(wrapped)
            {
                _contentTypeService = contentTypeService;
                _dataTypeService = dataTypeService;
                _cultureDictionary = cultureDictionary;
                _profilingLogger = profilingLogger;
                _propertyEditorResolver = propertyEditorResolver;
                _security = security;

                Validators.Add(new EmbeddedContentValidator(contentTypeService, dataTypeService));
            }

            public override void ConfigureForDisplay(PreValueCollection preValues)
            {
                using (_profilingLogger.DebugDuration<EmbeddedContentPropertyEditor>("ConfigureForDisplay()"))
                {
                    List<IContentType> contentTypes = _contentTypeService.GetAllContentTypes().ToList();

                    PreValue configPreValue = preValues.PreValuesAsDictionary["embeddedContentConfig"];
                    JObject config = JObject.Parse(configPreValue.Value);
                    config["configureForDisplay"] = true;

                    foreach (var item in config["documentTypes"].ToList())
                    {
                        var contentType = contentTypes.FirstOrDefault(x => x.Alias == item["documentTypeAlias"].Value<string>());
                        if (contentType == null)
                        {
                            item.Remove();
                            continue;
                        }

                        if (string.IsNullOrEmpty(item.Value<string>("nameTemplate")))
                        {
                            PropertyType propertyType = contentType.CompositionPropertyGroups
                                .OrderBy(x => x.SortOrder)
                                .FirstOrDefault()
                                ?.PropertyTypes
                                .OrderBy(x => x.SortOrder)
                                .FirstOrDefault();

                            if (propertyType != null)
                            {
                                item["nameTemplate"] = $"{{{{{propertyType.Alias}}}}}";
                            }
                        }
                    }

                    configPreValue.Value = config.ToString();
                }
                base.ConfigureForDisplay(preValues);
            }

            public override string ConvertDbToString(Property property, PropertyType propertyType, IDataTypeService dataTypeService)
            {
                if (string.IsNullOrEmpty(property.Value?.ToString()))
                {
                    return string.Empty;
                }
                using (_profilingLogger.DebugDuration<EmbeddedContentPropertyEditor>($"ConvertDbToString({property.Alias})"))
                {
                    List<IContentType> contentTypes = _contentTypeService.GetAllContentTypes().ToList();
                    var items = new List<EmbeddedContentItem>();

                    JArray source = NestedContentHelper.ConvertFromNestedContent(JArray.Parse(property.Value.ToString()));

                    foreach (EmbeddedContentItem item in source.ToObject<EmbeddedContentItem[]>())
                    {
                        if (!item.Published)
                        {
                            continue;
                        }

                        IContentType contentType = contentTypes.FirstOrDefault(x => x.Alias == item.ContentTypeAlias);
                        if (contentType == null)
                        {
                            continue;
                        }
                        foreach (PropertyType propType in contentType.CompositionPropertyGroups.SelectMany(_ => _.PropertyTypes))
                        {
                            item.Properties.TryGetValue(propType.Alias, out object value);
                            PropertyEditor propertyEditor = _propertyEditorResolver.GetByAlias(propType.PropertyEditorAlias);

                            if (propertyEditor == null)
                            {
                                continue;
                            }

                            item.Properties[propType.Alias] = propertyEditor.ValueEditor.ConvertDbToString(
                                new Property(propType, value),
                                propType,
                                dataTypeService
                            );
                        }

                        items.Add(item);
                    }

                    if (items.Count == 0)
                    {
                        return string.Empty;
                    }

                    return JsonConvert.SerializeObject(items);
                }
            }

            public override object ConvertDbToEditor(Property property, PropertyType propertyType, IDataTypeService dataTypeService)
            {
                if (string.IsNullOrEmpty(property.Value?.ToString()))
                {
                    return new object[0];
                }

                using (_profilingLogger.DebugDuration<EmbeddedContentPropertyEditor>($"ConvertDbToEditor({property.Alias})"))
                {
                    JArray source = NestedContentHelper.ConvertFromNestedContent(JArray.Parse(property.Value.ToString()));

                    List<IContentType> contentTypes = _contentTypeService.GetAllContentTypes().ToList();
                    PreValueCollection preValues = dataTypeService.GetPreValuesCollectionByDataTypeId(propertyType.DataTypeDefinitionId);

                    PreValue configPreValue = preValues.PreValuesAsDictionary["embeddedContentConfig"];
                    var config = JsonConvert.DeserializeObject<EmbeddedContentConfig>(configPreValue.Value);

                    var items = source.ToObject<EmbeddedContentItem[]>();

                    return (from indexedItem in items.Select((item, index) => new { item, index })
                            let item = indexedItem.item
                            let index = indexedItem.index
                            let configDocType = config.DocumentTypes.FirstOrDefault(x => x.DocumentTypeAlias == item.ContentTypeAlias)
                            where configDocType != null
                            let contentType = contentTypes.FirstOrDefault(x => x.Alias == item.ContentTypeAlias)
                            let tabs = (from pg in contentType.CompositionPropertyGroups
                                        orderby pg.SortOrder
                                        group pg by pg.Name into groupedByTabName
                                        let firstTab = groupedByTabName.First()
                                        let propertyTypes = groupedByTabName.SelectMany(x => x.PropertyTypes)
                                        select new TabWithKey<EmbeddedContentPropertyDisplay>()
                                        {
                                            Id = firstTab.Id,
                                            Key = firstTab.Key,
                                            Label = UmbracoDictionaryTranslate(firstTab.Name),
                                            Alias = firstTab.Key.ToString(),
                                            Properties = from pt in propertyTypes
                                                         orderby pt.SortOrder
                                                         let value = GetPropertyValue(item.Properties, pt.Alias)
                                                         let p = GetProperty(pt, value)
                                                         where p != null
                                                         select p
                                        }).ToList()
                            where contentType != null
                            select new EmbeddedContentItemDisplay
                            {
                                Key = item.Key,
                                AllowEditingName = configDocType.AllowEditingName == "1",
                                ContentTypeAlias = item.ContentTypeAlias,
                                ContentTypeName = UmbracoDictionaryTranslate(contentType.Name),
                                Description = UmbracoDictionaryTranslate(contentType.Description),
                                CreateDate = item.CreateDate,
                                UpdateDate = item.UpdateDate,
                                CreatorId = item.CreatorId,
                                WriterId = item.WriterId,
                                Icon = contentType.Icon,
                                Name = item.Name,
                                Published = item.Published,
                                SettingsTab = configDocType.SettingsTabKey.HasValue ? tabs.FirstOrDefault(x => x.Key == configDocType.SettingsTabKey) : null,
                                Tabs = configDocType.SettingsTabKey.HasValue ? tabs.Where(x => x.Key != configDocType.SettingsTabKey) : tabs
                            }).ToList();
                }
            }

            public override object ConvertEditorToDb(ContentPropertyData editorValue, object currentValue)
            {
                if (string.IsNullOrEmpty(editorValue.Value?.ToString()))
                {
                    return null;
                }

                using (_profilingLogger.DebugDuration<EmbeddedContentPropertyEditor>("ConvertEditorToDb()"))
                {
                    List<IContentType> contentTypes = _contentTypeService.GetAllContentTypes().ToList();
                    var itemsDisplay = JsonConvert.DeserializeObject<EmbeddedContentItemDisplay[]>(editorValue.Value.ToString());
                    var currentItems = JsonConvert.DeserializeObject<EmbeddedContentItem[]>(currentValue?.ToString() ?? "[]");
                    var items = new List<EmbeddedContentItem>();

                    IEnumerable<ContentItemFile> files = null;

                    if (editorValue.AdditionalData.TryGetValue("files", out object tmp))
                    {
                        files = tmp as IEnumerable<ContentItemFile>;
                    }

                    foreach (EmbeddedContentItemDisplay itemDisplay in itemsDisplay)
                    {
                        var item = new EmbeddedContentItem
                        {
                            ContentTypeAlias = itemDisplay.ContentTypeAlias,
                            Key = itemDisplay.Key,
                            Name = itemDisplay.Name,
                            Published = itemDisplay.Published,
                            CreateDate = itemDisplay.CreateDate,
                            UpdateDate = itemDisplay.UpdateDate,
                            CreatorId = itemDisplay.CreatorId,
                            WriterId = itemDisplay.WriterId
                        };

                        IContentType contentType = contentTypes.FirstOrDefault(x => x.Alias == itemDisplay.ContentTypeAlias);

                        if (contentType == null)
                        {
                            continue;
                        }

                        if (item.CreateDate == DateTime.MinValue)
                        {
                            item.CreateDate = DateTime.UtcNow;

                            if (_security?.CurrentUser != null)
                            {
                                item.CreatorId = _security.CurrentUser.Id;
                            }
                        }

                        EmbeddedContentItem currentItem = currentItems.FirstOrDefault(x => x.Key == itemDisplay.Key);
                        if (currentItem != null)
                        {
                            item.CreateDate = currentItem.CreateDate;
                            item.UpdateDate = currentItem.UpdateDate;
                            item.CreatorId = currentItem.CreatorId;
                            item.WriterId = currentItem.WriterId;
                        }

                        foreach (PropertyType propertyType in contentType.CompositionPropertyGroups.SelectMany(x => x.PropertyTypes))
                        {
                            IEnumerable<Tab<EmbeddedContentPropertyDisplay>> tabs = itemDisplay.Tabs;
                            if (itemDisplay.SettingsTab != null)
                            {
                                tabs = tabs.Concat(new[] { itemDisplay.SettingsTab });
                            }

                            EmbeddedContentPropertyDisplay property = tabs.SelectMany(x => x.Properties).FirstOrDefault(x => x.Alias == propertyType.Alias);
                            if (property == null)
                            {
                                continue;
                            }

                            PropertyEditor propertyEditor = _propertyEditorResolver.GetByAlias(propertyType.PropertyEditorAlias);

                            PreValueCollection preValues = _dataTypeService.GetPreValuesCollectionByDataTypeId(propertyType.DataTypeDefinitionId);
                            var additionalData = new Dictionary<string, object>();

                            if (files != null)
                            {
                                if (propertyEditor.Alias == EmbeddedContent.Constants.PropertyEditorAlias)
                                {
                                    additionalData["files"] = files;
                                }
                                else if (property.SelectedFiles != null)
                                {
                                    additionalData["files"] = files.Where(x => property.SelectedFiles.Contains(x.FileName));
                                }
                                additionalData["cuid"] = editorValue.AdditionalData["cuid"];
                                additionalData["puid"] = editorValue.AdditionalData["puid"];
                            }

                            var propData = new ContentPropertyData(property.Value, preValues, additionalData);
                            object currentPropertyValue = null;
                            if (currentItem != null && currentItem.Properties.TryGetValue(property.Alias, out currentPropertyValue))
                            {
                            }

                            item.Properties[propertyType.Alias] = propertyEditor.ValueEditor.ConvertEditorToDb(propData, currentPropertyValue);
                        }


                        if (currentItem == null
                            || currentItem.Name != item.Name
                            || currentItem.Published != item.Published
                            || JsonConvert.SerializeObject(currentItem.Properties) != JsonConvert.SerializeObject(item.Properties))
                        {
                            item.UpdateDate = DateTime.UtcNow;

                            if (_security?.CurrentUser != null)
                            {
                                item.WriterId = _security.CurrentUser.Id;
                            }
                        }

                        items.Add(item);
                    }

                    if (items.Count == 0)
                    {
                        return null;
                    }

                    return JsonConvert.SerializeObject(items);
                }
            }

            private EmbeddedContentPropertyDisplay GetProperty(PropertyType propertyType, object value)
            {
                var property = new EmbeddedContentPropertyDisplay
                {
                    Editor = propertyType.PropertyEditorAlias,
                    Label = UmbracoDictionaryTranslate(propertyType.Name),
                    Description = UmbracoDictionaryTranslate(propertyType.Description),
                    Alias = propertyType.Alias,
                    Value = value
                };

                PropertyEditor propertyEditor = _propertyEditorResolver.GetByAlias(propertyType.PropertyEditorAlias);

                PreValueCollection preValues = _dataTypeService.GetPreValuesCollectionByDataTypeId(propertyType.DataTypeDefinitionId);

                property.Value = propertyEditor.ValueEditor.ConvertDbToEditor(
                    new Property(propertyType, value?.ToString()),
                    propertyType,
                    _dataTypeService
                );


                propertyEditor.ValueEditor.ConfigureForDisplay(preValues);

                property.Config = propertyEditor.PreValueEditor.ConvertDbToEditor(propertyEditor.DefaultPreValues, preValues);
                property.View = propertyEditor.ValueEditor.View;
                property.HideLabel = propertyEditor.ValueEditor.HideLabel;
                property.Validation.Mandatory = propertyType.Mandatory;
                property.Validation.Pattern = propertyType.ValidationRegExp;

                return property;
            }

            private object GetPropertyValue(IDictionary<string, object> properties, string alias)
            {
                if (properties.TryGetValue(alias, out object value))
                {
                    return value;
                }
                return null;
            }

            private string UmbracoDictionaryTranslate(string text)
            {
                if (text == null || text.StartsWith("#") == false)
                {
                    return text;
                }

                text = text.Substring(1);
                return _cultureDictionary[text].IfNullOrWhiteSpace(text);
            }

            internal class EmbeddedContentValidator : IPropertyValidator
            {
                private readonly IContentTypeService _contentTypeService;
                private readonly IDataTypeService _dataTypeService;

                public EmbeddedContentValidator(IContentTypeService contentTypeService, IDataTypeService dataTypeService)
                {
                    _contentTypeService = contentTypeService ?? throw new ArgumentNullException(nameof(contentTypeService));
                    _dataTypeService = dataTypeService ?? throw new ArgumentNullException(nameof(dataTypeService));
                }

                public IEnumerable<ValidationResult> Validate(object value, PreValueCollection preValues, PropertyEditor editor)
                {
                    if (value == null)
                    {
                        yield break;
                    }

                    List<IContentType> contentTypes = _contentTypeService.GetAllContentTypes().ToList();
                    List<EmbeddedContentItemDisplay> itemsDisplay = JsonConvert.DeserializeObject<IEnumerable<EmbeddedContentItemDisplay>>(value.ToString()).ToList();

                    foreach (EmbeddedContentItemDisplay itemDisplay in itemsDisplay)
                    {
                        IContentType contentType = contentTypes.FirstOrDefault(x => x.Alias == itemDisplay.ContentTypeAlias);

                        if (contentType == null)
                        {
                            continue;
                        }

                        IEnumerable<EmbeddedContentPropertyDisplay> properties = itemDisplay.Tabs.SelectMany(x => x.Properties).ToList();

                        foreach (PropertyType propertyType in contentType.CompositionPropertyGroups.SelectMany(x => x.PropertyTypes))
                        {
                            EmbeddedContentPropertyDisplay property = properties.FirstOrDefault(x => x.Alias == propertyType.Alias);
                            if (property == null)
                            {
                                continue;
                            }

                            PropertyEditor propertyEditor = PropertyEditorResolver.Current.GetByAlias(propertyType.PropertyEditorAlias);
                            if (propertyEditor == null)
                            {
                                continue;
                            }

                            PreValueCollection propertyPreValues = _dataTypeService.GetPreValuesCollectionByDataTypeId(propertyType.DataTypeDefinitionId);
                            if (propertyType.Mandatory)
                            {
                                foreach (ValidationResult result in propertyEditor.ValueEditor.RequiredValidator.Validate(property.Value, "", propertyPreValues, editor))
                                {
                                    yield return new ValidationResult(result.ErrorMessage, result.MemberNames.Select(x => $"item-{itemDisplay.Key}-{property.Alias}-{x}"));
                                }
                            }

                            if (false == propertyType.ValidationRegExp.IsNullOrWhiteSpace())
                            {
                                var str = property.Value as string;
                                if (property.Value != null && false == str.IsNullOrWhiteSpace() || propertyType.Mandatory)
                                {
                                    foreach (ValidationResult result in propertyEditor.ValueEditor.RegexValidator.Validate(property.Value, propertyType.ValidationRegExp, propertyPreValues, editor))
                                    {
                                        yield return new ValidationResult(result.ErrorMessage, result.MemberNames.Select(x => $"item-{itemDisplay.Key}-{property.Alias}-{x}"));
                                    }
                                }
                            }

                            foreach (IPropertyValidator validator in propertyEditor.ValueEditor.Validators)
                            {
                                foreach (ValidationResult result in validator.Validate(property.Value, propertyPreValues, editor))
                                {
                                    yield return new ValidationResult(result.ErrorMessage, result.MemberNames.Select(x => $"item-{itemDisplay.Key}-{property.Alias}-{x}"));
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
