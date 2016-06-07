using System;
using System.Collections.Generic;

namespace DisPlay.EmbeddedContent.Models
{
    internal class EmbeddedContentItem
    {
        public string ContentTypeAlias { get; set; }
        public Guid Key { get; set; }
        public string Name { get; set; }
        public bool Published { get; set; }
        public IDictionary<string, object> Properties { get; set; }
    }
}
