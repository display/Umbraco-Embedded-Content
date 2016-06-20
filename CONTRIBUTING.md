# Contributing

## Bug Reports & Feature Requests

Please use the [issue tracker](issues) to report any bugs or file feature requests.

## Developing

### Prerequisites

* Visual Studio 2015
* IIS Express
* Node and NPM

### Setup

```bat
> npm i
> npm start
```

This will install node dependencies, build the project, start watching files
and start IIS Express listening on `http://localhost:8080`

Change `debug` to `true` in `samples/Website/Web.config`

When installing Umbraco hit `Customize` and select `No thanks` when asked to install a Starter kit.

uSync wil automatically populate Umbraco with sample content from disk.

### Folder structure

TODO: Describe the folder structure

### Packaging and releasing

TODO: Describe package and release

```bat
> npm run package
```

```
> npm run release
```

### Coding conventions

TODO: Describe coding conventions
