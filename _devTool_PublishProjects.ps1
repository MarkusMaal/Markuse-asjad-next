# attempts to build a single executable for every project in the solution for your OS
# Note: Additional files may be created for projects that use LibVLC
clear
& "$(Split-Path $MyInvocation.MyCommand.Path)/_devTool_Logo.ps1"
Get-ChildItem . -Directory -Exclude ".vscode","UniplatformTest" | Foreach-Object {
	# this stores just the directory name, not the full path
	$pn = $_.Name
	Write-Output "- Compiling $pn ..."
	if ($IsMacOS) {
	    dotnet publish $pn -c Release -o out/$pn -p:PublishReadyToRun=true -p:PublishSingleFile=true -p:PublishTrimmed=true --self-contained true -p:IncludeNativeLibrariesForSelfExtract=true
	} else {
	    dotnet publish $pn -c Release -o $pn -p:PublishReadyToRun=true -p:PublishSingleFile=true -p:PublishTrimmed=true --self-contained true -p:IncludeNativeLibrariesForSelfExtract=true
	}
}
