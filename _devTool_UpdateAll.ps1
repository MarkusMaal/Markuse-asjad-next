#
# {} markuse tarkvara
#
# markuse asjade uuendamise skript
#
# NOTE: PowerShell 3.0 and later required
& "$(Split-Path $MyInvocation.MyCommand.Path)/_devTool_Logo.ps1"
$platform = [System.Environment]::OSVersion.Platform
# exclude any directories here
$exclusions = @('.vscode', 'UniplatformTest')
# full path of .mas directory
$mas_root = [environment]::getfolderpath(“userprofile”) + "/.mas"
Write-Output "Stage 1: Stopping any existing processes..."
& "$(Split-Path $MyInvocation.MyCommand.Path)/_devTool_KillAll.ps1"
# avoids race condition where DesktopIcons isn't copied over due to the fact it's still in the process of being terminated
Start-Sleep -Seconds 1.5
if ($IsMacOS) {
	Write-Output "Stage 2: Creating MacOS bundles..."
	bash ./_devTool_CreateMacBundles.sh
} else
{
	Write-Output "Stage 2: Copying executables..."
	# gets list of directories
	Get-ChildItem out -Exclude $exclusions | Foreach-Object {
		# finds the release executables and other required files
		$dir = $_.FullName
		$project = $_.Name
		Write-Output "- $project"
		if ($verbose)
		{
			Write-Output "-- Copying $n"
		}
		$n = $_.Name
		$esc = [char]27  # we explicitly define escape character for backwards compatibility with older PowerShell versions
		$destinationPath = $mas_root + "/Markuse asjad"
		# copy to .d directories if running on macOS, because otherwise everything will segfault
		if ($IsMacOS)
		{
			$destinationPath = $mas_root + "/Markuse asjad/" + $project + ".d"
		}
		Copy-Item -Path $_.FullName -Destination $destinationPath -Force
	}
}
Write-Output "Stage 3: Restarting integration program..."
$mit_path = $mas_root + "/Markuse asjad/Markuse arvuti integratsioonitarkvara"
$di_path = $mas_root + "/Markuse asjad/DesktopIcons"
if ($IsMacOS) {
	open -a "$mit_path.app"
	open -a "$di_path.app"
	exit
}
# append .exe if we're running this script under Windows
if ($platform -eq "Win32NT") {
	$mit_path = $mit_path + ".exe"
	$di_path = $di_path + ".exe"
}
Start-Process -FilePath $mit_path
Start-Process -FilePath $di_path
Write-Output "Stage 4: Removing temporary files..."
$dest_path = $mas_root + "/Markuse asjad"
Get-ChildItem $dest_path -Filter *.pdb | Foreach-Object {
	Write-Host "- " -NoNewLine
	Write-Host $_.Name
	Remove-Item -Path $_.FullName
}
