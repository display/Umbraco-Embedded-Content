namespace DisPlay.Umbraco.EmbeddedContent
{

    using global::Umbraco.Core.Models;
    using global::Umbraco.Core.Models.PublishedContent;

    internal class PassthroughPublishedContentModelFactory : IPublishedContentModelFactory
    {
        public static PassthroughPublishedContentModelFactory Instance = new PassthroughPublishedContentModelFactory();

        public IPublishedContent CreateModel(IPublishedContent content)
        {
            return content;
        }
    }
}