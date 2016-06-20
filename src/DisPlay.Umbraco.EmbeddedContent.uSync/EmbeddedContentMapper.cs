namespace DisPlay.Umbraco.EmbeddedContent.uSync
{
    using System.Linq;

    using Jumoo.uSync.Core.Mappers;

    using Newtonsoft.Json;

    using global::Umbraco.Core;
    using global::Umbraco.Core.Services;

    using Models;

    public class EmbeddedContentMapper : IContentMapper
    {
        private IContentTypeService _contentTypeService;
        private IDataTypeService _dataTypeService;

        public EmbeddedContentMapper(IContentTypeService contentTypeService, IDataTypeService dataTypeService)
        {
            _contentTypeService = contentTypeService;
            _dataTypeService = dataTypeService;
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

            foreach (var item in items)
            {
                var contentType = _contentTypeService.GetContentType(item.ContentTypeAlias);
                if (contentType == null)
                {
                    continue;
                }

                foreach (var property in item.Properties.ToList())
                {
                    var propertyType = contentType.CompositionPropertyTypes.FirstOrDefault(_ => _.Alias == property.Key);
                    if (propertyType == null)
                    {
                        continue;
                    }
                    var dataType = _dataTypeService.GetDataTypeDefinitionById(propertyType.DataTypeDefinitionId);

                    IContentMapper mapper = ContentMapperFactory.GetMapper(new Jumoo.uSync.Core.uSyncContentMapping() { EditorAlias = dataType.PropertyEditorAlias });
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
