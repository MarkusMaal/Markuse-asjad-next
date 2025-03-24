![Markuse asjad logo](logo.png)
# Markuse asjad next

## Süsteemnõuded

* Markuse asjad nõuetele vastav süsteem kehtiva Verifile 2.0 räsiga
* dotnet-sdk-8.0 või muu ühilduv lahendus kompileerimiseks
* Microsoft Powershell devToolide kasutamiseks
* Avalonia UI pluginad vastava IDE jaoks (Microsoft Visual Studio või JetBrains Rider)


## PowerShell paigaldamine

Kui dotnet-sdk olemas, saab käivitada käsureal: `dotnet tool install --global PowerShell`

Vastasel korral, saate lugeda [Microsofti saidilt](https://learn.microsoft.com/en-us/powershell/scripting/install/installing-powershell?view=powershell-7.5), kuidas seda teha.


## Kompileerimine ja binraaride uuendamine

Kõik rakendused on järjest võimalik kompileerida, kasutades devTool menüüd. Et avada devTool menüü, sisesta käsureal: `pwsh _devTool_Menu.ps1` või kui olete juba PowerShellis, siis `.\_devTool_Menu.ps1`.

Seejärel peaks avama järgmine menüü:

```
         ●
        ● ●   markuse arvuti asjad
         ●

1. Kill processes
2. Restart processes
3. Show projects
4. Update binaries
5. Build solution
6. Git commit
7. Change colors: 2
8. Verbose mode: False
9. Exit

DevTool v1.2.1

Ends all Markuse asjad processes, which may not conflict with new binaries
```

Saate menüüs navigeerida numbrite või nooleklahvidega ning valiku kinnitada vajutades sisestusklahvi (ENTER).

Kompileerimiseks valige `5. Build solution`. See kompileerib kõik rakendused teie opsüsteemile ja riistvarale sobivateks binraarideks out/ kausta.

Et näha millised rakendused on edukalt kompileeritud, kasutage menüüvalikut `3. Show projects`. Iga edukalt kompileeritud rakenduse taga peaks olema kiri `[Build OK]`.

Binraaride uuendamiseks valige menüüst `4. Update binaries`. See sulgeb kõik avatud rakendused automaatselt ning avab need pärast uute binraaride kopeerimist ka uuesti.

## macOS .app konteinerite loomine

1. Kustutage enne alustamist out/ kataloogi sisu
2. Ehitage lahendus _devTool_publish.ps1 skriptiga (PowerShellis)
3. Genereerige .app konteinerid käivitades bashis _devTool_CreateMacBundles.sh (NB: see skript üritab ka kopeerida binraarid automaatselt "/home/$USER/.mas/Markuse asjad" kataloogi)
4. Ärge kasutage _devTool_UpdateAll.ps1 skripti või _devTool_Menu.ps1-s "Update binaries" valikut macOSis!!!
5. Käivita _devTool_KillAll.ps1 skript, et peatada jooksvad Markuse asjade rakendused
6. Kui avate nüüd out/ kataloogi Finderiga, näete seal genereeritud konteinereid, mida saate avada või liigutada Applications kausta
7. Kuna need binraarid ei ole märgistatud Apple poolt, siis peate iga rakenduse esimesel käivitamisel tegema järgnevat:
   1. Lockdown peab sätetes olema välja lülitatud
   2. Tuleb valida "Open anyway" sätete rakenduses "Privacy and Security" alt
8. Käivita _devTool_Restart.ps1 skript, et Markuse arvuti asjade tarkvara taaskäivitada
