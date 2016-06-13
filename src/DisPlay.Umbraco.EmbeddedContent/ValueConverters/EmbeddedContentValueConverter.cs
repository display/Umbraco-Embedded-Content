namespace DisPlay.Umbraco.EmbeddedContent.ValueConverters
{

    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using global::Umbraco.Core;
    using global::Umbraco.Core.Models;
    using global::Umbraco.Core.Models.PublishedContent;
    using global::Umbraco.Core.PropertyEditors;

    using Models;

    public class EmbeddedContentValueConverter : PropertyValueConverterBase, IPropertyValueConverterMeta
    {
        public PropertyCacheLevel GetPropertyCacheLevel(PublishedPropertyType propertyType, PropertyCacheValue cacheValue)
        {
            return PropertyCacheLevel.Content;
        }

        public Type GetPropertyValueType(PublishedPropertyType propertyType)
        {
            //TODO: Change if adding support for maxItems

            return typeof(IEnumerable<IPublishedContent>);
        }

        public override object ConvertDataToSource(PublishedPropertyType propertyType, object source, bool preview)
        {
            if(string.IsNullOrEmpty(source?.ToString()))
            {
                return null;
            }

            //TODO: Convert from nested content

            return JArray.Parse(source.ToString());
        }

        public override object ConvertSourceToObject(PublishedPropertyType propertyType, object source, bool preview)
        {
            if(source == null)
            {
                return Enumerable.Empty<IPublishedContent>();
            }

            var preValues = ApplicationContext.Current.Services.DataTypeService.GetPreValuesCollectionByDataTypeId(propertyType.DataTypeId);
            var configPreValue = preValues.PreValuesAsDictionary["embeddedContentConfig"];
            var config = JsonConvert.DeserializeObject<EmbeddedContentConfig[]>(configPreValue.Value);

            var result = new List<IPublishedContent>();
            var items = ((JArray)source).ToObject<EmbeddedContentItem[]>();

            for(var i  = 0; i < items.Length; i++)
            {
                EmbeddedContentItem item = items[i];

                if(!item.Published)
                {
                    continue;
                }

                if(config.FirstOrDefault(x => x.DocumentTypeAlias == item.ContentTypeAlias) == null)
                {
                    continue;
                }

                var contentType = PublishedContentType.Get(PublishedItemType.Content, item.ContentTypeAlias);
                if(contentType == null)
                {
                    continue;
                }

                IPublishedContent content = new PublishedEmbeddedContent(item, contentType, i, preview);

                if(PublishedContentModelFactoryResolver.HasCurrent)
                {
                    content = PublishedContentModelFactoryResolver.Current.Factory.CreateModel(content);
                }

                result.Add(content);
            }

            return result;

        }

        public override bool IsConverter(PublishedPropertyType propertyType)
        {
            return propertyType.PropertyEditorAlias == "DisPlay.Umbraco.EmbeddedContent";
        }
    }
}
