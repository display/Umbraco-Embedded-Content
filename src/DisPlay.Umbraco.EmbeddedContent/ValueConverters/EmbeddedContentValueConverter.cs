namespace DisPlay.Umbraco.EmbeddedContent.ValueConverters
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    using global::Umbraco.Core;
    using global::Umbraco.Core.Logging;
    using global::Umbraco.Core.Models;
    using global::Umbraco.Core.Models.PublishedContent;
    using global::Umbraco.Core.PropertyEditors;
    using global::Umbraco.Core.Services;
    using global::Umbraco.Web;

    using Models;

    public class EmbeddedContentValueConverter : PropertyValueConverterBase, IPropertyValueConverterMeta
    {
        private readonly IDataTypeService _dataTypeService;
        private readonly IUserService _userService;
        private readonly IPublishedContentModelFactory _publishedContentModelFactory;
        private readonly ProfilingLogger _profilingLogger;
        private readonly Func<UmbracoContext> _umbracoContextFactory;

        public EmbeddedContentValueConverter(
            IDataTypeService dataTypeService,
            IUserService userService,
            IPublishedContentModelFactory publishedContentModelFactory,
            ProfilingLogger profilingLogger,
            Func<UmbracoContext> umbracoContextFactory)
        {
            _dataTypeService = dataTypeService;
            _userService = userService;
            _publishedContentModelFactory = publishedContentModelFactory;
            _profilingLogger = profilingLogger;
            _umbracoContextFactory = umbracoContextFactory;
        }

        public EmbeddedContentValueConverter() : this(
            ApplicationContext.Current.Services.DataTypeService,
            ApplicationContext.Current.Services.UserService,
            PublishedContentModelFactoryResolver.HasCurrent
                ? PublishedContentModelFactoryResolver.Current.Factory
                : PassthroughPublishedContentModelFactory.Instance,
            ApplicationContext.Current.ProfilingLogger,
            () => UmbracoContext.Current)
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
            return ConvertSourceToObject(null, propertyType, source, preview);
        }

        internal object ConvertSourceToObject(IPublishedContent parent, PublishedPropertyType propertyType, object source, bool preview)
        {
            var config = GetConfig(propertyType.DataTypeId);

            using (_profilingLogger.DebugDuration<EmbeddedContentValueConverter>($"ConvertSourceToObject({propertyType.PropertyTypeAlias})"))
            {
                if (source == null)
                {
                    if (config.MaxItems == 1)
                    {
                        return null;
                    }

                    return Enumerable.Empty<IPublishedContent>();
                }

                var result = new List<IPublishedContent>();
                var items = ((JArray)source).ToObject<EmbeddedContentItem[]>();

                for (var i = 0; i < items.Length; i++)
                {
                    EmbeddedContentItem item = items[i];

                    if (!item.Published)
                    {
                        continue;
                    }

                    if (config.DocumentTypes.FirstOrDefault(x => x.DocumentTypeAlias == item.ContentTypeAlias) == null)
                    {
                        continue;
                    }

                    var contentType = PublishedContentType.Get(PublishedItemType.Content, item.ContentTypeAlias);
                    if (contentType == null)
                    {
                        continue;
                    }
                    if (parent == null)
                    {
                        parent = _umbracoContextFactory().ContentCache.GetById(item.ParentId);
                    }
                    IPublishedContent content = _publishedContentModelFactory.CreateModel(
                        new PublishedEmbeddedContent(_userService, item, contentType, parent, i, preview)
                    );

                    result.Add(content);
                }

                if (config.MaxItems == 1)
                {
                    return result.FirstOrDefault();
                }

                return result;
            }
        }

        public override bool IsConverter(PublishedPropertyType propertyType)
        {
            return propertyType.PropertyEditorAlias == EmbeddedContent.Constants.PropertyEditorAlias;
        }

        private EmbeddedContentConfig GetConfig(int dataTypeId)
        {
            using (_profilingLogger.DebugDuration<EmbeddedContentValueConverter>($"GetConfig({dataTypeId})"))
            {
                var preValues = _dataTypeService.GetPreValuesCollectionByDataTypeId(dataTypeId);
                var configPreValue = preValues.PreValuesAsDictionary["embeddedContentConfig"];
                return JsonConvert.DeserializeObject<EmbeddedContentConfig>(configPreValue.Value);
            }
        }
    }
}
