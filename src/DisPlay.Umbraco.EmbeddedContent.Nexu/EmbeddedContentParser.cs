namespace DisPlay.Umbraco.EmbeddedContent.Nexu
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using DisPlay.Umbraco.EmbeddedContent.Models;
    using global::Umbraco.Core;
    using global::Umbraco.Core.Logging;
    using global::Umbraco.Core.Models;
    using global::Umbraco.Core.Services;
    using Newtonsoft.Json;
    using Our.Umbraco.Nexu.Core.Interfaces;
    using Our.Umbraco.Nexu.Core.ObjectResolution;

    internal class EmbeddedContentParser : IPropertyParser
    {
        private readonly IContentTypeService _contentTypeService;
        private readonly IDataTypeService _dataTypeService;
        private readonly ILogger _logger;

        public EmbeddedContentParser()
            : this(
                ApplicationContext.Current.Services.ContentTypeService,
                ApplicationContext.Current.Services.DataTypeService,
                ApplicationContext.Current.ProfilingLogger.Logger)
        {
        }

        public EmbeddedContentParser(IContentTypeService contentTypeService, IDataTypeService dataTypeService, ILogger logger)
        {
            _contentTypeService = contentTypeService ?? throw new ArgumentNullException(nameof(contentTypeService));
            _dataTypeService = dataTypeService ?? throw new ArgumentNullException(nameof(dataTypeService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool IsParserFor(IDataTypeDefinition dataTypeDefinition)
        {
            return dataTypeDefinition.PropertyEditorAlias.Equals(EmbeddedContent.Constants.PropertyEditorAlias);
        }

        public IEnumerable<ILinkedEntity> GetLinkedEntities(object propertyValue)
        {
            if (propertyValue == null)
            {
                return Enumerable.Empty<ILinkedEntity>();
            }

            var entities = new List<ILinkedEntity>();
            var contentTypes = new Dictionary<string, IContentType>();
            var dataTypes = new Dictionary<int, IDataTypeDefinition>();

            try
            {
                var items = JsonConvert.DeserializeObject<EmbeddedContentItem[]>(propertyValue.ToString());

                foreach (EmbeddedContentItem item in items)
                {
                    if (false == contentTypes.TryGetValue(item.ContentTypeAlias, out IContentType contentType))
                    {
                        contentTypes[item.ContentTypeAlias] = contentType = _contentTypeService.GetContentType(item.ContentTypeAlias);
                    }
                    if (contentType == null)
                    {
                        continue;
                    }
                    foreach (PropertyType propertyType in contentType.PropertyTypes)
                    {
                        if (false == item.Properties.TryGetValue(propertyType.Alias, out object value))
                        {
                            continue;
                        }

                        if (false ==dataTypes.TryGetValue(propertyType.DataTypeDefinitionId, out IDataTypeDefinition dataTypeDefinition))
                        {
                            dataTypes[propertyType.DataTypeDefinitionId] = dataTypeDefinition = _dataTypeService.GetDataTypeDefinitionById(propertyType.DataTypeDefinitionId);
                        }
                        if (dataTypeDefinition == null)
                        {
                            continue;
                        }
                        IPropertyParser parser = PropertyParserResolver.Current.Parsers.FirstOrDefault(x => x.IsParserFor(dataTypeDefinition));

                        if (parser != null)
                        {
                            entities.AddRange(parser.GetLinkedEntities(value));
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                _logger.Error<EmbeddedContentParser>("Error parsing embedded content", exception);
            }
            return entities;
        }
    }
}
