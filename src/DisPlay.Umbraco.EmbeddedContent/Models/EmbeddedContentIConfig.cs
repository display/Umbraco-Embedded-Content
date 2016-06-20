namespace DisPlay.Umbraco.EmbeddedContent.Models
{
    using System.Collections.Generic;

    internal class EmbeddedContentConfig
    {
        public IEnumerable<EmbeddedContentConfigDocumentType> DocumentTypes { get; set; }
        public int? MaxItems { get; set; }
        public int? MinItems { get; set; }
    }
}
