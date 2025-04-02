# attempts to build a single executable for every project in the solution for your OS
# Note: Additional files may be created for projects that use LibVLC
clear
& "$(Split-Path $MyInvocation.MyCommand.Path)/_devTool_Logo.ps1"
# List of projects that can only be compiled as x86_64 and therefore must be translated through Rosetta when running on macOS
# We have to do that here, because LibVLC for Apple Silicon isn't available yet
[string] $RosettaOnly = 'MarkuStation2','Pidu!'
Get-ChildItem . -Directory -Exclude ".vscode",".vs",".idea",".git","out","UniplatformTest" | Foreach-Object {
	# this stores just the directory name, not the full path
	$pn = $_.Name
	Write-Output "- Compiling $pn ..."
	if ($IsMacOS) {
	    $r_option = ""
	    if ($RosettaOnly -contains $pn) {
			$r_option = "-r osx-x64"
	    }
	    dotnet publish $pn -c Release $r_option -o out/$pn.d -p:PublishReadyToRun=true -p:PublishSingleFile=true -p:PublishTrimmed=true --self-contained true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugSymbols=false
	} else {
	    dotnet publish $pn -c Release -o out -p:PublishReadyToRun=true -p:PublishSingleFile=true -p:PublishTrimmed=true --self-contained true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugSymbols=false
	}
}
