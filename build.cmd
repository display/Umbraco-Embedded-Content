@ECHO OFF
CLS

IF DEFINED APPVEYOR_BUILD_NUMBER (
	SET "buildVersion=build%APPVEYOR_BUILD_NUMBER%"
)

SETLOCAL
SET "target=%*"
IF NOT DEFINED target (
    SET "target=default"
)

dotnet tool install fake-cli --tool-path tools\bin\ --version 5.*
tools\bin\fake.exe run tools\build.fsx --parallel 3 --target %target% -e buildVersion=%buildVersion%
