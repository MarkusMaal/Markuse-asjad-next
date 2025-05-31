
# cross-build option
$crossbuild = $false
if (Test-Path "DevTool.json" -PathType Leaf) {
	$settings = Get-Content -Path "DevTool.json" | ConvertFrom-Json
	$crossbuild = $settings.CrossBuild
}

# attempts to build a single executable for every project in the solution for your OS
# Note: Additional files may be created for projects that use LibVLC
clear
& "$(Split-Path $MyInvocation.MyCommand.Path)/_devTool_Logo.ps1"
# List of projects that can only be compiled as x86_64 and therefore must be translated through Rosetta when running on macOS
# We have to do that here, because LibVLC for Apple Silicon isn't available yet
$RosettaOnly = @('MarkuStation2','Pidu!')
Get-ChildItem . -Directory -Exclude ".vscode",".vs",".idea",".git","out","UniplatformTest","MasCommon" | Foreach-Object {
	# this stores just the directory name, not the full path
	$pn = $_.Name
	if ((Test-Path "$(Split-Path $MyInvocation.MyCommand.Path)/out/$pn" -PathType Leaf) -or (Test-Path "$(Split-Path $MyInvocation.MyCommand.Path)/out/$pn.d/$pn" -PathType Leaf) -or (Test-Path "$(Split-Path $MyInvocation.MyCommand.Path)/out/$pn.app/Contents/MacOS/$pn" -PathType Leaf)) {
		Write-Output "/!\ $pn already built. Skipping..."
	} else {
		if (!$crossbuild) {
			Write-Output "- Compiling $pn ..."
			if ($IsMacOS) {
				$r_option = ""
				if ($RosettaOnly -contains $pn) {
					Write-Host "/!\ Compiling as x64 executable"
					dotnet publish $pn -c Release -r osx-x64 -o out/$pn.d -p:PublishReadyToRun=true -p:PublishSingleFile=true -p:PublishTrimmed=true --self-contained true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugSymbols=false
				} else
				{
					dotnet publish $pn -c Release -o out/$pn.d -p:PublishReadyToRun=true -p:PublishSingleFile=true -p:PublishTrimmed=true --self-contained true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugSymbols=false
				}

			} else {
				dotnet publish $pn -c Release -o out -p:PublishReadyToRun=true -p:PublishSingleFile=true -p:PublishTrimmed=true --self-contained true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugSymbols=false
			}
		} else {
			"win", "osx", "linux" | Foreach-Object {
					Write-Output " - Cross-compiling ${pn}_$_-x64"
					dotnet publish $pn -r $_-x64 -c Release -o out/${pn}_$_-x64 -p:PublishReadyToRun=true -p:PublishSingleFile=true -p:PublishTrimmed=true --self-contained true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugSymbols=false
					Write-Output " - Cross-compiling ${pn}_$_-arm64"
					dotnet publish $pn -r $_-arm64 -c Release -o out/${pn}_$_-arm64 -p:PublishReadyToRun=true -p:PublishSingleFile=true -p:PublishTrimmed=true --self-contained true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugSymbols=false
			}
		}
	}
}
