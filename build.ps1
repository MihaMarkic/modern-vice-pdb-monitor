<#

.SYNOPSIS
This is a Powershell script to bootstrap a Cake build.

.DESCRIPTION
Modern.PdbMonitor build bootstrap.
.PARAMETER Script
The build script to execute.
.PARAMETER Target
The build script target to run.
.PARAMETER Configuration
The build configuration to use.
.PARAMETER Verbosity
Specifies the amount of information to be displayed.
.PARAMETER ShowDescription
Shows description about tasks.

.LINK
https://cakebuild.net

#>

[CmdletBinding()]
Param(
	[ValidateSet("Default", "Clean", "Build", "UnitTest")]
    [string]$Target,
    [string]$SolutionDirectory,
    [ValidateSet("Quiet", "Minimal", "Normal", "Verbose", "Diagnostic")]
    [string]$Verbosity,
    [switch]$ShowDescription,
    [Alias("WhatIf", "Noop")]
	[string]$ReleaseNotes
)

# Build Cake arguments
$cakeArguments = @("$Script")
if ($Target) { $cakeArguments += "--target=$Target" }
if ($SolutionDirectory) { $cakeArguments += "--solution-dir=$SolutionDirectory" }
if ($Verbosity) { $cakeArguments += "--verbosity=$Verbosity" }
if ($ShowDescription) { $cakeArguments += "--showdescription" }
#if ($DryRun) { $cakeArguments += "--dryrun" }
if ($ReleaseNotes) { $cakeArguments += "--releaseNotes=$ReleaseNotes" }
$cakeArguments += $ScriptArgs

if ($cakeArguments) {
    dotnet run --project source/PdbMonitor.Builder/PdbMonitor.Builder/build/Build.csproj -- $cakeArguments
} else {
    dotnet run --project source/PdbMonitor.Builder/PdbMonitor.Builder/build/Build.csproj
}
exit $LASTEXITCODE;