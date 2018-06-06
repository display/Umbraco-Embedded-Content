namespace DisPlay.Umbraco.EmbeddedContent.Models
{
    using System.Runtime.Serialization;

    [DataContract]
    internal class EmbeddedContentConfigDocumentTypeDisplay
    {
        [DataMember(Name = "allowEditingName")]
        public string AllowEditingName { get; set; }

        [DataMember(Name = "documentTypeAlias")]
        public string DocumentTypeAlias { get; set; }

        [DataMember(Name = "group")]
        public string Group { get; set; }

        [DataMember(Name = "maxInstances")]
        public int? MaxInstances { get; set; }

        [DataMember(Name = "nameTemplate")]
        public string NameTemplate { get; set; }

        [DataMember(Name = "settingsTab")]
        public int? SettingsTab { get; set; }
    }
}
