// include Fake lib
#r "paket:
source nuget/dotnetcore
source https://api.nuget.org/v3/index.json
nuget FSharp.Core ~> 4.1
nuget Fake.Core.Target prerelease
nuget Fake.DotNet.Cli prerelease
nuget Fake.IO.FileSystem prerelease //"
#load "./.fake/build.fsx/intellisense.fsx"

open System.IO
open Fake.Core
open Fake.IO
open Fake.DotNet
open System

// Properties
let currentDirectory = Directory.GetCurrentDirectory()
let solutionFile = Directory.findFirstMatchingFile "*.sln" currentDirectory
let artifactsDir = Path.getFullName "./artifacts/"

let buildVersion = Environment.environVar "buildVersion"


// Targets
Target.create "Clean" (fun _ ->
  Shell.cleanDirs [artifactsDir]
)

Target.create "Build" (fun _ ->
  DotNet.build (fun c ->
    { c with
        Configuration = DotNet.BuildConfiguration.Release
        OutputPath = Some artifactsDir
    }) solutionFile
)

Target.create "Package" (fun _ ->
  DotNet.pack (fun c ->
    { c with
        Configuration = DotNet.BuildConfiguration.Release
        OutputPath = Some artifactsDir
        VersionSuffix = Some buildVersion
    }) solutionFile
)

Target.create "Default" ignore

open Fake.Core.TargetOperators

"Clean"
  ==> "Build"
  ==> "Default"

"Clean"
  ==> "Package"

// start build
Target.runOrDefault "Default"
