
$verbose = $false # verbose mode toggle
$showlogo = $true # logo visibility
$showhints = $true # hints visibility
$showversion = $true # version visibility
$crossbuild = $false # cross building
$bgs = @('Gray', 'Green', 'Blue', 'Black', 'Blue', 'Red', 'Black', 'Yellow', 'Black', 'Cyan', 'Black', 'Black')  # highlight background colors
$fgs = @('Black', 'Black', 'White', 'Green', 'Black', 'White', 'Red', 'Black', 'Yellow', 'Black', 'Cyan', 'Magenta')     # highlight foreground colors
$cidx = 2  # color index
$this = $MyInvocation.MyCommand.Path
if (Test-Path "DevTool.json" -PathType Leaf) {
	$settings = Get-Content -Path "DevTool.json" | ConvertFrom-Json
	$cidx = $settings.Theme
	$verbose = $settings.Verbose
	$showlogo = $settings.ShowLogo
	$showhints = $settings.ShowHints
	$showversion = $settings.ShowVersion
	$crossbuild = $settings.CrossBuild
}

function Exit-Now {  # tidy everything up and exit the script; usage: Exit-Now
	$esc = [char]27
	Write-Host "$esc[?25h"
	Clear-Host
	exit
}

function Run-Script {
	param (
		$Script
	)
    ClearScreen
    & "./$Script"
    Pause
    Clear-Host
    ClearScreen
}

function ClearScreen {
	$esc = [char]27
	Write-Host "$esc[?25h"
	Clear-Host
}

function Get-GitStatus {
    ClearScreen
    git status
    Pause
    ClearScreen
}

function ListMenu {
	[CmdletBinding()]
	param (
		$MenuItems,
		$Sel
	)
	if ($Sel -is [Object[]]) {
		$Sel = 1
	}
	$i = 1
	foreach ($item in $MenuItems) {  # display all menu items and highlight the selected one
		if ($Sel -eq $i) {
			Write-Host -BackgroundColor $bgs[$cidx] -ForegroundColor $fgs[$cidx] "$i. $item" -NoNewline
			Write-Host " "
		}
		if ($Sel -ne $i) { Write-Host "$i. $item " }
		$i++
	}
}

function ShowHint {
    [CmdletBinding()]
	param (
		$Hints,
		$Sel
	)
	if ($Sel -is [Object[]]) {
		$Sel = 1
	}
	$hint = $Hints[$Sel-1]
	if ($showversion) { Write-Host "`r`nDevTool v1.3.2" }
	if ($showhints) { Write-Host "`r`n$hint" }
}

function Perform-GitCommit {
    ClearScreen
    & "./_devTool_Logo.ps1" # header
    $rem = git remote | Out-String
    $brnch = git branch | Out-String
    $message = Read-Host "Please enter Git message"
    ClearScreen
    & "./_devTool_Logo.ps1" # header
    Write-Host "Pushing to remote: $rem" -NoNewline
    Write-Host "Branch: $brnch" -NoNewline
    Write-Host "Commit message: $message"
    git add .
    git commit -m "$message"
    git push
    Pause
    ClearScreen
}

function MenuPrompt {
    [CmdletBinding()]
	param (
		$Actions,
		$MenuItems,
		$Sel
	)
	if ($Sel -is [Object[]]) {
		$Sel = 1
	}
	$setCursorTop = "$esc[0;0H"  # moves caret to top-left (0, 0)
	Write-Host "${setCursorTop}" -NoNewLine
	$KE = [System.Console]::ReadKey()               # wait for user to press a key, then save that as an object
	Write-Host "${setCursorTop}" -NoNewLine
	Write-Host " "                                  # <-- if the user presses a number key, make sure to clear it from screen immediately
	Write-Host "${setCursorTop}" -NoNewline
	$K = $KE.Key                                    # get Key value from $KE object
	if ($K -eq "DownArrow") { $Sel++ }              # move selection up/down if arrow up/down are pressed
	if ($K -eq "UpArrow") { $Sel-- }
	for($i=1; $i -lt 10; $i++) {
		if ($K -eq "D$i") { $Sel = $i }
		if ($K -eq "NumPad$i") { $Sel = $i }
	}
	if ($K -eq "Enter") {
		return $Actions[$Sel-1]
	}  # pipe selected action to iex to run the string as code through the interpreter if Enter is pressed
	if ($Sel -gt $MenuItems.Length) { $Sel = 1 }
	if ($Sel -lt 1) { $Sel = $MenuItems.Length }    # check for selection overflows/underflows
	return $Sel
}

