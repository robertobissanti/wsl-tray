// WSL Tray - pulsante luminoso nella barra delle applicazioni per accendere/spegnere WSL
// Verde = acceso, Ambra = in avvio/spegnimento o VM ancora in memoria, Rosso = spento
// Clic sinistro: accende/spegne. Clic destro: menu.
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

static class Program
{
    [DllImport("user32.dll")] static extern bool SetProcessDPIAware();

    [STAThread]
    static void Main(string[] args)
    {
        // WslTray.exe --render <cartella> : salva le icone in PNG (anteprima / documentazione)
        if (args.Length == 2 && args[0] == "--render")
        {
            TrayContext.RenderPreviews(args[1]);
            return;
        }
        bool created;
        using (var mutex = new Mutex(true, "WslTray_SingleInstance", out created))
        {
            if (!created) return;
            try { SetProcessDPIAware(); } catch { }
            Application.EnableVisualStyles();
            Application.Run(new TrayContext());
        }
    }
}

enum WslState { Off, On, Standby, Busy }

class TrayContext : ApplicationContext
{
    [DllImport("user32.dll")] static extern bool DestroyIcon(IntPtr h);

    const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string RunName = "WslTray";

    readonly NotifyIcon tray = new NotifyIcon();
    readonly System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
    readonly ToolStripMenuItem miStatus, miOn, miOff, miTerminal, miAutostart;
    Process keepAlive;             // processo "sleep infinity" che tiene viva la distro
    WslState state = WslState.Off;
    bool busy;                     // operazione di accensione/spegnimento in corso
    bool polling;
    IntPtr currentIcon = IntPtr.Zero;

    public TrayContext()
    {
        var menu = new ContextMenuStrip();
        miStatus = new ToolStripMenuItem("WSL: ...") { Enabled = false };
        miOn = new ToolStripMenuItem("Accendi WSL", null, delegate { TurnOn(); });
        miOff = new ToolStripMenuItem("Spegni WSL (libera la RAM)", null, delegate { TurnOff(); });
        miTerminal = new ToolStripMenuItem("Apri terminale Linux", null, delegate { OpenTerminal(); });
        miAutostart = new ToolStripMenuItem("Avvia con Windows", null, delegate { ToggleAutostart(); });
        miAutostart.Checked = IsAutostart();
        menu.Items.Add(miStatus);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(miOn);
        menu.Items.Add(miOff);
        menu.Items.Add(miTerminal);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(miAutostart);
        menu.Items.Add(new ToolStripMenuItem("Esci (WSL resta com'e')", null, delegate { Exit(); }));

        tray.ContextMenuStrip = menu;
        tray.MouseClick += delegate (object s, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || busy || asking) return;
            if (state == WslState.Off) TurnOn(); else TurnOff();
        };
        tray.Visible = true;
        Apply(WslState.Off, null, null);

