using DisPlay.EmbeddedContent.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.PropertyEditors;

namespace DisPlay.EmbeddedContent.ValueConverters
{
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

                var properties = from property in item.Properties
                                 let propType = contentType.GetPropertyType(property.Key)
                                 where propType != null
                                 select new DetachedPublishedProperty(propType, property.Value, preview);

                IPublishedContent content = new DetachedPublishedContent(item.Name, item.Key, i, contentType, properties);

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
