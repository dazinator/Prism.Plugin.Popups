[CmdletBinding()]
Param(
	[validateSet("Release", "Debug")]
	[string]$Configuration = "Release"
)

Write-Host "Starting $Configuration Build"

$solutionFile = "Prism.Plugin.Popups.sln"

nuget restore $solutionFile

msbuild $solutionFile /t:Clean

msbuild $solutionFile /p:"Configuration=$Configuration"