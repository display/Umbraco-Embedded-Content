﻿namespace DisPlay.Umbraco.EmbeddedContent.Models
{
    using System;

    using global::Umbraco.Core.Models;
    using global::Umbraco.Core.Models.PublishedContent;

    using ValueConverters;

    internal class PublishedEmbeddedContentProperty : IPublishedProperty
    {
        private readonly IPublishedContent _content;
        private readonly bool _isPreview;
        private readonly Lazy<object> _objectValue;
        private readonly PublishedPropertyType _propertyType;
        private readonly Lazy<object> _sourceValue;
        private readonly Lazy<object> _xpathValue;

        public PublishedEmbeddedContentProperty(IPublishedContent content, PublishedPropertyType propertyType, object value, bool isPreview)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }
            if (propertyType == null)
            {
                throw new ArgumentNullException(nameof(propertyType));
            }

            _content = content;
            _propertyType = propertyType;
            _isPreview = isPreview;

            DataValue = value;

            _sourceValue = new Lazy<object>(() => _propertyType.ConvertDataToSource(DataValue, _isPreview));
            _objectValue = new Lazy<object>(ConvertSourceToObject);
            _xpathValue = new Lazy<object>(() => _propertyType.ConvertSourceToXPath(_sourceValue.Value, _isPreview));

            PropertyTypeAlias = propertyType.PropertyTypeAlias;
        }

        public string PropertyTypeAlias { get; }
        public bool HasValue => DataValue != null && DataValue.ToString().Trim().Length > 0;
        public object DataValue { get; }
        public object Value => _objectValue.Value;
        public object XPathValue => _xpathValue.Value;

        private object ConvertSourceToObject()
        {
            // if the property type is EmbeddedContent we need to set the
            // parent to the content this property belongs to.
            // I bet this could be done way better, but it'll do for now.
            if (_propertyType.PropertyEditorAlias == Constants.PropertyEditorAlias)
            {
                var converter = new EmbeddedContentValueConverter();

                return converter.ConvertSourceToObject(_content, _propertyType, _sourceValue.Value, _isPreview);
            }
            return _propertyType.ConvertSourceToObject(_sourceValue.Value, _isPreview);
        }
    }
}