function ResetSettings {
	$cidx = 2
	$verbose = $false # verbose mode toggle
	$showlogo = $true # logo visibility
	$showhints = $true # hints visibility
	$showversion = $true # version visibility
	Remove-Item "DevTool.json"
	& $this -Settings
	exit
}


function Settings {
	$esc = [char]27  # we explicitly define escape character for backwards compatibility with older PowerShell versions
	$setCursorTop = "$esc[0;0H"  # moves caret to top-left (0, 0)
	$actions = @('$cidx++', '$verbose = !$verbose', '$showlogo = !$showlogo', '$showversion = !$showversion', '$showhints = !$showhints', '$crossbuild = !$crossbuild', 'ResetSettings', 'break')  # actions, which are related to menuitems
	$hints = @('Changes color scheme in this script                                                     ', 'When updating binaries, while this option is enabled, all copied files will be displayed', 'Toggles the visibility of the logo                                                      ', 'Toggles the visibility of the version number                                            ', 'Toggles the visibility of these hint texts                                              ', 'Allows you to build the solution for every supported platform                           ', 'Resets all settings to default values                                                   ', 'Returns to previous menu                                                                ')
	$sel = 1
	Clear-Host
	Write-Host "$esc[?25l" -NoNewLine  # hide caret
	while ($false -eq [bool]@(0)) {    # always true
		$menuitems = @("Change colors: $cidx", "Verbose mode: $verbose", "Show logo: $showlogo","Show version: $showversion","Show hints: $showhints", "Cross-build: $crossbuild", "Reset settings", 'Go back')  # displayable menu items
		& "./_devTool_Logo.ps1" # header
		ListMenu -MenuItems $menuitems -Sel $sel
        # footer
        ShowHint -Hints $hints -Sel $sel
        $lastsel = $sel
		$sel = MenuPrompt -Actions $actions -MenuItems $menuitems -Sel $sel
		if ($sel -is [string]) {
			$action = $sel
			$sel = $lastsel
			$action | iex
			if ($cidx -ge $bgs.Length) { $cidx = 0 }
		}
	}
	Clear-Host
	$settings = @{
		Theme = $cidx
		Verbose = $verbose
		ShowLogo = $showlogo
		ShowVersion = $showversion
		ShowHints = $showhints
		CrossBuild = $crossbuild
	}
	$settings | ConvertTo-Json | Set-Content -Path "DevTool.json"
	$menuitems = @()
	& $this
	exit
}

function Main {  # main function
	$esc = [char]27  # we explicitly define escape character for backwards compatibility with older PowerShell versions
	$sel = 1  # menu selection
	Clear-Host                         # clear screen
	Write-Host "$esc[?25l" -NoNewLine  # hide caret
	while ($false -eq [bool]@(0)) {    # always true
        & "./_devTool_Logo.ps1" # header
		$menuitems = @('Kill processes', 'Restart processes', 'Show projects', 'Update binaries', 'Build solution', 'Clean solution', "Settings", 'Exit')  # displayable menu items
	    $actions = @('Run-Script _devTool_KillAll.ps1', 'Run-Script _devTool_Restart.ps1', 'Run-Script _devTool_ShowProjects.ps1', 'Run-Script _devTool_UpdateAll.ps1 -Verbose $verbose', 'Run-Script _devTool_PublishProjects.ps1', 'Run-Script _devTool_Clean.ps1', 'Settings', 'Exit-Now')  # actions, which are related to menuitems
        $hints = @('Ends all Markuse asjad processes, which may be in conflict with new binaries            ', 'Restarts Markuse asjad processes for maintenance purposes                               ', 'Displays all available projects on this solution                                        ', 'Upgrades all executables in .mas/Markuse asjad directory                                ', 'Attempts to build all projects for the current platform                                 ', 'Deletes leftover files from building/debugging to free up disk space                    ', 'Change devTool settings                                                                 ', 'Exits the script                                                                        ')
		ListMenu -MenuItems $menuitems -Sel $sel
        # footer
        ShowHint -Hints $hints -Sel $sel
        $lastsel = $sel
		$sel = MenuPrompt -Actions $actions -MenuItems $menuitems -Sel $sel
		if ($sel -is [string]) {
			$action = $sel
			$sel = $lastsel
			$action | iex
			Write-Host "$esc[?25l" -NoNewLine  # hide caret
		}
	}
}


if ([Boolean]([Environment]::GetCommandLineArgs() -match "-Settings")) {
	Settings
} else {
	Main
}
