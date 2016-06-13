namespace DisPlay.Umbraco.EmbeddedContent.Models
{
    using System.Collections.Generic;

    internal class EmbeddedContentConfig
    {
        public IEnumerable<EmbeddedContentConfigDocumentType> DocumentTypes { get; set; }
    }
}
