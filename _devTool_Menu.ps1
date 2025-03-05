

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

function Main {  # main function
	$esc = [char]27  # we explicitly define escape character for backwards compatibility with older PowerShell versions
	$setCursorTop = "$esc[0;0H"  # moves caret to top-left (0, 0)
	$sel = 1  # menu selection
    $verbose = False # verbose mode toggle
    $verbose = !$verbose
    $verbose = !$verbose
	$bgs = @('Gray', 'Green', 'Blue', 'Black', 'Blue')  # highlight background colors
	$fgs = @('Black', 'Black', 'White', 'Green', 'Black')     # highlight foreground colors
	$cidx = 2  # color index
	Clear-Host                         # clear screen
	Write-Host "$esc[?25l" -NoNewLine  # hide caret
	while ($false -eq [bool]@(0)) {    # always true
        & "./_devTool_Logo.ps1" # header
		$menuitems = @('Kill processes', 'Restart processes', 'Show projects', 'Update binaries', 'Build solution', 'Git commit', "Change colors: $cidx", "Verbose mode: $verbose", 'Exit')  # displayable menu items
	    $actions = @('Run-Script _devTool_KillAll.ps1', 'Run-Script _devTool_Restart.ps1', 'Run-Script _devTool_ShowProjects.ps1', 'Run-Script _devTool_UpdateAll.ps1 -Verbose $verbose', 'Run-Script _devTool_PublishProjects.ps1', 'Perform-GitCommit', '$cidx++', '$verbose = !$verbose', 'Exit-Now')  # actions, which are related to menuitems
        $hints = @('Ends all Markuse asjad processes, which may not conflict with new binaries              ', 'Restarts Markuse asjad processes for maintenance purposes                               ', 'Displays all available projects on this solution                                        ', 'Upgrades all executables in .mas/Markuse asjad directory                                ', 'Attempts to build all projects for the current platform                                 ', 'Allows you to commit and push git changes                                               ', 'Changes color scheme in this script                                                     ', 'When updating binaries, while this option is enabled, all copied files will be displayed', 'Exits the script                                                                        ')
	    $i = 1
			foreach ($item in $menuitems) {  # display all menu items and highlight the selected one
			if ($sel -eq $i) {
                Write-Host -BackgroundColor $bgs[$cidx] -ForegroundColor $fgs[$cidx] "$i. $item" -NoNewline
                Write-Host " "
            }
			if ($sel -ne $i) { Write-Host "$i. $item " }
			$i++
		}
        # footer
        $hint = $hints[$sel-1]
        Write-Host "`r`nDevTool v1.2`r`n`r`n$hint"
		Write-Host "${setCursorTop}" -NoNewLine
		$KE = [System.Console]::ReadKey()               # wait for user to press a key, then save that as an object
		Write-Host "${setCursorTop}" -NoNewLine
        Write-Host " "                                  # <-- if the user presses a number key, make sure to clear it from screen immediately
		Write-Host "${setCursorTop}" -NoNewline         
		$K = $KE.Key                                    # get Key value from $KE object
		if ($K -eq "DownArrow") { $sel++ }              # move selection up/down if arrow up/down are pressed
		if ($K -eq "UpArrow") { $sel-- }
        for($i=1; $i -lt 10; $i++) {
		    if ($K -eq "D$i") { $sel = $i }
		    if ($K -eq "NumPad$i") { $sel = $i }
        }
		if ($K -eq "Enter") { $actions[$sel-1] | iex }  # pipe selected action to iex to run the string as code through the interpreter if Enter is pressed
		if ($sel -gt $menuitems.Length) { $sel = 1 }    
		if ($sel -lt 1) { $sel = $menuitems.Length }    # check for selection overflows/underflows
		if ($cidx -ge $bgs.Length) { $cidx = 0 }        
	}
}

Main
