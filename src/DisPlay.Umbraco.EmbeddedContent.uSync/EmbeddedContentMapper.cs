namespace DisPlay.Umbraco.EmbeddedContent.uSync
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Jumoo.uSync.Core.Mappers;

    using Newtonsoft.Json;

    using global::Umbraco.Core;
    using global::Umbraco.Core.Models;
    using global::Umbraco.Core.Services;

    using Jumoo.uSync.Core;

    using Models;

    public class EmbeddedContentMapper : IContentMapper
    {
        private readonly IContentTypeService _contentTypeService;
        private readonly IDataTypeService _dataTypeService;

        public EmbeddedContentMapper(IContentTypeService contentTypeService, IDataTypeService dataTypeService)
        {
            _contentTypeService = contentTypeService ?? throw new ArgumentNullException(nameof(contentTypeService));
            _dataTypeService = dataTypeService ?? throw new ArgumentNullException(nameof(dataTypeService));
        }

        public EmbeddedContentMapper() : this(
            ApplicationContext.Current.Services.ContentTypeService,
            ApplicationContext.Current.Services.DataTypeService)
        {
        }

        public string GetExportValue(int dataTypeDefinitionId, string value)
        {
            return GetValue(value, true);

        }

        public string GetImportValue(int dataTypeDefinitionId, string content)
        {
            return GetValue(content, false);
        }

        private string GetValue(string value, bool exporting)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            var items = JsonConvert.DeserializeObject<EmbeddedContentItem[]>(value);

            foreach (EmbeddedContentItem item in items)
            {
                IContentType contentType = _contentTypeService.GetContentType(item.ContentTypeAlias);
                if (contentType == null)
                {
                    continue;
                }

                foreach (KeyValuePair<string, object> property in item.Properties.ToList())
                {
                    PropertyType propertyType = contentType.CompositionPropertyTypes.FirstOrDefault(_ => _.Alias == property.Key);
                    if (propertyType == null)
                    {
                        continue;
                    }
                    IDataTypeDefinition dataType = _dataTypeService.GetDataTypeDefinitionById(propertyType.DataTypeDefinitionId);

                    IContentMapper mapper = ContentMapperFactory.GetMapper(new uSyncContentMapping { EditorAlias = dataType.PropertyEditorAlias });
                    if (mapper != null)
                    {
                        string newValue;
                        if(exporting)
                        {
                            newValue = mapper.GetExportValue(dataType.Id, property.Value.ToString());
                        }
                        else
                        {
                            newValue = mapper.GetImportValue(dataType.Id, property.Value.ToString());
                        }
                        item.Properties[property.Key] = newValue;
                    }
                }
            }

            return JsonConvert.SerializeObject(items, Formatting.Indented);
        }
    }
}
