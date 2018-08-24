# Umbraco Embedded Content

[![NuGet release](https://img.shields.io/nuget/v/DisPlay.Umbraco.EmbeddedContent.svg)](https://www.nuget.org/packages/DisPlay.Umbraco.EmbeddedContent)
[![Our Umbraco project page](https://img.shields.io/badge/our-umbraco-orange.svg)](https://our.umbraco.com/projects/backoffice-extensions/embedded-content-blocks)

An Umbraco property editor that uses document types for content blocks.

![Demo](assets/demo.gif)


> *Note:*  Embedded Content works with **Umbraco 7.6.0** and newer


## Installation

The preferred way to install Embedded Content is through NuGet

### Option 1: NuGet

Embedded Content is available as a NuGet [package](https://www.nuget.org/packages/DisPlay.Umbraco.EmbeddedContent).

To install run the following command in the [Package Manager Console](https://docs.nuget.org/docs/start-here/using-the-package-manager-console)

```powershell
PM> Install-Package DisPlay.Umbraco.EmbeddedContent
```

### Option 2: Our Umbraco

Embedded Content is available as a package on [Our Umbraco](https://our.umbraco.com/projects/backoffice-extensions/embedded-content-blocks)

## Usage

For now look at the sample project in `samples/Website`

## Label resolvers

Out of the box the following property editors have custom label resolvers:

| Property editor alias         | Displayed value                       |
| ----------------------------- | ------------------------------------- |
| Umbraco.MultipleMediaPicker   | Comma separated list of media names   |
| Umbraco.MediaPicker2          | Comma separated list of media names   |
| Umbraco.MultiNodeTreePicker   | Comma separated list of entity names  |
| Umbraco.MultiNodeTreePicker2  | Comma separated list of entity names  |
| Umbraco.TinyMCEv3             | Text without HTML                     |
| UmbracoForms.FormPicker       | Form name                             |
| RJP.MultiUrlPicker            | Comma separated list of link names    |

Property editors without a label resolver shows the raw value.

### Creating a custom label resolver
```javascript
angular.module('umbraco')
.run(['DisPlay.Umbraco.EmbeddedContent.LabelResolvers', 'formResource', (labelResolvers, formResource) => {
  labelResolvers['UmbracoForms.FormPicker'] = (property, cacheService) => {
    const fromCache = cacheService.getOrAdd('UmbracoForms.FormPicker', property.value, () => {
      return formResource.getByGuid(property.value);
    });

    return fromCache ? fromCache.data.name : null;
  }
}]);
```

## Contributing

Anyone can help make this project better - check out our [Contributing guide](CONTRIBUTING.md)

## Authors

 * [Rasmus John Pedersen](https://www.github.com/rasmusjp)

## License

Copyright © 2016 DIS/PLAY A/S

Embedded Content is released under the [MIT License](LICENSE)
