namespace DisPlay.Umbraco.EmbeddedContent.Models
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    using global::Umbraco.Web.Models.ContentEditing;

    [DataContract]
    internal class EmbeddedContentPropertyDisplay : ContentPropertyDisplay
    {
        [DataMember(Name = "selectedFiles")]
        public IEnumerable<string> SelectedFiles { get; set; }
    }
}
