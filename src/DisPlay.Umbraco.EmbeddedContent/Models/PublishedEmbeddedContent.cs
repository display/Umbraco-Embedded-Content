﻿namespace DisPlay.Umbraco.EmbeddedContent.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using global::Umbraco.Core;
    using global::Umbraco.Core.Models;
    using global::Umbraco.Core.Models.PublishedContent;
    using global::Umbraco.Web.Models;

    internal class PublishedEmbeddedContent : PublishedContentWithKeyBase
    {
        private readonly IEnumerable<IPublishedProperty> _properties;
        private readonly Lazy<string> _writerName;
        private readonly Lazy<string> _creatorName;

        public PublishedEmbeddedContent(ApplicationContext applicationContext,
                                        EmbeddedContentItem item,
                                        PublishedContentType contentType,
                                        int sortOrder,
                                        bool isPreview)
        {
            Name = item.Name;
            Key = item.Key;
            UpdateDate = item.UpdateDate;
            CreateDate = item.CreateDate;
            CreatorId = item.CreatorId;
            WriterId = item.WriterId;
            IsDraft = isPreview;
            SortOrder = sortOrder;
            ContentType = contentType;

            _writerName = new Lazy<string>(() => applicationContext.Services.UserService.GetByProviderKey(WriterId).Name);
            _creatorName = new Lazy<string>(() => applicationContext.Services.UserService.GetByProviderKey(CreatorId).Name);

            _properties = from property in item.Properties
                          let propType = contentType.GetPropertyType(property.Key)
                          where propType != null
                          select new PublishedEmbeddedContentProperty(propType, property.Value, isPreview);
        }

        public override int Id => 0;
        public override string Name { get; }
        public override bool IsDraft { get; }
        public override PublishedItemType ItemType => PublishedItemType.Content;
        public override PublishedContentType ContentType { get; }
        public override string DocumentTypeAlias => ContentType.Alias;
        public override int DocumentTypeId => ContentType.Id;
        public override ICollection<IPublishedProperty> Properties => _properties.ToArray();
        public override IPublishedContent Parent { get; }
        public override IEnumerable<IPublishedContent> Children => Enumerable.Empty<IPublishedContent>();
        public override int TemplateId => 0;
        public override int SortOrder { get; }
        public override string UrlName => Name.ToUrlSegment();
        public override string WriterName => _writerName.Value;
        public override string CreatorName => _creatorName.Value;
        public override int WriterId { get; }
        public override int CreatorId { get; }
        public override string Path { get; }
        public override DateTime CreateDate { get; }
        public override DateTime UpdateDate { get; }
        public override Guid Version => Guid.Empty;
        public override int Level => 0;
        public override Guid Key { get; }

        public override IPublishedProperty GetProperty(string alias)
        {
            return _properties.FirstOrDefault(x => x.PropertyTypeAlias.Equals(alias, StringComparison.InvariantCultureIgnoreCase));
        }

        public override IPublishedProperty GetProperty(string alias, bool recurse)
        {
            IPublishedProperty property = GetProperty(alias);
            if (recurse && Parent != null && (property == null || false == property.HasValue))
            {
                return Parent.GetProperty(alias, true);
            }
            return property;
        }
    }
}
