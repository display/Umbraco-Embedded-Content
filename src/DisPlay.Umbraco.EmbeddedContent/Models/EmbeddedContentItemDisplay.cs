namespace DisPlay.Umbraco.EmbeddedContent.Models
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    using global::Umbraco.Web.Models.ContentEditing;

    [DataContract]
    internal class EmbeddedContentItemDisplay
    {
        [DataMember(Name = "key")]
        public Guid Key { get; set; }

        [DataMember(Name = "contentTypeAlias")]
        public string ContentTypeAlias { get; set; }

        [DataMember(Name = "contentTypeName")]
        public string ContentTypeName { get; set; }

        [DataMember(Name = "createDate")]
        public DateTime CreateDate { get; set; }

        [DataMember(Name = "updateDate")]
        public DateTime UpdateDate { get; set; }

        [DataMember(Name = "icon")]
        public string Icon { get; set; }

        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "published")]
        public bool Published { get; set; }

        [DataMember(Name = "properties")]
        public IEnumerable<EmbeddedContentPropertyDisplay> Properties { get; set; }
    }
}
