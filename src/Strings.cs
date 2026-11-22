// User interface strings. The language follows the Windows UI language;
// anything not listed here falls back to English.
//
// To add a language: copy the English block, translate it and register it in T.All.
//
// Copyright (c) 2026 Roberto Bissanti - BSD 3-Clause License
using System.Collections.Generic;
using System.Globalization;

class Strings
{
    public string StatusOn, StatusOff, StatusStandby, StatusBusy;
    public string MenuOn, MenuOff, MenuTerminal, MenuStyle, MenuAutostart, MenuExit;
    public string StyleTerminal, StylePenguin, StylePower, StyleTerminalLed;
    public string ConfirmOnTitle, ConfirmOnText, ConfirmOffTitle, ConfirmOffText;
    public string ErrStart, ErrTerminal;
    public string PreviewActual, PreviewZoom;
}

static class T
{
    public static Strings S;

    static readonly Dictionary<string, Strings> All = new Dictionary<string, Strings>
    {
        { "en", new Strings {
            StatusOn = "WSL on",
            StatusOff = "WSL off",
            StatusStandby = "WSL shutting down (VM still in memory)",
            StatusBusy = "WSL: working...",
            MenuOn = "Turn WSL on",
            MenuOff = "Turn WSL off (free RAM)",
            MenuTerminal = "Open Linux terminal",
            MenuStyle = "Icon style",
            MenuAutostart = "Start with Windows",
            MenuExit = "Exit (WSL stays as it is)",
            StyleTerminal = "Terminal  >_",
            StylePenguin = "Penguin (Tux)",
            StylePower = "Power button",
            StyleTerminalLed = "Dark terminal with LED",
            ConfirmOnTitle = "WSL - Turn on",
            ConfirmOnText = "Turn on WSL (Linux)?\n\nThe virtual machine will use RAM for as long as it stays on.",
            ConfirmOffTitle = "WSL - Turn off",
            ConfirmOffText = "Turn off WSL?\n\nAll open Linux programs and terminals will be closed and any unsaved work in them will be lost.",
            ErrStart = "Could not start WSL:",
            ErrTerminal = "Could not open the terminal:",
            PreviewActual = "actual 16 px",
            PreviewZoom = "zoomed 4x" } },

        { "it", new Strings {
            StatusOn = "WSL acceso",
            StatusOff = "WSL spento",
            StatusStandby = "WSL in spegnimento (VM ancora in memoria)",
            StatusBusy = "WSL: operazione in corso...",
            MenuOn = "Accendi WSL",
            MenuOff = "Spegni WSL (libera la RAM)",
            MenuTerminal = "Apri terminale Linux",
            MenuStyle = "Stile icona",
            MenuAutostart = "Avvia con Windows",
            MenuExit = "Esci (WSL resta com'è)",
            StyleTerminal = "Terminale  >_",
            StylePenguin = "Pinguino (Tux)",
            StylePower = "Pulsante di accensione",
            StyleTerminalLed = "Terminale scuro con LED",
            ConfirmOnTitle = "WSL - Accensione",
            ConfirmOnText = "Vuoi accendere WSL (Linux)?\n\nLa macchina virtuale userà RAM finché resta accesa.",
            ConfirmOffTitle = "WSL - Spegnimento",
            ConfirmOffText = "Vuoi spegnere WSL?\n\nTutti i programmi e i terminali Linux aperti verranno chiusi e il lavoro non salvato al loro interno andrà perso.",
            ErrStart = "Impossibile accendere WSL:",
            ErrTerminal = "Impossibile aprire il terminale:",
            PreviewActual = "16 px reali",
            PreviewZoom = "ingrandite 4x" } },

        { "es", new Strings {
            StatusOn = "WSL encendido",
            StatusOff = "WSL apagado",
            StatusStandby = "WSL apagándose (VM aún en memoria)",
            StatusBusy = "WSL: operación en curso...",
            MenuOn = "Encender WSL",
            MenuOff = "Apagar WSL (liberar RAM)",
            MenuTerminal = "Abrir terminal de Linux",
            MenuStyle = "Estilo del icono",
            MenuAutostart = "Iniciar con Windows",
            MenuExit = "Salir (WSL queda como está)",
            StyleTerminal = "Terminal  >_",
            StylePenguin = "Pingüino (Tux)",
            StylePower = "Botón de encendido",
            StyleTerminalLed = "Terminal oscura con LED",
            ConfirmOnTitle = "WSL - Encender",
            ConfirmOnText = "¿Encender WSL (Linux)?\n\nLa máquina virtual usará RAM mientras siga encendida.",
            ConfirmOffTitle = "WSL - Apagar",
            ConfirmOffText = "¿Apagar WSL?\n\nSe cerrarán todos los programas y terminales de Linux abiertos y se perderá el trabajo no guardado en ellos.",
            ErrStart = "No se pudo encender WSL:",
            ErrTerminal = "No se pudo abrir el terminal:",
            PreviewActual = "16 px reales",
            PreviewZoom = "ampliado 4x" } },

        { "fr", new Strings {
            StatusOn = "WSL allumé",
            StatusOff = "WSL éteint",
            StatusStandby = "WSL en cours d'arrêt (VM encore en mémoire)",
            StatusBusy = "WSL : opération en cours...",
            MenuOn = "Allumer WSL",
            MenuOff = "Éteindre WSL (libérer la RAM)",
            MenuTerminal = "Ouvrir un terminal Linux",
            MenuStyle = "Style de l'icône",
            MenuAutostart = "Lancer avec Windows",
            MenuExit = "Quitter (WSL reste tel quel)",
            StyleTerminal = "Terminal  >_",
            StylePenguin = "Manchot (Tux)",
            StylePower = "Bouton d'alimentation",
            StyleTerminalLed = "Terminal sombre avec LED",
            ConfirmOnTitle = "WSL - Allumer",
            ConfirmOnText = "Allumer WSL (Linux) ?\n\nLa machine virtuelle utilisera de la RAM tant qu'elle restera allumée.",
            ConfirmOffTitle = "WSL - Éteindre",
            ConfirmOffText = "Éteindre WSL ?\n\nTous les programmes et terminaux Linux ouverts seront fermés et le travail non enregistré sera perdu.",
            ErrStart = "Impossible d'allumer WSL :",
            ErrTerminal = "Impossible d'ouvrir le terminal :",
            PreviewActual = "16 px réels",
            PreviewZoom = "agrandi 4x" } },

        { "de", new Strings {
            StatusOn = "WSL an",
            StatusOff = "WSL aus",
            StatusStandby = "WSL wird beendet (VM noch im Speicher)",
            StatusBusy = "WSL: Vorgang läuft...",
            MenuOn = "WSL einschalten",
            MenuOff = "WSL ausschalten (RAM freigeben)",
            MenuTerminal = "Linux-Terminal öffnen",
            MenuStyle = "Symbolstil",
            MenuAutostart = "Mit Windows starten",
            MenuExit = "Beenden (WSL bleibt, wie es ist)",
            StyleTerminal = "Terminal  >_",
            StylePenguin = "Pinguin (Tux)",
            StylePower = "Ein-/Aus-Taste",
            StyleTerminalLed = "Dunkles Terminal mit LED",
            ConfirmOnTitle = "WSL - Einschalten",
            ConfirmOnText = "WSL (Linux) einschalten?\n\nDie virtuelle Maschine belegt RAM, solange sie läuft.",
            ConfirmOffTitle = "WSL - Ausschalten",
            ConfirmOffText = "WSL ausschalten?\n\nAlle geöffneten Linux-Programme und -Terminals werden geschlossen; nicht gespeicherte Arbeit darin geht verloren.",
            ErrStart = "WSL konnte nicht gestartet werden:",
            ErrTerminal = "Terminal konnte nicht geöffnet werden:",
            PreviewActual = "echte 16 px",
            PreviewZoom = "4-fach vergrößert" } },

        { "pt", new Strings {
            StatusOn = "WSL ligado",
            StatusOff = "WSL desligado",
            StatusStandby = "WSL desligando (VM ainda na memória)",
            StatusBusy = "WSL: operação em andamento...",
            MenuOn = "Ligar WSL",
            MenuOff = "Desligar WSL (liberar RAM)",
            MenuTerminal = "Abrir terminal Linux",
            MenuStyle = "Estilo do ícone",
            MenuAutostart = "Iniciar com o Windows",
            MenuExit = "Sair (WSL fica como está)",
            StyleTerminal = "Terminal  >_",
            StylePenguin = "Pinguim (Tux)",
            StylePower = "Botão de ligar",
            StyleTerminalLed = "Terminal escuro com LED",
            ConfirmOnTitle = "WSL - Ligar",
            ConfirmOnText = "Ligar o WSL (Linux)?\n\nA máquina virtual usará RAM enquanto estiver ligada.",
            ConfirmOffTitle = "WSL - Desligar",
            ConfirmOffText = "Desligar o WSL?\n\nTodos os programas e terminais Linux abertos serão fechados e o trabalho não salvo neles será perdido.",
            ErrStart = "Não foi possível ligar o WSL:",
            ErrTerminal = "Não foi possível abrir o terminal:",
            PreviewActual = "16 px reais",
            PreviewZoom = "ampliado 4x" } },
    };

    /// <param name="forced">two-letter code from --lang, or null to use the Windows UI language</param>
    public static void Init(string forced)
    {
        string code = forced ?? CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        if (code != null) code = code.ToLowerInvariant();
        if (code == null || !All.TryGetValue(code, out S)) S = All["en"];
    }

    public static IEnumerable<string> Languages { get { return All.Keys; } }
}
