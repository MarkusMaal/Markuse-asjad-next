& "$(Split-Path $MyInvocation.MyCommand.Path)/_devTool_Logo.ps1"
Write-Output "Available projects:"
Get-ChildItem . -Directory -Exclude ".vscode","out","UniplatformTest" | Foreach-Object {
	# this stores just the directory name, not the full path
	$pn = $_.Name
	$in = $pn
	if ($platform -eq "Win32NT") {
		$pn = $pn + ".exe"
	}
	if (Test-Path "$(Split-Path $MyInvocation.MyCommand.Path)/out/$pn" -PathType Leaf) {
		Write-Output "- $in [Build OK]"
	} else {
		Write-Output "- $in"
	}
}
