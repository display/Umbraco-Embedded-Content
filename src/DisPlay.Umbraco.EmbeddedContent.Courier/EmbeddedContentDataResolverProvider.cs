namespace DisPlay.Umbraco.EmbeddedContent.Courier
{
    using System.Collections.Generic;
    using System.Linq;

    using Newtonsoft.Json;

    using global::Umbraco.Courier.Core;
    using global::Umbraco.Courier.Core.ProviderModel;
    using global::Umbraco.Courier.DataResolvers;
    using global::Umbraco.Courier.ItemProviders;

    using Models;

    public class EmbeddedContentDataResolverProvider : PropertyDataResolverProvider
    {

        public override string EditorAlias
        {
            get { return EmbeddedContent.Constants.PropertyEditorAlias; }
        }

        public override void PackagingDataType(DataType item)
        {
            var preValue = item.Prevalues.FirstOrDefault(_ => _.Alias == "embeddedContentConfig");
            if(preValue != null)
            {
                var config = JsonConvert.DeserializeObject<EmbeddedContentConfig>(preValue.Value);
                foreach(var docTypeConfig in config.DocumentTypes)
                {
                    var contentType = ExecutionContext.DatabasePersistence.RetrieveItem<DocumentType>(
                         new ItemIdentifier(docTypeConfig.DocumentTypeAlias, ItemProviderIds.documentTypeItemProviderGuid)
                     );

                    if(contentType != null)
                    {
                        item.Dependencies.Add(contentType.UniqueId.ToString(), ItemProviderIds.documentTypeItemProviderGuid);
                    }
                }
            }
        }

        public override void ExtractingDataType(DataType item)
        {
            base.ExtractingDataType(item);
        }

        public override void PackagingProperty(Item item, ContentProperty propertyData)
        {
            ProcessProperty(item, propertyData, true);
        }

        public override void ExtractingProperty(Item item, ContentProperty propertyData)
        {
            ProcessProperty(item, propertyData, false);
        }

        private void ProcessProperty(Item item, ContentProperty propertyData, bool packaging)
        {
            var propertyItemProvider = ItemProviderCollection.Instance.GetProvider(ItemProviderIds.propertyDataItemProviderGuid, ExecutionContext);

            if (propertyData.Value != null)
            {
                var items = JsonConvert.DeserializeObject<EmbeddedContentItem[]>(propertyData.Value.ToString());

                foreach (var embeddedContent in items)
                {
                    var contentType = ExecutionContext.DatabasePersistence.RetrieveItem<DocumentType>(
                        new ItemIdentifier(embeddedContent.ContentTypeAlias, ItemProviderIds.documentTypeItemProviderGuid)
                    );

                    if (contentType == null)
                    {
                        continue;
                    }

                    if (packaging)
                    {
                        item.Dependencies.Add(contentType.UniqueId.ToString(), ItemProviderIds.documentTypeItemProviderGuid);
                    }

                    foreach (var property in embeddedContent.Properties.ToList())
                    {
                        var propertyType = contentType.Properties.FirstOrDefault(_ => _.Alias == property.Key);
                        if (propertyType == null)
                        {
                            continue;
                        }

                        var dataType = ExecutionContext.DatabasePersistence.RetrieveItem<DataType>(
                            new ItemIdentifier(propertyType.DataTypeDefinitionId.ToString(), ItemProviderIds.dataTypeItemProviderGuid)
                        );

                        if (dataType == null)
                        {
                            continue;
                        }

                        var fakeItem = new ContentPropertyData
                        {
                            ItemId = item.ItemId,
                            Name = string.Format("{0} [{1}: Nested {2} ({3})]", new[] { item.Name, EditorAlias, dataType.PropertyEditorAlias, property.Key }),
                            Data = new List<ContentProperty>
                            {
                                new ContentProperty
                                {
                                    Alias = propertyType.Alias,
                                    DataType = propertyType.DataTypeDefinitionId,
                                    PropertyEditorAlias = dataType.PropertyEditorAlias,
                                    Value = property.Value
                                }
                            }
                        };


                        if (packaging)
                        {
                            ResolutionManager.Instance.PackagingItem(fakeItem, propertyItemProvider);
                            item.Dependencies.AddRange(fakeItem.Dependencies);
                            item.Resources.AddRange(fakeItem.Resources);
                        }
                        else
                        {
                            ResolutionManager.Instance.ExtractingItem(fakeItem, propertyItemProvider);
                        }

                        if (fakeItem.Data != null)
                        {
                            var data = fakeItem.Data.FirstOrDefault();
                            if (data != null)
                            {
                                embeddedContent.Properties[property.Key] = data.Value;
                                if (packaging)
                                {
                                    item.Dependencies.Add(data.DataType.ToString(), ItemProviderIds.dataTypeItemProviderGuid);
                                }
                            }
                        }
                    }
                }

                propertyData.Value = JsonConvert.SerializeObject(items);
            }
        }
    }
}
