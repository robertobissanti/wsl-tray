// WSL Tray - glowing button in the Windows notification area to turn WSL on/off
// Green = on, Amber = starting/stopping or VM still in memory, Red = off
// Left click: toggle (asks for confirmation). Right click: menu.
//
// Copyright (c) 2026 Roberto Bissanti - BSD 3-Clause License
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
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
        // WslTray.exe --render <folder> : saves the icons as PNG (previews / documentation)
        // WslTray.exe --lang <it|en|es|fr|de|pt> : forces a language (otherwise: Windows UI language)
        string lang = null;
        for (int i = 0; i + 1 < args.Length; i++)
            if (args[i] == "--lang") lang = args[i + 1];
        T.Init(lang);

        if (args.Length >= 2 && args[0] == "--render")
        {
            IconPainter.RenderPreviews(args[1]);
            return;
        }
        // WslTray.exe --ico <file.ico> : writes the application icon (used by build.ps1)
        if (args.Length >= 2 && args[0] == "--ico")
        {
            IconPainter.WriteAppIcon(args[1]);
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
enum IconStyle { Terminal, Penguin, Power, TerminalLed }

class TrayContext : ApplicationContext
{
    [DllImport("user32.dll")] static extern bool DestroyIcon(IntPtr h);

    const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string RunName = "WslTray";
    const string SettingsKey = @"Software\WslTray";

    readonly NotifyIcon tray = new NotifyIcon();
    readonly System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
    readonly Control ui = new Control();   // bridge to the UI thread (handle created up front)
    readonly ToolStripMenuItem miStatus, miOn, miOff, miTerminal, miAutostart, miStyle;
    Process keepAlive;             // "sleep infinity" process that keeps the distro alive
    WslState state = WslState.Off;
    IconStyle style;
    bool busy;                     // a start/stop operation is in progress
    bool asking;                   // a confirmation dialog is open
    bool polling;
    IntPtr currentIcon = IntPtr.Zero;

    public TrayContext()
    {
        // Force handle creation: without it, BeginInvoke from worker threads fails
        IntPtr unused = ui.Handle;

        style = LoadStyle();

        var menu = new ContextMenuStrip();
        miStatus = new ToolStripMenuItem("WSL: ...") { Enabled = false };
        miOn = new ToolStripMenuItem(T.S.MenuOn, null, delegate { TurnOn(); });
        miOff = new ToolStripMenuItem(T.S.MenuOff, null, delegate { TurnOff(); });
        miTerminal = new ToolStripMenuItem(T.S.MenuTerminal, null, delegate { OpenTerminal(); });
        miStyle = new ToolStripMenuItem(T.S.MenuStyle);
        AddStyle(T.S.StyleTerminal, IconStyle.Terminal);
        AddStyle(T.S.StylePenguin, IconStyle.Penguin);
        AddStyle(T.S.StylePower, IconStyle.Power);
        AddStyle(T.S.StyleTerminalLed, IconStyle.TerminalLed);
        miAutostart = new ToolStripMenuItem(T.S.MenuAutostart, null, delegate { ToggleAutostart(); });
        miAutostart.Checked = IsAutostart();

        menu.Items.Add(miStatus);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(miOn);
        menu.Items.Add(miOff);
        menu.Items.Add(miTerminal);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(miStyle);
        menu.Items.Add(miAutostart);
        menu.Items.Add(new ToolStripMenuItem(T.S.MenuExit, null, delegate { Exit(); }));

        tray.ContextMenuStrip = menu;
        tray.MouseClick += delegate (object s, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || busy || asking) return;
            if (state == WslState.Off) TurnOn(); else TurnOff();
        };
        Apply(WslState.Off, null, null);
        tray.Visible = true;

        timer.Interval = 3000;
        timer.Tick += delegate { Poll(); };
        timer.Start();
        Poll();
    }

    void OnUi(MethodInvoker action)
    {
        try { ui.BeginInvoke(action); } catch { }
    }

    // ---------- state ----------

    void Poll()
    {
        if (polling || busy) return;
        polling = true;
        ThreadPool.QueueUserWorkItem(delegate
        {
            string running = "";
            bool vmAlive = false;
            long ramMb = 0;
            try
            {
                running = RunWsl("-l --running -q", 8000);
                vmAlive = Process.GetProcessesByName("vmmemWSL").Length > 0;
                ramMb = VmRamMb();
            }
            catch { }
            WslState s;
            if (!string.IsNullOrEmpty(running)) s = WslState.On;
            else if (vmAlive) s = WslState.Standby;
            else s = WslState.Off;
            OnUi(delegate
            {
                polling = false;
                if (!busy) Apply(s, running, ramMb > 0 ? (long?)ramMb : null);
            });
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
                text = T.S.StatusOn;
                string d = (distros ?? "").Replace("\r", "").Trim().Replace("\n", ", ");
                if (d.Length > 0) text += " - " + d;
                break;
            case WslState.Standby: text = T.S.StatusStandby; break;
            case WslState.Busy: text = T.S.StatusBusy; break;
            default: text = T.S.StatusOff; break;
        }
        if (ramMb.HasValue && s != WslState.Off)
            text += " - RAM " + (ramMb.Value / 1024.0).ToString("0.0", CultureInfo.CurrentCulture) + " GB";

        miStatus.Text = text;
        tray.Text = text.Length > 63 ? text.Substring(0, 63) : text;
        miOn.Enabled = (s == WslState.Off || s == WslState.Standby) && !busy;
        miOff.Enabled = s != WslState.Off && !busy;
        SetIcon(s);
    }

    // ---------- actions ----------

    bool Confirm(string question, string title)
    {
        if (asking) return false;
        asking = true;
        try
        {
            // invisible "always on top" owner window,
            // so the confirmation never ends up hidden behind other windows
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
        if (!Confirm(T.S.ConfirmOnText, T.S.ConfirmOnTitle)) return;
        busy = true;
        Apply(WslState.Busy, null, null);
        try
        {
            // A process that sleeps forever keeps the default distro running
            var psi = new ProcessStartInfo("wsl.exe", "--exec sleep infinity");
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            psi.WindowStyle = ProcessWindowStyle.Hidden;
            keepAlive = Process.Start(psi);
        }
        catch (Exception ex)
        {
            Notify(T.S.ErrStart + " " + ex.Message, ToolTipIcon.Error);
        }
        FinishAfter(2500);
    }

    void TurnOff()
    {
        if (busy) return;
        if (!Confirm(T.S.ConfirmOffText, T.S.ConfirmOffTitle)) return;
        busy = true;
        Apply(WslState.Busy, null, null);
        ThreadPool.QueueUserWorkItem(delegate
        {
            RunWsl("--shutdown", 30000);
            try { if (keepAlive != null && !keepAlive.HasExited) keepAlive.Kill(); } catch { }
            keepAlive = null;
            OnUi(delegate { FinishAfter(500); });
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
            // Windows Terminal if available, otherwise the classic console
            try { Process.Start(new ProcessStartInfo("wt.exe", "wsl.exe ~") { UseShellExecute = true }); }
            catch { Process.Start(new ProcessStartInfo("wsl.exe", "~") { UseShellExecute = true }); }
            FinishAfter(2000);
        }
        catch (Exception ex) { Notify(T.S.ErrTerminal + " " + ex.Message, ToolTipIcon.Error); }
    }

    void Exit()
    {
        timer.Stop();
        tray.Visible = false;
        tray.Dispose();
        if (currentIcon != IntPtr.Zero) DestroyIcon(currentIcon);
        // keepAlive is left running: exiting the app does not stop WSL
        ExitThread();
    }

    void Notify(string msg, ToolTipIcon icon)
    {
        tray.ShowBalloonTip(4000, "WSL", msg, icon);
    }

    // ---------- settings ----------

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

    static IconStyle LoadStyle()
    {
        try
        {
            using (var k = Registry.CurrentUser.OpenSubKey(SettingsKey))
            {
                if (k != null)
                {
                    string v = k.GetValue("IconStyle") as string;
                    if (v != null && Enum.IsDefined(typeof(IconStyle), v))
                        return (IconStyle)Enum.Parse(typeof(IconStyle), v);
                }
            }
        }
        catch { }
        return IconStyle.Terminal;
    }

    void AddStyle(string label, IconStyle st)
    {
        var item = new ToolStripMenuItem(label);
        item.Tag = st;
        item.Checked = st == style;
        item.Click += delegate
        {
            style = st;
            foreach (ToolStripMenuItem i in miStyle.DropDownItems) i.Checked = (IconStyle)i.Tag == st;
            try { using (var k = Registry.CurrentUser.CreateSubKey(SettingsKey)) k.SetValue("IconStyle", st.ToString()); }
            catch { }
            SetIcon(state);
        };
        miStyle.DropDownItems.Add(item);
    }

    // ---------- icon ----------

    void SetIcon(WslState s)
    {
        int size = Math.Max(16, SystemInformation.SmallIconSize.Width);
        using (var bmp = IconPainter.Draw(style, size, IconPainter.ColorFor(s)))
        {
            IntPtr h = bmp.GetHicon();
            tray.Icon = Icon.FromHandle(h);
            if (currentIcon != IntPtr.Zero) DestroyIcon(currentIcon);
            currentIcon = h;
        }
    }

    // ---------- helpers ----------

    static string RunWsl(string args, int timeoutMs)
    {
        try
        {
            var psi = new ProcessStartInfo("wsl.exe", args);
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.EnvironmentVariables["WSL_UTF8"] = "1";   // UTF-8 output instead of UTF-16
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
}

// ================= icon drawing =================
static class IconPainter
{
    public static readonly Color Red = Color.FromArgb(220, 38, 38);
    public static readonly Color Green = Color.FromArgb(22, 190, 80);
    public static readonly Color Amber = Color.FromArgb(245, 150, 11);

    public static Color ColorFor(WslState s)
    {
        switch (s)
        {
            case WslState.On: return Green;
            case WslState.Standby:
            case WslState.Busy: return Amber;
            default: return Red;
        }
    }

    public static Bitmap Draw(IconStyle style, int size, Color c)
    {
        var bmp = new Bitmap(size, size);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.Clear(Color.Transparent);
            float s = size;
            switch (style)
            {
                case IconStyle.Penguin: DrawPenguin(g, s, c); break;
                case IconStyle.Power: DrawPower(g, s, c); break;
                case IconStyle.TerminalLed: DrawTerminalLed(g, s, c); break;
                default: DrawTerminal(g, s, c); break;
            }
        }
        return bmp;
    }

    // Full-size glossy tile in the state color
    static void GlossyTile(Graphics g, float s, Color c, out GraphicsPath tile)
    {
        var r = new RectangleF(0, 0, s - 1, s - 1);
        tile = RoundRect(r, s * 0.22f);
        using (var br = new LinearGradientBrush(r, Lighten(c, 0.30f), Darken(c, 0.25f), 90f))
            g.FillPath(br, tile);
        using (var pen = new Pen(Darken(c, 0.45f), Math.Max(1f, s / 24f)))
            g.DrawPath(pen, tile);
    }

    // Style 1: colored tile with a ">_" prompt
    static void DrawTerminal(Graphics g, float s, Color c)
    {
        GraphicsPath tile;
        GlossyTile(g, s, c, out tile);
        tile.Dispose();
        DrawPrompt(g, s, Color.White, 0.18f, 0.26f, 1f);
    }

    static void DrawPrompt(Graphics g, float s, Color col, float x0, float y0, float scale)
    {
        float pw = Math.Max(1.7f, s * 0.13f * scale);
        using (var pen = new Pen(col, pw))
        {
            pen.StartCap = LineCap.Round;
            pen.EndCap = LineCap.Round;
            pen.LineJoin = LineJoin.Round;
            float w = s * scale;
            g.DrawLines(pen, new[] {
                new PointF(s * x0,              s * y0),
                new PointF(s * x0 + w * 0.24f,  s * y0 + w * 0.22f),
                new PointF(s * x0,              s * y0 + w * 0.44f) });
            g.DrawLine(pen, s * x0 + w * 0.36f, s * y0 + w * 0.46f, s * x0 + w * 0.64f, s * y0 + w * 0.46f);
        }
    }

    // Style 2: simplified Tux on a colored tile
    static void DrawPenguin(Graphics g, float s, Color c)
    {
        GraphicsPath tile;
        GlossyTile(g, s, c, out tile);
        var oldClip = g.Clip;
        g.SetClip(tile);
        using (var black = new SolidBrush(Color.FromArgb(25, 25, 25)))
        using (var white = new SolidBrush(Color.White))
        using (var orange = new SolidBrush(Color.FromArgb(255, 176, 0)))
        {
            g.FillEllipse(black, s * 0.17f, s * 0.10f, s * 0.66f, s * 1.10f);   // body
            g.FillEllipse(white, s * 0.29f, s * 0.42f, s * 0.42f, s * 0.70f);   // belly
            g.FillEllipse(white, s * 0.31f, s * 0.22f, s * 0.16f, s * 0.20f);   // eyes
            g.FillEllipse(white, s * 0.53f, s * 0.22f, s * 0.16f, s * 0.20f);
            g.FillEllipse(black, s * 0.37f, s * 0.29f, s * 0.08f, s * 0.10f);   // pupils
            g.FillEllipse(black, s * 0.56f, s * 0.29f, s * 0.08f, s * 0.10f);
            g.FillEllipse(orange, s * 0.36f, s * 0.40f, s * 0.28f, s * 0.13f);  // beak
        }
        g.Clip = oldClip;
        using (var pen = new Pen(Darken(c, 0.45f), Math.Max(1f, s / 24f)))
            g.DrawPath(pen, tile);
        tile.Dispose();
    }

    // Style 3: full-size glowing button with the power symbol
    static void DrawPower(Graphics g, float s, Color c)
    {
        var body = new RectangleF(0.5f, 0.5f, s - 2f, s - 2f);
        using (var bodyPath = new GraphicsPath())
        {
            bodyPath.AddEllipse(body);
            using (var br = new PathGradientBrush(bodyPath))
            {
                br.CenterPoint = new PointF(body.X + body.Width * 0.38f, body.Y + body.Height * 0.32f);
                br.CenterColor = Lighten(c, 0.50f);
                br.SurroundColors = new[] { Darken(c, 0.30f) };
                g.FillEllipse(br, body);
            }
        }
        using (var rim = new Pen(Darken(c, 0.50f), Math.Max(1f, s / 20f)))
            g.DrawEllipse(rim, body);

        float pw = Math.Max(1.7f, s / 8f);
        float r = body.Width * 0.28f;
        float cx = body.X + body.Width / 2f, cy = body.Y + body.Height / 2f + s * 0.03f;
        using (var pen = new Pen(Color.White, pw))
        {
            pen.StartCap = LineCap.Round;
            pen.EndCap = LineCap.Round;
            g.DrawArc(pen, cx - r, cy - r, 2 * r, 2 * r, -55, 290);
            g.DrawLine(pen, cx, cy - r * 1.35f, cx, cy - r * 0.15f);
        }
    }

    // Style 4: dark terminal window with ">_" and a status LED bottom-right
    static void DrawTerminalLed(Graphics g, float s, Color c)
    {
        var r = new RectangleF(0, 0, s - 1, s - 1);
        using (var tile = RoundRect(r, s * 0.20f))
        {
            using (var br = new LinearGradientBrush(r, Color.FromArgb(70, 70, 78), Color.FromArgb(28, 28, 32), 90f))
                g.FillPath(br, tile);
            using (var pen = new Pen(Color.FromArgb(120, 120, 130), Math.Max(1f, s / 24f)))
                g.DrawPath(pen, tile);
        }
        DrawPrompt(g, s, Color.FromArgb(235, 235, 235), 0.14f, 0.14f, 0.82f);

        // LED
        float d = s * 0.50f;
        var led = new RectangleF(s - d - s * 0.04f, s - d - s * 0.04f, d, d);
        using (var path = new GraphicsPath())
        {
            path.AddEllipse(led);
            using (var br = new PathGradientBrush(path))
            {
                br.CenterPoint = new PointF(led.X + led.Width * 0.38f, led.Y + led.Height * 0.35f);
                br.CenterColor = Lighten(c, 0.60f);
                br.SurroundColors = new[] { c };
                g.FillEllipse(br, led);
            }
        }
        using (var pen = new Pen(Color.FromArgb(20, 20, 20), Math.Max(1f, s / 16f)))
            g.DrawEllipse(pen, led);
    }

    static GraphicsPath RoundRect(RectangleF r, float rad)
    {
        float d = rad * 2;
        var p = new GraphicsPath();
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }

    static Color Lighten(Color c, float k)
    {
        return Color.FromArgb(c.A, c.R + (int)((255 - c.R) * k), c.G + (int)((255 - c.G) * k), c.B + (int)((255 - c.B) * k));
    }

    static Color Darken(Color c, float k)
    {
        return Color.FromArgb(c.A, (int)(c.R * (1 - k)), (int)(c.G * (1 - k)), (int)(c.B * (1 - k)));
    }

    // Multi-size .ico (PNG-compressed entries, supported since Windows Vista):
    // green "Terminal >_" style, the "WSL on" look
    public static void WriteAppIcon(string path)
    {
        int[] sizes = { 16, 24, 32, 48, 64, 256 };
        var images = new List<byte[]>();
        foreach (int sz in sizes)
            using (var b = Draw(IconStyle.Terminal, sz, Green))
            using (var ms = new System.IO.MemoryStream())
            {
                b.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                images.Add(ms.ToArray());
            }

        using (var fs = System.IO.File.Create(path))
        using (var w = new System.IO.BinaryWriter(fs))
        {
            w.Write((ushort)0); w.Write((ushort)1); w.Write((ushort)sizes.Length);   // ICONDIR
            int offset = 6 + 16 * sizes.Length;
            for (int i = 0; i < sizes.Length; i++)                                     // ICONDIRENTRY
            {
                w.Write((byte)(sizes[i] >= 256 ? 0 : sizes[i]));
                w.Write((byte)(sizes[i] >= 256 ? 0 : sizes[i]));
                w.Write((byte)0); w.Write((byte)0);
                w.Write((ushort)1); w.Write((ushort)32);
                w.Write((uint)images[i].Length); w.Write((uint)offset);
                offset += images[i].Length;
            }
            foreach (var img in images) w.Write(img);
        }
    }

    // Previews: individual PNGs + a comparison sheet with the 16 px icons zoomed in
    public static void RenderPreviews(string dir)
    {
        System.IO.Directory.CreateDirectory(dir);
        var styles = (IconStyle[])Enum.GetValues(typeof(IconStyle));
        var states = new[] { WslState.Off, WslState.On, WslState.Standby };
        string[] names = { "off", "on", "standby" };

        foreach (var st in styles)
            for (int i = 0; i < states.Length; i++)
                using (var b = Draw(st, 128, ColorFor(states[i])))
                    b.Save(System.IO.Path.Combine(dir, st.ToString().ToLower() + "-" + names[i] + ".png"),
                           System.Drawing.Imaging.ImageFormat.Png);

        // Sheet: for each style -> 3 states at actual 16 px, then the same zoomed 4x
        int cell = 80, pad = 12, label = 240;
        var sheet = new Bitmap(label + 3 * (16 + pad) + 3 * (64 + pad) + pad, styles.Length * cell + 40);
        using (var g = Graphics.FromImage(sheet))
        using (var font = new Font("Segoe UI", 11f))
        using (var small = new Font("Segoe UI", 9f))
        {
            g.Clear(Color.FromArgb(32, 32, 36));   // similar to a dark taskbar
            g.DrawString(T.S.PreviewActual, small, Brushes.Silver, label, 8);
            g.DrawString(T.S.PreviewZoom, small, Brushes.Silver, label + 3 * (16 + pad) + 10, 8);
            string[] labels = { "1. " + T.S.StyleTerminal, "2. " + T.S.StylePenguin, "3. " + T.S.StylePower, "4. " + T.S.StyleTerminalLed };
            for (int r = 0; r < styles.Length; r++)
            {
                int y = 40 + r * cell;
                g.DrawString(labels[r], font, Brushes.White, 10, y + 22);
                for (int i = 0; i < 3; i++)
                {
                    using (var b = Draw(styles[r], 16, ColorFor(states[i])))
                    {
                        g.DrawImage(b, label + i * (16 + pad), y + 24, 16, 16);
                        g.InterpolationMode = InterpolationMode.NearestNeighbor;
                        g.PixelOffsetMode = PixelOffsetMode.Half;
                        g.DrawImage(b, label + 3 * (16 + pad) + 10 + i * (64 + pad), y, 64, 64);
                    }
                }
            }
        }
        sheet.Save(System.IO.Path.Combine(dir, "icon-styles.png"), System.Drawing.Imaging.ImageFormat.Png);
        sheet.Dispose();
    }
}