        timer.Interval = 3000;
        timer.Tick += delegate { Poll(); };
        timer.Start();
        Poll();
    }

    // ---------- stato ----------

    void Poll()
    {
        if (polling || busy) return;
        polling = true;
        ThreadPool.QueueUserWorkItem(delegate
        {
            string running = RunWsl("-l --running -q", 8000);
            bool vmAlive = Process.GetProcessesByName("vmmemWSL").Length > 0;
            long ramMb = VmRamMb();
            WslState s;
            if (!string.IsNullOrEmpty(running)) s = WslState.On;
            else if (vmAlive) s = WslState.Standby;
            else s = WslState.Off;
            try
            {
                tray.ContextMenuStrip.BeginInvoke((MethodInvoker)delegate
                {
                    polling = false;
                    if (!busy) Apply(s, running, ramMb > 0 ? (long?)ramMb : null);
                });
            }
            catch { polling = false; }
        });
    }

    static long VmRamMb()
    {
        long total = 0;
        foreach (var p in Process.GetProcessesByName("vmmemWSL"))
        {
            try { total += p.WorkingSet64; } catch { }
            p.Dispose();
        }
        return total / (1024 * 1024);
    }

    void Apply(WslState s, string distros, long? ramMb)
    {
        state = s;
        string text;
        switch (s)
        {
            case WslState.On:
                text = "WSL acceso";
                string d = (distros ?? "").Replace("\r", "").Trim().Replace("\n", ", ");
                if (d.Length > 0) text += " - " + d;
                break;
            case WslState.Standby: text = "WSL in spegnimento (VM ancora in memoria)"; break;
            case WslState.Busy: text = "WSL: operazione in corso..."; break;
            default: text = "WSL spento"; break;
        }
        if (ramMb.HasValue && s != WslState.Off) text += string.Format(" - RAM {0:0.0} GB", ramMb.Value / 1024.0);

        miStatus.Text = text;
        tray.Text = text.Length > 63 ? text.Substring(0, 63) : text;
        miOn.Enabled = (s == WslState.Off || s == WslState.Standby) && !busy;
        miOff.Enabled = s != WslState.Off && !busy;
        SetIcon(s);
    }

    // ---------- azioni ----------

    bool asking;   // evita due finestre di conferma contemporanee

    bool Confirm(string question, string title)
    {
        if (asking) return false;
        asking = true;
        try
        {
            // finestra invisibile "sempre in primo piano" come proprietaria,
            // cosi' la conferma non resta nascosta dietro altre finestre
            using (var owner = new Form())
            {
                owner.TopMost = true;
                owner.ShowInTaskbar = false;
                owner.StartPosition = FormStartPosition.Manual;
                owner.Location = new Point(-32000, -32000);
                owner.Size = new Size(1, 1);
                owner.Show();
                owner.Activate();
                return MessageBox.Show(owner, question, title, MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes;
            }
        }
        finally { asking = false; }
    }

    void TurnOn()
    {
        if (busy) return;
        if (!Confirm("Vuoi accendere WSL (Linux)?\n\nLa macchina virtuale usera' fino a 6 GB di RAM finche' resta accesa.",
                     "WSL - Accensione")) return;
        busy = true;
        Apply(WslState.Busy, null, null);
        try
        {
            // Un processo che dorme per sempre tiene accesa la distro predefinita
            var psi = new ProcessStartInfo("wsl.exe", "--exec sleep infinity");
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            psi.WindowStyle = ProcessWindowStyle.Hidden;
            keepAlive = Process.Start(psi);
        }
        catch (Exception ex)
        {
            Notify("Impossibile accendere WSL: " + ex.Message, ToolTipIcon.Error);
        }
        FinishAfter(2500);
    }

    void TurnOff()
    {
        if (busy) return;
        if (!Confirm("Vuoi spegnere WSL?\n\nTutti i programmi e i terminali Linux aperti verranno chiusi " +
                     "e il lavoro non salvato al loro interno andra' perso.",
                     "WSL - Spegnimento")) return;
        busy = true;
        Apply(WslState.Busy, null, null);
        ThreadPool.QueueUserWorkItem(delegate
        {
            RunWsl("--shutdown", 30000);
            try { if (keepAlive != null && !keepAlive.HasExited) keepAlive.Kill(); } catch { }
            keepAlive = null;
            tray.ContextMenuStrip.BeginInvoke((MethodInvoker)delegate { FinishAfter(500); });
        });
    }

    void FinishAfter(int ms)
    {
        var t = new System.Windows.Forms.Timer();
        t.Interval = ms;
        t.Tick += delegate { t.Stop(); t.Dispose(); busy = false; Poll(); };
        t.Start();
    }

    void OpenTerminal()
    {
        try
        {
            // Windows Terminal se presente, altrimenti la console classica
            try { Process.Start(new ProcessStartInfo("wt.exe", "wsl.exe ~") { UseShellExecute = true }); }
            catch { Process.Start(new ProcessStartInfo("wsl.exe", "~") { UseShellExecute = true }); }
            FinishAfter(2000);
        }
        catch (Exception ex) { Notify("Impossibile aprire il terminale: " + ex.Message, ToolTipIcon.Error); }
    }

    void Exit()
    {
        timer.Stop();
        tray.Visible = false;
        tray.Dispose();
        if (currentIcon != IntPtr.Zero) DestroyIcon(currentIcon);
        // keepAlive non viene chiuso: uscire dal programma non spegne WSL
        ExitThread();
    }

    void Notify(string msg, ToolTipIcon icon)
    {
        tray.ShowBalloonTip(4000, "WSL", msg, icon);
    }

    // ---------- avvio con Windows ----------

    static bool IsAutostart()
    {
        using (var k = Registry.CurrentUser.OpenSubKey(RunKey))
            return k != null && k.GetValue(RunName) != null;
    }

    void ToggleAutostart()
    {
        using (var k = Registry.CurrentUser.CreateSubKey(RunKey))
        {
            if (IsAutostart()) k.DeleteValue(RunName, false);
            else k.SetValue(RunName, "\"" + Application.ExecutablePath + "\"");
        }
        miAutostart.Checked = IsAutostart();
    }

    // ---------- utilita' ----------

    static string RunWsl(string args, int timeoutMs)
    {
        try
        {
            var psi = new ProcessStartInfo("wsl.exe", args);
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.EnvironmentVariables["WSL_UTF8"] = "1";   // output in UTF-8 invece di UTF-16
            psi.StandardOutputEncoding = System.Text.Encoding.UTF8;
            using (var p = Process.Start(psi))
            {
                string o = p.StandardOutput.ReadToEnd();
                p.WaitForExit(timeoutMs);
                if (p.ExitCode != 0) return "";
                return o.Replace("\0", "").Trim();
            }
        }
        catch { return ""; }
    }

    // ---------- icona: pulsante luminoso con simbolo IEC "I/O" ----------

    void SetIcon(WslState s)
    {
        Color c;
        switch (s)
        {
            case WslState.On: c = Color.FromArgb(34, 197, 94); break;        // verde
            case WslState.Standby:
            case WslState.Busy: c = Color.FromArgb(245, 158, 11); break;     // ambra
            default: c = Color.FromArgb(220, 38, 38); break;                // rosso
        }
        int size = Math.Max(16, SystemInformation.SmallIconSize.Width);
        using (var bmp = DrawButton(size, c))
        {
            IntPtr h = bmp.GetHicon();
            tray.Icon = Icon.FromHandle(h);
            if (currentIcon != IntPtr.Zero) DestroyIcon(currentIcon);
            currentIcon = h;
        }
    }

    public static void RenderPreviews(string dir)
    {
        System.IO.Directory.CreateDirectory(dir);
        var states = new[] {
            new { Name = "off",     C = Color.FromArgb(220, 38, 38) },
            new { Name = "on",      C = Color.FromArgb(34, 197, 94) },
            new { Name = "standby", C = Color.FromArgb(245, 158, 11) } };
        foreach (var st in states)
            foreach (int sz in new[] { 16, 24, 32, 128 })
                using (var b = DrawButton(sz, st.C))
                    b.Save(System.IO.Path.Combine(dir, st.Name + "-" + sz + ".png"), System.Drawing.Imaging.ImageFormat.Png);
    }

    static Bitmap DrawButton(int size, Color c)
    {
        var bmp = new Bitmap(size, size);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            float s = size;

            // alone luminoso
            using (var glowPath = new GraphicsPath())
            {
                glowPath.AddEllipse(0, 0, s - 1, s - 1);
                using (var glow = new PathGradientBrush(glowPath))
                {
                    glow.CenterColor = Color.FromArgb(150, c);
                    glow.SurroundColors = new[] { Color.FromArgb(0, c) };
                    g.FillEllipse(glow, 0, 0, s - 1, s - 1);
                }
            }

            // corpo del pulsante: sfera con riflesso
            float m = s * 0.10f;
            var body = new RectangleF(m, m, s - 2 * m - 1, s - 2 * m - 1);
            using (var bodyPath = new GraphicsPath())
            {
                bodyPath.AddEllipse(body);
                using (var br = new PathGradientBrush(bodyPath))
                {
                    br.CenterPoint = new PointF(body.X + body.Width * 0.38f, body.Y + body.Height * 0.32f);
                    br.CenterColor = Lighten(c, 0.55f);
                    br.SurroundColors = new[] { Darken(c, 0.35f) };
                    g.FillEllipse(br, body);
                }
            }
            using (var rim = new Pen(Darken(c, 0.55f), Math.Max(1f, s / 20f)))
                g.DrawEllipse(rim, body);

            // simbolo di accensione (cerchio aperto + barra)
            float pw = Math.Max(1.6f, s / 9f);
            float r = body.Width * 0.27f;
            float cx = body.X + body.Width / 2f, cy = body.Y + body.Height / 2f + s * 0.02f;
            using (var pen = new Pen(Color.White, pw))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                g.DrawArc(pen, cx - r, cy - r, 2 * r, 2 * r, -55, 290);
                g.DrawLine(pen, cx, cy - r * 1.35f, cx, cy - r * 0.15f);
            }
        }
        return bmp;
    }

    static Color Lighten(Color c, float k)
    {
        return Color.FromArgb(c.R + (int)((255 - c.R) * k), c.G + (int)((255 - c.G) * k), c.B + (int)((255 - c.B) * k));
    }

    static Color Darken(Color c, float k)
    {
        return Color.FromArgb((int)(c.R * (1 - k)), (int)(c.G * (1 - k)), (int)(c.B * (1 - k)));
    }
}
