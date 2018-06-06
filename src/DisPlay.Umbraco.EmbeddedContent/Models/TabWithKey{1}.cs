namespace DisPlay.Umbraco.EmbeddedContent.Models
{
    using System;
    using System.Runtime.Serialization;

    using global::Umbraco.Web.Models.ContentEditing;

    public class TabWithKey<T> : Tab<T>
    {
        [DataMember(Name = "key")]
        public Guid Key { get; set; }
    }
}
