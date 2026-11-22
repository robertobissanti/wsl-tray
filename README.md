# WSL Tray

Un pulsante luminoso nell'area di notifica di Windows per vedere a colpo d'occhio se **WSL** è acceso e per accenderlo o spegnerlo con un clic. Così Linux non tiene occupata la RAM quando non serve.

| Spento | Acceso | In spegnimento |
|:---:|:---:|:---:|
| <img src="docs/terminal-off.png" width="64"> | <img src="docs/terminal-on.png" width="64"> | <img src="docs/terminal-standby.png" width="64"> |

Lo stile dell'icona si sceglie da tasto destro → **Stile icona**, e la scelta viene ricordata. Gli stili sono quattro: terminale `>_` (predefinito), pinguino Tux, pulsante di accensione e terminale scuro con LED.

![confronto stili](docs/confronto-stili.png)

- **Rosso**: WSL è spento e non usa RAM.
- **Verde**: una distribuzione Linux è in esecuzione. Il tooltip mostra quale e quanta RAM usa la VM.
- **Ambra**: è in corso un'accensione o uno spegnimento, oppure nessuna distro è attiva ma la VM è ancora in memoria (si spegne da sola dopo `vmIdleTimeout`).

## Uso

- **Clic sinistro**: accende o spegne WSL. Chiede **sempre conferma** prima di farlo.
- **Clic destro**: menu con stato, Accendi, Spegni, Apri terminale Linux, Stile icona, Avvia con Windows ed Esci.
- **Esci** chiude solo l'icona e lascia WSL nello stato in cui si trova.

Per l'accensione, WSL Tray avvia nella distribuzione predefinita un processo `sleep infinity` nascosto, così la distro resta accesa. Per lo spegnimento esegue `wsl --shutdown`, che chiude tutte le distribuzioni e la VM e restituisce la RAM a Windows.

Lo stato si aggiorna ogni 3 secondi con `wsl -l --running -q` e controllando il processo `vmmemWSL`.

## Compilazione

Non serve installare nulla: usa il compilatore C# incluso in Windows (.NET Framework 4.x).

```powershell
.\build.ps1          # crea bin\WslTray.exe
.\bin\WslTray.exe
```

Per farlo partire all'accesso usa la voce **Avvia con Windows** nel menu. La voce scrive in `HKCU\...\Run` e non servono privilegi di amministratore.

## Suggerimento: limita la RAM di WSL

Senza configurazione, WSL 2 può usare fino al 50% della RAM. Un esempio di `%USERPROFILE%\.wslconfig`:

```ini
[wsl2]
memory=6GB
swap=4GB
vmIdleTimeout=60000

[experimental]
autoMemoryReclaim=gradual
sparseVhd=true
```

## Requisiti

- Windows 10/11 con WSL 2
- .NET Framework 4.x, già presente in Windows

## Licenza

[BSD 3-Clause](LICENSE) © 2026 Roberto Bissanti
