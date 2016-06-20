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
    using global::Umbraco.Core.Services;

    using Models;

    public class EmbeddedContentValueConverter : PropertyValueConverterBase, IPropertyValueConverterMeta
    {
        private IDataTypeService _dataTypeService;
        private IUserService _userService;
        private IPublishedContentModelFactory _publishedContentModelFactory;

        public EmbeddedContentValueConverter(
            IDataTypeService dataTypeService,
            IUserService userService,
            IPublishedContentModelFactory publishedContentModelFactory)
        {
            _dataTypeService = dataTypeService;
            _userService = userService;
            _publishedContentModelFactory = publishedContentModelFactory;
        }

        public EmbeddedContentValueConverter() : this(
            ApplicationContext.Current.Services.DataTypeService,
            ApplicationContext.Current.Services.UserService,
            PublishedContentModelFactoryResolver.HasCurrent
                ? PublishedContentModelFactoryResolver.Current.Factory
                : PassthroughPublishedContentModelFactory.Instance)
        {

        }

        public PropertyCacheLevel GetPropertyCacheLevel(PublishedPropertyType propertyType, PropertyCacheValue cacheValue)
        {
            return PropertyCacheLevel.Content;
        }

        public Type GetPropertyValueType(PublishedPropertyType propertyType)
        {
            var config = GetConfig(propertyType.DataTypeId);

            if(config.MaxItems == 1)
            {
                return typeof(IPublishedContent);
            }

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
            var config = GetConfig(propertyType.DataTypeId);

            if (source == null)
            {
                if(config.MaxItems == 1)
                {
                    return null;
                }

                return Enumerable.Empty<IPublishedContent>();
            }

            var result = new List<IPublishedContent>();
            var items = ((JArray)source).ToObject<EmbeddedContentItem[]>();

            for(var i  = 0; i < items.Length; i++)
            {
                EmbeddedContentItem item = items[i];

                if(!item.Published)
                {
                    continue;
                }

                if(config.DocumentTypes.FirstOrDefault(x => x.DocumentTypeAlias == item.ContentTypeAlias) == null)
                {
                    continue;
                }

                var contentType = PublishedContentType.Get(PublishedItemType.Content, item.ContentTypeAlias);
                if(contentType == null)
                {
                    continue;
                }

                IPublishedContent content = _publishedContentModelFactory.CreateModel(
                    new PublishedEmbeddedContent(_userService, item, contentType, i, preview)
                );

                result.Add(content);
            }

            return result;

        }

        public override bool IsConverter(PublishedPropertyType propertyType)
        {
            return propertyType.PropertyEditorAlias == "DisPlay.Umbraco.EmbeddedContent";
        }

        private EmbeddedContentConfig GetConfig(int dataTypeId)
        {
            var preValues = _dataTypeService.GetPreValuesCollectionByDataTypeId(dataTypeId);
            var configPreValue = preValues.PreValuesAsDictionary["embeddedContentConfig"];
            return JsonConvert.DeserializeObject<EmbeddedContentConfig>(configPreValue.Value);
        }
    }
}
