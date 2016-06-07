namespace DisPlay.EmbeddedContent.PropertyEditors
{
    using System.Collections.Generic;
    using Newtonsoft.Json;
    using Umbraco.Core;
    using Umbraco.Core.Models;
    using Umbraco.Core.Models.Editors;
    using Umbraco.Core.PropertyEditors;
    using Umbraco.Core.Services;
    using Umbraco.Web.PropertyEditors;
    using Newtonsoft.Json.Linq;
    using System.Linq;
    using Umbraco.Web.Models.ContentEditing;
    using Models;

    [PropertyEditorAsset(ClientDependency.Core.ClientDependencyType.Css, "~/App_Plugins/DisPlay.Umbraco.EmbeddedContent/DisPlay.Umbraco.EmbeddedContent.min.css")]
    [PropertyEditorAsset(ClientDependency.Core.ClientDependencyType.Javascript, "~/App_Plugins/DisPlay.Umbraco.EmbeddedContent/DisPlay.Umbraco.EmbeddedContent.min.js")]
    [PropertyEditor("DisPlay.Umbraco.EmbeddedContent", "DIS/PLAY Embedded Content", "JSON", "~/App_Plugins/DisPlay.Umbraco.EmbeddedContent/EmbeddedContent.html", Group = "Rich content", HideLabel = true, Icon = "icon-code")]
    public class EmdeddedContentPropertyEditor : PropertyEditor
    {
        protected override PreValueEditor CreatePreValueEditor()
        {
           return new EmbeddedContentPreValueEditor();
        }

        protected override PropertyValueEditor CreateValueEditor()
        {
            return new EmbeddedContentValueEditor(base.CreateValueEditor());
        }

        internal class EmbeddedContentPreValueEditor : PreValueEditor
        {
            [PreValueField("embeddedContentConfig", "Config", "/App_Plugins/DisPlay.Umbraco.EmbeddedContent/embeddedcontent-config.html", HideLabel = false)]
            public string[] EmbeddedContentConfig { get; set; }

            public override IDictionary<string, object> ConvertDbToEditor(IDictionary<string, object> defaultPreVals, PreValueCollection persistedPreVals)
            {
                return base.ConvertDbToEditor(defaultPreVals, persistedPreVals);
            }
        }


        internal class EmbeddedContentValueEditor : PropertyValueEditorWrapper
        {
            public EmbeddedContentValueEditor(PropertyValueEditor wrapped) : base(wrapped)
            {
            }

            public override void ConfigureForDisplay(PreValueCollection preValues)
            {
                var contentTypes = ApplicationContext.Current.Services.ContentTypeService.GetAllContentTypes();

                var configPreValue = preValues.PreValuesAsDictionary["embeddedContentConfig"];
                var config = JArray.Parse(configPreValue.Value);

                foreach(var item in config)
                {
                    var contentType = contentTypes.FirstOrDefault(x => x.Alias == item["documentTypeAlias"].Value<string>());
                    if(contentType != null)
                    {
                        item["name"] = contentType.Name;
                        item["description"] = contentType.Description;
                        item["icon"] = contentType.Icon;
                    }
                }

                configPreValue.Value = config.ToString();

                base.ConfigureForDisplay(preValues);
            }

            public override string ConvertDbToString(Property property, PropertyType propertyType, IDataTypeService dataTypeService)
            {
                if (string.IsNullOrEmpty(property.Value?.ToString()))
                {
                    return string.Empty;
                }

                var contentTypes = ApplicationContext.Current.Services.ContentTypeService.GetAllContentTypes();
                var items = JsonConvert.DeserializeObject<EmbeddedContentItem[]>(property.Value.ToString());

                foreach (var item in items)
                {
                    var contentType = contentTypes.FirstOrDefault(x => x.Alias == item.ContentTypeAlias);
                    foreach (var propType in contentType.CompositionPropertyGroups.First().PropertyTypes)
                    {
                        object value = item.Properties[propType.Alias];
                        PropertyEditor propertyEditor = PropertyEditorResolver.Current.GetByAlias(propType.PropertyEditorAlias);

                        item.Properties[propType.Alias] = propertyEditor.ValueEditor.ConvertDbToString(
                          new Property(propType, value),
                          propType,
                          dataTypeService
                        );
                    }
                }

                return JsonConvert.SerializeObject(items);
            }

            public override object ConvertDbToEditor(Property property, PropertyType propertyType, IDataTypeService dataTypeService)
            {
                if (string.IsNullOrEmpty(property.Value?.ToString()))
                {
                    return new object[0];
                }

                //TODO: Convert from nested content

                var source = JArray.Parse(property.Value.ToString());

                var contentTypes = ApplicationContext.Current.Services.ContentTypeService.GetAllContentTypes();
                var preValues = dataTypeService.GetPreValuesCollectionByDataTypeId(propertyType.DataTypeDefinitionId);

                var configPreValue = preValues.PreValuesAsDictionary["embeddedContentConfig"];
                var config = JsonConvert.DeserializeObject<EmbeddedContentConfig[]>(configPreValue.Value);

                var items = ((JArray)source).ToObject<EmbeddedContentItem[]>();

                return from item in items
                       let configDocType = config.FirstOrDefault(x => x.DocumentTypeAlias == item.ContentTypeAlias)
                       where configDocType != null
                       let contentType = contentTypes.FirstOrDefault(x => x.Alias == item.ContentTypeAlias)
                       where contentType != null && contentType.CompositionPropertyGroups.Any()
                       select new
                       {
                           key = item.Key,
                           contentTypeAlias = item.ContentTypeAlias,
                           contentTypeName = contentType.Name,
                           icon = contentType.Icon,
                           name = item.Name,
                           published = item.Published,
                           properties = from pt in contentType.CompositionPropertyGroups.First().PropertyTypes
                                        orderby pt.SortOrder
                                        let value = item.Properties[pt.Alias]
                                        let p = GetProperty(pt, value, dataTypeService)
                                        where p != null
                                        select p
                       };
            }

            public override object ConvertEditorToDb(ContentPropertyData editorValue, object currentValue)
            {
                if (string.IsNullOrEmpty(editorValue.Value?.ToString()))
                {
                    return string.Empty;
                }

                var contentTypes = ApplicationContext.Current.Services.ContentTypeService.GetAllContentTypes();
                var items = JsonConvert.DeserializeObject<EmbeddedContentItem[]>(editorValue.Value.ToString());

                foreach(var item in items)
                {
                    var contentType = contentTypes.FirstOrDefault(x => x.Alias == item.ContentTypeAlias);
                    foreach(var propertyType in contentType.CompositionPropertyGroups.First().PropertyTypes)
                    {
                        //TODO: Add support for upload

                        var value = item.Properties[propertyType.Alias];
                        PropertyEditor propertyEditor = PropertyEditorResolver.Current.GetByAlias(propertyType.PropertyEditorAlias);

                        var preValues = ApplicationContext.Current.Services.DataTypeService.GetPreValuesCollectionByDataTypeId(propertyType.DataTypeDefinitionId);
                        var additionalData = new Dictionary<string, object>();

                        var propData = new ContentPropertyData(value, preValues, additionalData);


                        item.Properties[propertyType.Alias] = propertyEditor.ValueEditor.ConvertEditorToDb(propData, null);
                    }
                }

                return JsonConvert.SerializeObject(items);
            }

            private object GetProperty(PropertyType propertyType, object value, IDataTypeService dataTypeService)
            {
                var property = new ContentPropertyDisplay
                {
                    Label = propertyType.Name,
                    Description = propertyType.Description,
                    Alias = propertyType.Alias,
                    Value = value
                };

                PropertyEditor propertyEditor = PropertyEditorResolver.Current.GetByAlias(propertyType.PropertyEditorAlias);

                PreValueCollection preValues = dataTypeService.GetPreValuesCollectionByDataTypeId(propertyType.DataTypeDefinitionId);

                property.Value = propertyEditor.ValueEditor.ConvertDbToEditor(
                    new Property(propertyType, value), propertyType,
                    ApplicationContext.Current.Services.DataTypeService
                );


                propertyEditor.ValueEditor.ConfigureForDisplay(preValues);

                property.Config = propertyEditor.PreValueEditor.ConvertDbToEditor(propertyEditor.DefaultPreValues, preValues);
                property.View = propertyEditor.ValueEditor.View;
                property.HideLabel = propertyEditor.ValueEditor.HideLabel;

                return property;
            }
        }
    }
}
