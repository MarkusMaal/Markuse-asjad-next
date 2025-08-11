
# cross-build option
$crossbuild = $false
if (Test-Path "DevTool.json" -PathType Leaf) {
	$settings = Get-Content -Path "DevTool.json" | ConvertFrom-Json
	$crossbuild = $settings.CrossBuild
	$verbose = $settings.Verbose
}

function ShowProgress {
    [CmdletBinding()]
	param (
		$Activity,
		$Status,
		$Percent,
		$Operation
	)
	$esc = [char]27
	$setCursorTop = "$esc[5;0H"  # moves caret to top-left (0, 0)
	Write-Host "${setCursorTop}" -NoNewLine
	$PSStyle.Progress.View = 'Classic'
	Write-Progress -Activity $Activity -Status $Status -PercentComplete $Percent -CurrentOperation $Operation
	Start-Sleep -Seconds 1
}

# attempts to build a single executable for every project in the solution for your OS
# Note: Additional files may be created for projects that use LibVLC
clear
& "$(Split-Path $MyInvocation.MyCommand.Path)/_devTool_Logo.ps1"
# List of projects that can only be compiled as x86_64 and therefore must be translated through Rosetta when running on macOS
# We have to do that here, because LibVLC for Apple Silicon isn't available yet
$RosettaOnly = @('MarkuStation2','Pidu!')
$AppCount = (Get-ChildItem . -Directory -Exclude ".vscode",".vs",".idea",".git","out","UniplatformTest","MasCommon" | measure).Count
$Increment = 100 / $AppCount
if ($crossbuild) {
	$Increment = $Increment / 3
	$Increment = $Increment / 2
	$AppCount = $AppCount * 2 * 3
}

if (Test-Path "build.log") {
	Remove-Item "build.log" -Force
}
$Progress = 0
Get-ChildItem . -Directory -Exclude ".vscode",".vs",".idea",".git","out","UniplatformTest","MasCommon" | Foreach-Object {
	# this stores just the directory name, not the full path
	$Current = $Progress / $Increment
	$pn = $_.Name
	if ((Test-Path "$(Split-Path $MyInvocation.MyCommand.Path)/out/$pn" -PathType Leaf) -or (Test-Path "$(Split-Path $MyInvocation.MyCommand.Path)/out/$pn.d/$pn" -PathType Leaf) -or (Test-Path "$(Split-Path $MyInvocation.MyCommand.Path)/out/$pn.app/Contents/MacOS/$pn" -PathType Leaf)) {
		Write-Output "/!\ $pn already built. Skipping..."
	} else {
		if (!$crossbuild) {
			ShowProgress -Activity "Compiling $pn ..." -Status "Processed: $Current/$AppCount" -Percent $Progress -Operation "Publishing projects for current platform"
			if ($IsMacOS) {
				$r_option = ""
				if ($RosettaOnly -contains $pn) {
					ShowProgress -Activity "Compiling $pn in x86_64 mode ..." -Status "Processed: $Current/$AppCount" -Percent $Progress -Operation "Publishing projects for current platform"
					dotnet publish $pn -c Release -r osx-x64 -o out/$pn.d -p:PublishReadyToRun=true -p:PublishSingleFile=true -p:PublishTrimmed=true --self-contained true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugSymbols=false >> build.log
				} else
				{
					dotnet publish $pn -c Release -o out/$pn.d -p:PublishReadyToRun=true -p:PublishSingleFile=true -p:PublishTrimmed=true --self-contained true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugSymbols=false >> build.log
				}

			} else {
				dotnet publish $pn -c Release -o out -p:PublishReadyToRun=true -p:PublishSingleFile=true -p:PublishTrimmed=true --self-contained true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugSymbols=false >> build.log
			}
			$Progress = $Progress + $Increment
		} else {
			"win", "osx", "linux" | Foreach-Object {
					ShowProgress -Activity "Compiling ${pn}_$_-x64..." -Status "Processed: $Current/$AppCount" -Percent $Progress -Operation "Publishing projects for all supported platforms"
					dotnet publish $pn -r $_-x64 -c Release -o out/${pn}_$_-x64 -p:PublishReadyToRun=true -p:PublishSingleFile=true -p:PublishTrimmed=true --self-contained true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugSymbols=false >> build.log
					$Progress = $Progress + $Increment
					$Current = $Progress / $Increment
					ShowProgress -Activity "Compiling ${pn}_$_-arm64..." -Status "Processed: $Current/$AppCount" -Percent $Progress -Operation "Publishing projects for all supported platforms"
					dotnet publish $pn -r $_-arm64 -c Release -o out/${pn}_$_-arm64 -p:PublishReadyToRun=true -p:PublishSingleFile=true -p:PublishTrimmed=true --self-contained true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugSymbols=false >> build.log
					$Progress = $Progress + $Increment
					$Current = $Progress / $Increment
			}
		}
	}
}

Write-Output "Finished!"
Write-Progress -Activity "Compiling..." -Completed
