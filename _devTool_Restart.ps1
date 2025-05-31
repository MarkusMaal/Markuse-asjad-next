#
# {} markuse tarkvara
#
# markuse asjade taaskäivitamise skript
#
& "$(Split-Path $MyInvocation.MyCommand.Path)/_devTool_Logo.ps1"
$platform = [System.Environment]::OSVersion.Platform
# full path of .mas directory
$mas_root = [environment]::getfolderpath("userprofile") + "/.mas"
Write-Output "Stage 1: Stopping any existing processes..."
& "$(Split-Path $MyInvocation.MyCommand.Path)/_devTool_KillAll.ps1"
Write-Output "Stage 2: Restarting integration program..."
$mit_path = $mas_root + "/Markuse asjad/Markuse arvuti integratsioonitarkvara"
$di_path = $mas_root + "/Markuse asjad/DesktopIcons"
# append .exe if we're running this script under Windows
if ($platform -eq "Win32NT") {
	$mit_path = $mit_path + ".exe"
	$di_path = $di_path + ".exe"
}
if ($IsMacOS) {
	open -a "$mit_path.app"
	open -a "$di_path.app"
	exit
} else
{
	Start-Process -FilePath $mit_path
	Start-Process -FilePath $di_path
}
