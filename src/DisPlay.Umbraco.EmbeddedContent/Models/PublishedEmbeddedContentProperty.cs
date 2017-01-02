namespace DisPlay.Umbraco.EmbeddedContent.Models
{
    using System;

    using global::Umbraco.Core.Models;
    using global::Umbraco.Core.Models.PublishedContent;

    internal class PublishedEmbeddedContentProperty : IPublishedProperty
    {
        private readonly bool _isPreview;
        private readonly Lazy<object> _objectValue;
        private readonly PublishedPropertyType _propertyType;
        private readonly Lazy<object> _sourceValue;
        private readonly Lazy<object> _xpathValue;

        public PublishedEmbeddedContentProperty(PublishedPropertyType propertyType, object value, bool isPreview)
        {
            if (propertyType == null)
            {
                throw new ArgumentNullException(nameof(propertyType));
            }

            _propertyType = propertyType;
            _isPreview = isPreview;

            DataValue = value;

            _sourceValue = new Lazy<object>(() => _propertyType.ConvertDataToSource(DataValue, _isPreview));
            _objectValue = new Lazy<object>(() => _propertyType.ConvertSourceToObject(_sourceValue.Value, _isPreview));
            _xpathValue = new Lazy<object>(() => _propertyType.ConvertSourceToXPath(_sourceValue.Value, _isPreview));

            PropertyTypeAlias = propertyType.PropertyTypeAlias;
        }

        public string PropertyTypeAlias { get; }
        public bool HasValue => DataValue != null && DataValue.ToString().Trim().Length > 0;
        public object DataValue { get; }
        public object Value => _objectValue.Value;
        public object XPathValue => _xpathValue.Value;
    }
}
