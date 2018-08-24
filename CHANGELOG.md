# Change log
All notable changes to this project will be documented in this file.
This project adheres to [Semantic Versioning](http://semver.org/).

## 1.2.3 - 2018-08-23
- Public release

## 1.2.2 - 2018-06-06
### Bugfixes
- Fix settings button not being shown when adding a new item

## 1.2.1 - 2018-06-06
### Features
- Store settings tab as Guid so Courier transfers get's easier

## 1.2.0 - 2018-06-06
### Features
- Add settings dialog based on a document type tab

### Bugfixes
- Don't toggle item if deletion is cancelled

## 1.1.1 - 2018-05-29
### Bugfixes
- Fix property values not always being synced

## 1.1.0 - 2018-01-17
### Features
- Value converter now returns a `CollectionSet`. This enables ExtensionMethods like `Index()`, `First()`, `Last()` etc. work

## 1.0.3 - 2017-12-11
### Bugfixes
- Ensure `ConvertFromNestedContent` is called in `ConvertDbToString`

## 1.0.2 - 2017-12-11
### Bugfixes
- Fix upload, cuid and puid additionalData is now required
- Fix validation and expanding active on error

## 1.0.1 - 2017-12-08
### Bugfixes
- Manually update property values instead of using two way binding

## 1.0.0 - 2017-09-08
### Features
- Update colours to match Umbraco 7.6

### Breaking
- Upgrade to Umbraco 7.6

## 0.7.0 - 2017-06-20
### Features
- Ensure expanded item is still expanded after save
- Use first property as name template if none is defined

## 0.6.2 - 2017-06-13
### Bugfixes
- Fix title overflow (for real this time)
- Fix bug when converting from Nested Content and a ncDisabled property exist

### Bugfixes
- Fix exception when property editor is null 
- Don't save unpublished items in the xml cache

## 0.6.0 - 2016-11-22
### Breaking
- Remove Parent support

### Features
- Merge property groups with same name to a single section
- Clean config UI
- Add filtering support when creating content
- Add item group support

### Bugfixes
- Fix item title overflow

## 0.5.1 - 2016-11-03
### Bugfixes
- Fix null exception

## 0.5.0 - 2016-11-02
### Features
- Support for custom labels
- Label for Umbraco forms

## 0.4.1 - 2016-09-20
### Bugfixes
- Fix angular dependency error when js is minified

## 0.4.0 - 2016-09-20
### Features
- Labels for media picker, multi node tree picker and multi url picker
- Show property description
- Toggle expanded when clicking on item control bar

### Bugfixes
- Sort styling is inherited by other property editors
- Item not expanded when allow expanding is false
- Fix error when adding item to new page
- Fix property editor settings could not be opened in Firefox

## 0.3.0 - 2016-08-29
### Features
- Add server-side validation
- Add support for defining max instances per document type

### Bugfixes
- Fix name template not working because it used the unique property alias

## 0.2.1 - 2016-07-04
### Features
- Replace unsupported IE javascript functions with underscore equivalent ones
- Backoffice performance optimisations
- Ensure properties have unique alias and id

## 0.2.0 - 2016-07-04
### Features
- Add name editing support
- Add tabs support
- Add support for document types, tabs and properties localization
- Implement EmbeddedPublishedContent Parent property
- Conversion from Nested Content

### Bugfixes
- Return single item from value converter if max items is 1

## 0.1.0 - 2016-06-21
- Initial release
