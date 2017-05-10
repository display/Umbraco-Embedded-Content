// include Fake lib
#r @"packages/FAKE/tools/FakeLib.dll"
open Fake

// Properties
let solutionFile = FindFirstMatchingFile "*.sln" currentDirectory
let projectName =
  if hasBuildParam "project" then sprintf "DisPlay.Umbraco.EmbeddedContent.%s" (environVar "project")
  else "DisPlay.Umbraco.EmbeddedContent"

let uiDir = directoryInfo (sprintf "./src/%s.Web.UI" projectName)
let nuspecFile = sprintf "./build/nuspec/%s.nuspec" projectName

let version = environVarOrFail "release"
let informationalVersion =
  if hasBuildParam "buildVersion" then sprintf "%s-%s" version (environVar "buildVersion")
  else version

let nugetPath = FullName "./.nuget/nuget.exe"
let artifactsDir = FullName "./artifacts/"
let buildDir = artifactsDir @@ projectName @@ "bin"
let appPluginsDir = artifactsDir @@ "App_Plugins" @@ (fileNameWithoutExt solutionFile)

let Exec command args workingDir =
  let result = Shell.Exec(command, args, workingDir)
  if result <> 0 then failwithf "%s exited with error %d" command result

let yarnOrNpm =
  match tryFindFileOnPath "yarnpkg.cmd" with
  | Some path -> path
  | None ->
     match tryFindFileOnPath "npm.cmd" with
     | Some path -> path
     | None -> failwith "yarn or npm could not be found"

// Targets
Target "Clean" (fun _ ->
  CleanDirs [artifactsDir]
)

Target "RestorePackages" (fun _ ->
  solutionFile
  |> RestoreMSSolutionPackages(fun p ->
      { p with
          Retries = 4 })
)

Target "RestoreUiPackages" (fun _ ->
  if uiDir.Exists then
    Exec yarnOrNpm "install" uiDir.FullName
)

Target "AssemblyInfo" (fun _ ->
  ReplaceAssemblyInfoVersions (fun f ->
  { f with
      AssemblyVersion = version
      AssemblyInformationalVersion = informationalVersion
      OutputFileName = "./src" @@ projectName @@ "Properties" @@ "AssemblyInfo.cs" })
)

Target "Build" (fun _ ->
  !! solutionFile
  |> MSBuildRelease buildDir projectName
  |> ignore
)

Target "BuildUI" (fun _ ->
  if uiDir.Exists then
    Exec yarnOrNpm (sprintf "run build -- --output-path %s" appPluginsDir) uiDir.FullName
)

Target "Package" (fun _ ->
  nuspecFile
  |> NuGetPackDirectly (fun p ->
      { p with
          ToolPath = nugetPath
          WorkingDir = currentDirectory
          OutputPath = artifactsDir
          Version = informationalVersion })
)

Target "Default" DoNothing

// Dependencies
"Build" <=> "BuildUI"
  ==> "Default"

"RestoreUiPackages"
  ==> "BuildUI"

"RestorePackages"
  ==> "AssemblyInfo"
  ==> "Build"

"Build" <=> "BuildUI"
  ==> "Package"

// start build
RunTargetOrDefault "Default"
