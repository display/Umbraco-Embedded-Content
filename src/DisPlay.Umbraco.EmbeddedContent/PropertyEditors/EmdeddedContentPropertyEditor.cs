namespace DisPlay.Umbraco.EmbeddedContent.PropertyEditors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    using global::Umbraco.Core;
    using global::Umbraco.Core.Logging;
    using global::Umbraco.Core.Models;
    using global::Umbraco.Core.Models.Editors;
    using global::Umbraco.Core.PropertyEditors;
    using global::Umbraco.Core.Services;
    using global::Umbraco.Web;
    using global::Umbraco.Web.Models.ContentEditing;
    using global::Umbraco.Web.PropertyEditors;
    using global::Umbraco.Web.Security;

    using Models;

    [PropertyEditorAsset(ClientDependency.Core.ClientDependencyType.Css, "~/App_Plugins/EmbeddedContent/EmbeddedContent.min.css")]
    [PropertyEditorAsset(ClientDependency.Core.ClientDependencyType.Javascript, "~/App_Plugins/EmbeddedContent/EmbeddedContent.min.js")]
    [PropertyEditor("DisPlay.Umbraco.EmbeddedContent", "Embedded Content", "JSON", "~/App_Plugins/EmbeddedContent/embeddedcontent.html", Group = "Rich content", HideLabel = true, Icon = "icon-code")]
    public class EmbeddedContentPropertyEditor : PropertyEditor
    {
        private IContentTypeService _contentTypeService;
        private IDataTypeService _dataTypeService;
        private PropertyEditorResolver _propertyEditorResolver;
        private WebSecurity _security;
        private ProfilingLogger _profilingLogger;

        public EmbeddedContentPropertyEditor(
            IContentTypeService contentTypeService,
            IDataTypeService dataTypeService,
            ProfilingLogger profilingLogger,
            PropertyEditorResolver propertyEditorResolver,
            WebSecurity security)
        {
            _contentTypeService = contentTypeService;
            _dataTypeService = dataTypeService;
            _profilingLogger = profilingLogger;
            _propertyEditorResolver = propertyEditorResolver;
            _security = security;
        }

        public EmbeddedContentPropertyEditor() : this(
            ApplicationContext.Current.Services.ContentTypeService,
            ApplicationContext.Current.Services.DataTypeService,
            ApplicationContext.Current.ProfilingLogger,
            PropertyEditorResolver.Current,
            UmbracoContext.Current.Security)
        {

        }

        protected override PreValueEditor CreatePreValueEditor()
        {
           return new EmbeddedContentPreValueEditor();
        }

        protected override PropertyValueEditor CreateValueEditor()
        {
            return new EmbeddedContentValueEditor(
                base.CreateValueEditor(),
                _contentTypeService,
                _dataTypeService,
                _profilingLogger,
                _propertyEditorResolver,
                _security
            );
        }

        internal class EmbeddedContentPreValueEditor : PreValueEditor
        {
            [PreValueField("embeddedContentConfig", "Config", "/App_Plugins/EmbeddedContent/embeddedcontent.prevalues.html", HideLabel = true)]
            public string[] EmbeddedContentConfig { get; set; }
        }


        internal class EmbeddedContentValueEditor : PropertyValueEditorWrapper
        {
            private IContentTypeService _contentTypeService;
            private IDataTypeService _dataTypeService;
            private PropertyEditorResolver _propertyEditorResolver;
            private WebSecurity _security;
            private ProfilingLogger _profilingLogger;

            public EmbeddedContentValueEditor(
                PropertyValueEditor wrapped,
                IContentTypeService contentTypeService,
                IDataTypeService dataTypeService,
                ProfilingLogger profilingLogger,
                PropertyEditorResolver propertyEditorResolver,
                WebSecurity security) : base(wrapped)
            {
                _contentTypeService = contentTypeService;
                _dataTypeService = dataTypeService;
                _profilingLogger = profilingLogger;
                _propertyEditorResolver = propertyEditorResolver;
                _security = security;
            }

            public override void ConfigureForDisplay(PreValueCollection preValues)
            {
                using (_profilingLogger.DebugDuration<EmbeddedContentPropertyEditor>("ConfigureForDisplay()"))
                {
                    var contentTypes = _contentTypeService.GetAllContentTypes();

                    var configPreValue = preValues.PreValuesAsDictionary["embeddedContentConfig"];
                    var config = JObject.Parse(configPreValue.Value);

                    foreach (var item in config["documentTypes"])
                    {
                        var contentType = contentTypes.FirstOrDefault(x => x.Alias == item["documentTypeAlias"].Value<string>());
                        if (contentType != null)
                        {
                            item["name"] = contentType.Name;
                            item["description"] = contentType.Description;
                            item["icon"] = contentType.Icon;
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
                    var contentTypes = _contentTypeService.GetAllContentTypes();
                    var items = JsonConvert.DeserializeObject<EmbeddedContentItem[]>(property.Value.ToString());

                    foreach (var item in items)
                    {
                        var contentType = contentTypes.FirstOrDefault(x => x.Alias == item.ContentTypeAlias);
                        foreach (var propType in contentType.CompositionPropertyGroups.First().PropertyTypes)
                        {
                            object value;
                            item.Properties.TryGetValue(propType.Alias, out value);
                            PropertyEditor propertyEditor = _propertyEditorResolver.GetByAlias(propType.PropertyEditorAlias);

                            item.Properties[propType.Alias] = propertyEditor.ValueEditor.ConvertDbToString(
                              new Property(propType, value),
                              propType,
                              dataTypeService
                            );
                        }
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

                //TODO: Convert from nested content
                using (_profilingLogger.DebugDuration<EmbeddedContentPropertyEditor>($"ConvertDbToEditor({property.Alias})"))
                {
                    var source = JArray.Parse(property.Value.ToString());

                    var contentTypes = _contentTypeService.GetAllContentTypes();
                    var preValues = dataTypeService.GetPreValuesCollectionByDataTypeId(propertyType.DataTypeDefinitionId);

                    var configPreValue = preValues.PreValuesAsDictionary["embeddedContentConfig"];
                    var config = JsonConvert.DeserializeObject<EmbeddedContentConfig>(configPreValue.Value);

                    var items = source.ToObject<EmbeddedContentItem[]>();

                    return from indexedItem in items.Select((item, index) => new { item, index })
                           let item = indexedItem.item
                           let index = indexedItem.index
                           let configDocType = config.DocumentTypes.FirstOrDefault(x => x.DocumentTypeAlias == item.ContentTypeAlias)
                           where configDocType != null
                           let contentType = contentTypes.FirstOrDefault(x => x.Alias == item.ContentTypeAlias)
                           where contentType != null && contentType.CompositionPropertyGroups.Any()
                           select new EmbeddedContentItemDisplay
                           {
                               Key = item.Key,
                               ContentTypeAlias = item.ContentTypeAlias,
                               ContentTypeName = contentType.Name,
                               CreateDate = item.CreateDate,
                               UpdateDate = item.UpdateDate,
                               CreatorId = item.CreatorId,
                               WriterId = item.WriterId,
                               Icon = contentType.Icon,
                               Name = item.Name,
                               Published = item.Published,
                               Properties = from pt in contentType.CompositionPropertyGroups.First().PropertyTypes
                                            orderby pt.SortOrder
                                            let value = GetPropertyValue(item.Properties, pt.Alias)
                                            let p = GetProperty(pt, value)
                                            where p != null
                                            select p
                           };
                }
            }

            public override object ConvertEditorToDb(ContentPropertyData editorValue, object currentValue)
            {
                if (string.IsNullOrEmpty(editorValue.Value?.ToString()))
                {
                    return string.Empty;
                }

                using (_profilingLogger.DebugDuration<EmbeddedContentPropertyEditor>($"ConvertEditorToDb()"))
                {
                    var contentTypes = _contentTypeService.GetAllContentTypes();
                    var itemsDisplay = JsonConvert.DeserializeObject<EmbeddedContentItemDisplay[]>(editorValue.Value.ToString());
                    var currentItems = JsonConvert.DeserializeObject<EmbeddedContentItem[]>(currentValue?.ToString() ?? "[]");
                    var items = new List<EmbeddedContentItem>();

                    IEnumerable<ContentItemFile> files = null;

                    object tmp;
                    if (editorValue.AdditionalData.TryGetValue("files", out tmp))
                    {
                        files = tmp as IEnumerable<ContentItemFile>;
                    }

                    foreach (var itemDisplay in itemsDisplay)
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

                        var contentType = contentTypes.FirstOrDefault(x => x.Alias == itemDisplay.ContentTypeAlias);

                        if (contentType == null)
                        {
                            continue;
                        }

                        if (item.CreateDate == DateTime.MinValue)
                        {
                            item.CreateDate = DateTime.UtcNow;
                            item.CreatorId = _security.CurrentUser.Id;
                        }

                        var currentItem = currentItems.FirstOrDefault(x => x.Key == itemDisplay.Key);
                        if (currentItem != null)
                        {
                            item.CreateDate = currentItem.CreateDate;
                            item.UpdateDate = currentItem.UpdateDate;
                            item.CreatorId = currentItem.CreatorId;
                            item.WriterId = currentItem.WriterId;
                        }

                        foreach (var propertyType in contentType.CompositionPropertyGroups.First().PropertyTypes)
                        {
                            var property = itemDisplay.Properties.FirstOrDefault(x => x.Alias == propertyType.Alias);
                            if (property == null)
                            {
                                continue;
                            }

                            PropertyEditor propertyEditor = _propertyEditorResolver.GetByAlias(propertyType.PropertyEditorAlias);

                            var preValues = _dataTypeService.GetPreValuesCollectionByDataTypeId(propertyType.DataTypeDefinitionId);
                            var additionalData = new Dictionary<string, object>();

                            if (files != null)
                            {
                                if (propertyEditor.Alias == "DisPlay.Umbraco.EmbeddedContent")
                                {
                                    additionalData["files"] = files;
                                }
                                else if (property.SelectedFiles != null)
                                {
                                    additionalData["files"] = files.Where(x => property.SelectedFiles.Contains(x.FileName));
                                }
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
                            item.WriterId = _security.CurrentUser.Id;
                        }

                        items.Add(item);
                    }

                    return JsonConvert.SerializeObject(items);
                }
            }

            private EmbeddedContentPropertyDisplay GetProperty(PropertyType propertyType, object value)
            {
                var property = new EmbeddedContentPropertyDisplay
                {
                    Label = propertyType.Name,
                    Description = propertyType.Description,
                    Alias = propertyType.Alias,
                    Value = value
                };

                PropertyEditor propertyEditor = _propertyEditorResolver.GetByAlias(propertyType.PropertyEditorAlias);

                PreValueCollection preValues = _dataTypeService.GetPreValuesCollectionByDataTypeId(propertyType.DataTypeDefinitionId);

                property.Value = propertyEditor.ValueEditor.ConvertDbToEditor(
                    new Property(propertyType, value),
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
                object value;
                if (properties.TryGetValue(alias, out value))
                {
                    return value;
                }
                return null;
            }
        }
    }
}
