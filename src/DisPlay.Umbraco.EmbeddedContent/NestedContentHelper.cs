using System;
using System.Collections.Generic;
using System.Linq;
using DisPlay.Umbraco.EmbeddedContent.Models;
using Newtonsoft.Json.Linq;

namespace DisPlay.Umbraco.EmbeddedContent
{
    internal class NestedContentHelper
    {
        public static JArray ConvertFromNestedContent(JArray input)
        {
            if (input == null)
            {
                return null;
            }

            var result = new List<EmbeddedContentItem>();

            // ReSharper disable once PossibleInvalidCastExceptionInForeachLoop
            foreach (IDictionary<string, JToken> token in input)
            {
                JToken ncContentTypeAlias = token["ncContentTypeAlias"];
                // if the token doesn't contain a ncAlias, we assume that none of the others does
                // and return the original value immediately
                if (ncContentTypeAlias == null)
                {
                    return input;
                }

                var embeddedContentItem = new EmbeddedContentItem
                {
                    Name = token["name"]?.Value<string>(),
                    ContentTypeAlias = ncContentTypeAlias.Value<string>(),
                    Key = Guid.NewGuid(),
                    Properties = token.ToDictionary(_ => _.Key, _ => (object) _.Value),
                    Published = token["ncDisabled"]?.Value<string>() != "1",
                };

                result.Add(embeddedContentItem);
            }

            return JArray.FromObject(result);
        }
    }
}
