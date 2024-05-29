# exclude any directories here
$exclusions = @('.vscode', 'UniplatformTest')
# loop through project directories and attempt to kill processes
Get-ChildItem . -Directory -Exclude $exclusions | Foreach-Object {
	# this stores just the directory name, not the full path
	$pn = $_.Name
	# we pass SilentlyContinue, otherwise PS spit errors at us if the process doesn't exist
	$p = Get-Process -Name $pn -ErrorAction SilentlyContinue
	# check if process is running, if it is then stop it
	if ($p) {
		Stop-Process -InputObject $p -Force
		Write-Output "- Killed $pn"
	} else {
		Write-Output "- Skipped $pn"
	}
}
