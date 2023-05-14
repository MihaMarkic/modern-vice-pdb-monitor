<#

.SYNOPSIS
This is a Powershell script to bootstrap a Cake build.

.DESCRIPTION
KxGBuilder bootstrap.

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
	[ValidateSet("Test", "Publish")]
    [string]$Target,
    [string]$Configuration,
    [ValidateSet("Quiet", "Minimal", "Normal", "Verbose", "Diagnostic")]
    [string]$Verbosity,
    [switch]$ShowDescription,
    [Alias("WhatIf", "Noop")]
	[ValidateSet("Squirrel")]
	[string]$ProjectType,
	[string]$ReleaseNotes
)

# Build Cake arguments
$cakeArguments = @("$Script");
if ($Target) { $cakeArguments += "--target=$Target" }
#if ($Configuration) { $cakeArguments += "--configuration=$Configuration" }
if ($Verbosity) { $cakeArguments += "--verbosity=$Verbosity" }
if ($ShowDescription) { $cakeArguments += "--showdescription" }
#if ($DryRun) { $cakeArguments += "--dryrun" }
if ($ProjectType) { $cakeArguments += "--project-type=$ProjectType" }
if ($ReleaseNotes) { $cakeArguments += "--releaseNotes=$ReleaseNotes" }
$cakeArguments += $ScriptArgs

dotnet run --project BuildTool/BuildTool/build/Build.csproj -- $cakeArguments
exit $LASTEXITCODE;
