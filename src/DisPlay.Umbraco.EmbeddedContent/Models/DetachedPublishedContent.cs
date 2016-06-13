namespace DisPlay.Umbraco.EmbeddedContent.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using global::Umbraco.Core;
    using global::Umbraco.Core.Models;
    using global::Umbraco.Core.Models.PublishedContent;
    using global::Umbraco.Web.Models;

    internal class DetachedPublishedContent : PublishedContentBase, IPublishedContentWithKey
    {
        private readonly IEnumerable<IPublishedProperty> _properties;

        public DetachedPublishedContent(string name,
                                        Guid key,
                                        int sortOrder,
                                        PublishedContentType contentType,
                                        IEnumerable<IPublishedProperty> properties)
        {
            Name = name;
            Key = key;
            SortOrder = sortOrder;
            ContentType = contentType;
            _properties = properties ?? Enumerable.Empty<IPublishedProperty>();
        }

        public override int Id => 0;
        public override string Name { get; }
        public override bool IsDraft => false;
        public override PublishedItemType ItemType => PublishedItemType.Content;
        public override PublishedContentType ContentType { get; }
        public override string DocumentTypeAlias => ContentType?.Alias;
        public override int DocumentTypeId => ContentType?.Id ?? 0;
        public override ICollection<IPublishedProperty> Properties => _properties.ToArray();
        public override IPublishedContent Parent { get; }
        public override IEnumerable<IPublishedContent> Children => Enumerable.Empty<IPublishedContent>();
        public override int TemplateId => 0;
        public override int SortOrder { get; }
        public override string UrlName => Name.ToUrlSegment();
        public override string WriterName => null;
        public override string CreatorName => null;
        public override int WriterId => 0;
        public override int CreatorId => 0;
        public override string Path { get; }
        public override DateTime CreateDate => DateTime.MinValue;
        public override DateTime UpdateDate => DateTime.MinValue;
        public override Guid Version => Guid.Empty;
        public override int Level => 0;
        public Guid Key { get; }

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
