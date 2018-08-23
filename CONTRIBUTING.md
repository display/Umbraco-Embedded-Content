# Contributing

## Bug Reports & Feature Requests

Please use the [issue tracker](https://github.com/display/umbraco-embedded-content/issues) to report any bugs or file feature requests.

## Developing

### Prerequisites

* Visual Studio
* IIS Express
* Node and NPM

### Setup

## Frontend

In `src/DisPlay.Umbraco.EmbeddedContent/ClientApp` run the following

```bat
> npm i
> npm start
```

This will install node dependencies, build the project, and start the webpack dev server on `http://localhost:3000` which proxies all requests except to `App_Plugins/EmbeddeContent/` to the backend server.

## Backend

First build the Solution 

Change `debug` to `true` in `samples/Website/Web.config`

Run the `samples/Website` project, this will start IIS Express listeninng on `http://localhost:49392` and open `http://localhost:3000`

When installing Umbraco hit `Customize` and select `No thanks` when asked to install a Starter kit.

uSync wil automatically populate Umbraco with sample content from disk.


### Folder structure

    ├── artifacts                                           # Compiled/Packaged files 
    ├── lib                                                 # Dependencies that's not on NuGet 
    ├── samples                                             # Sample projects
    │   ├── Website                                         # Sample/development site
    ├── src                                                 # Source files
    │   ├── DisPlay.Umbraco.EmbeddedContent                 # Property editor project
    │       ├── ClientApp                                   # Frontend project
    │           ├── app                                     # Frontend source files
    │           ├── dist                                    # Compiled frontend files
    │           ├── public                                  # Files copied to dist folder
    │   ├── DisPlay.Umbraco.EmbeddedContent.Courier         # Courier integration project
    │   ├── DisPlay.Umbraco.EmbeddedContent.Nexu            # Nexu integration project
    │   ├── DisPlay.Umbraco.EmbeddedContent.uSync           # uSync integration project
    ├── tools                                               # Build tools

### Packaging and releasing

TODO: Describe package and release

Increment version numbers in the respective csproj files

```bat
> build.bat package
```

### Coding conventions

TODO: Describe coding conventions
