namespace DisPlay.Umbraco.EmbeddedContent.Nexu
{
    using global::Umbraco.Core;
    using Our.Umbraco.Nexu.Core.ObjectResolution;

    internal class UmbracoEvents : ApplicationEventHandler
    {
        protected override void ApplicationStarting(UmbracoApplicationBase umbracoApplication,
            ApplicationContext applicationContext)
        {
            PropertyParserResolver.Current.AddType<EmbeddedContentParser>();
        }
    }
}
