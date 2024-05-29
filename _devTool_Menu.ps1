

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
    git show
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
    $confirm = false
    $cancel = false
    while (!$confirm) {
        $title = 'Commit message'
        $question = 'Append more lines or commit?'
        $choices = 'Conti&nue', '&Commit', 'C&ancel'
        $decision = $Host.UI.PromptForChoice($title, $question, $choices, 0)
        if ($decision -eq 0) {
            ClearScreen
            & "./_devTool_Logo.ps1" # header
            $msg2 = Read-Host "Please enter Git message"
            $message = "$message\n$msg2"
            ClearScreen
            & "./_devTool_Logo.ps1" # header
            Write-Host "Pushing to remote: $rem" -NoNewline
            Write-Host "Commit message: $message"
            $confirm = false
        }
        if ($decision -eq 1) {
            $confirm = true
        }
        if ($decision -eq 2) {
            $cancel = true
            $confirm = true
        }
    }
    if (!$cancel) {
        git add .
        git commit -m "$message"
        git push
        Pause
    } else {
        Write-Host "Cancelled!!!"
        Pause
    }
    ClearScreen
}

function Main {  # main function; usage: Main -Header [string]
	$esc = [char]27  # we explicitly define escape character for backwards compatibility with older PowerShell versions
	$setCursorTop = "$esc[0;0H"  # moves caret to top-left (0, 0)
	$sel = 1  # menu selection
	$false = [bool]@(0)
	$bgs = @('Gray', 'Green', 'Blue', 'Black')  # highlight background colors
	$fgs = @('Black', 'Black', 'White', 'Green')     # highlight foreground colors
	$cidx = 2  # color index
	$menuitems = @('Kill processes', 'Restart processes', 'Show projects', 'Update binaries', 'Git changes', 'Git commit', 'Change colors', 'Exit')  # displayable menu items
	$actions = @('Run-Script _devTool_KillAll.ps1', 'Run-Script _devTool_Restart.ps1', 'Run-Script _devTool_ShowProjects.ps1', 'Run-Script _devTool_UpdateAll.ps1', 'Get-GitStatus', 'Perform-GitCommit', '$cidx++', 'Exit-Now')  # actions, which are related to menuitems
	Clear-Host                         # clear screen
	Write-Host "$esc[?25l" -NoNewLine  # hide caret
	while ($false -eq [bool]@(0)) {    # always true
        & "./_devTool_Logo.ps1" # header
		$i = 1
			foreach ($item in $menuitems) {  # display all menu items and highlight the selected one
			if ($sel -eq $i) { Write-Host -BackgroundColor $bgs[$cidx] -ForegroundColor $fgs[$cidx] "$item" }
			if ($sel -ne $i) { Write-Host "$item" }
			$i++
		}
		Write-Host "${setCursorTop}" -NoNewLine
		$KE = [System.Console]::ReadKey()               # wait for user to press a key, then save that as an object
		$K = $KE.Key                                    # get Key value from $KE object
		if ($K -eq "DownArrow") { $sel++ }              # move selection up/down if arrow up/down are pressed
		if ($K -eq "UpArrow") { $sel-- }
		if ($K -eq "Enter") { $actions[$sel-1] | iex }  # pipe selected action to iex to run the string as code through the interpreter if Enter is pressed
		if ($sel -gt $menuitems.Length) { $sel = 1 }    
		if ($sel -lt 1) { $sel = $menuitems.Length }    # check for selection overflows/underflows
		if ($cidx -ge $bgs.Length) { $cidx = 0 }        
	}
}

Main