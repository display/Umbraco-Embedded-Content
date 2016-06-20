namespace DisPlay.Umbraco.EmbeddedContent.Models
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    [DataContract]
    public class EmbeddedContentItem
    {
        public EmbeddedContentItem()
        {
            Properties = new Dictionary<string, object>();
        }

        [DataMember(Name = "contentTypeAlias")]
        public string ContentTypeAlias { get; set; }

        [DataMember(Name = "createDate")]
        public DateTime CreateDate { get; set; }

        [DataMember(Name = "updateDate")]
        public DateTime UpdateDate { get; set; }

        [DataMember(Name = "creatorId")]
        public int CreatorId { get; set; }

        [DataMember(Name = "writerId")]
        public int WriterId { get; set; }

        [DataMember(Name = "key")]
        public Guid Key { get; set; }

        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "published")]
        public bool Published { get; set; }

        [DataMember(Name = "properties")]
        public IDictionary<string, object> Properties { get; set; }
    }
}
