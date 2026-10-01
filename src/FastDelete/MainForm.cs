using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace FastDelete;

public sealed class MainForm : Form
{
    static readonly Color PrimaryBlue = Color.FromArgb(0, 120, 215);
    static readonly Color ActionBlue = Color.FromArgb(30, 80, 160);
    static readonly Color BgGray = Color.FromArgb(244, 246, 249);
    static readonly Color BarBg = Color.FromArgb(250, 250, 252);
    static readonly Color DarkText = Color.FromArgb(45, 45, 50);
    static readonly Font BtnFont = new Font("Microsoft YaHei", 9F, FontStyle.Bold);

    readonly Button _btnAdd = new();
    readonly ListView _list = new();
    readonly TextBox _logBox = new() { ReadOnly = true, ScrollBars = ScrollBars.Vertical };
    readonly Button _btnDelete = new();
    readonly LogFile _logFile;

    readonly Panel _overlay = new();
    readonly Panel _overlayCard = new();
    readonly ProgressBar _progress = new();
    readonly Label _overlayLabel = new();
    readonly System.Windows.Forms.Timer _progTimer = new() { Interval = 200 };
    int _progStep = 3;

    readonly Dictionary<int, string> _rowDir = new();
    bool _scanning = false;
    bool _autoMode = false;
    string? _lastDir = null;
    bool _deleting = false;
    readonly Label _overlayHint = new();

    readonly List<string> _logLines = new();
    const int MaxLogLines = 500;

    readonly Button _btnAbout = new();
    readonly Label _lblUpdate = new();
    readonly Panel _statusBar = new();
    readonly Label _lblStatus = new();
    string _latestVersion = "";
    bool _hasUpdate = false;
    string _updateUrl = "";

    public MainForm(string? autoAddDirectory = null)
    {
        _logFile = new LogFile("log");
        WriteLog("🚀 FastDelete 已启动");

        Text = "FastDelete v" + AppVersion + " · 快速文件清理";
        Font = new Font("Microsoft YaHei", 10F);
        Size = new Size(980, 640);
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(820, 500);
        BackColor = BgGray;
        try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

        _autoMode = !string.IsNullOrEmpty(autoAddDirectory);
        _lastDir = LoadLastDir();
        CheckForUpdatesInBackground();
        FormClosing += (s, e) =>
        {
            if (_deleting)
            {
                e.Cancel = true;
                MessageBox.Show("正在删除中，无法关闭，请稍候…", "FastDelete", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        };
        BuildLayout();
        _progTimer.Tick += (s, e) => { if (_progress.Value < 92) _progress.Value = Math.Min(92, _progress.Value + _progStep); };

        if (!string.IsNullOrEmpty(autoAddDirectory))
        {
            Task.Run(() => AddDirectory(autoAddDirectory));
        }
    }

    void BuildLayout()
    {
        var mid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = new Padding(12), BackColor = BgGray };
        mid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        mid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        var leftContainer = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = Color.White };
        leftContainer.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        leftContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var leftBar = new Panel { Dock = DockStyle.Fill, BackColor = BarBg, Padding = new Padding(8, 7, 8, 7) };
        var hint = new Label { Text = "📁 请选择目录", AutoSize = true, Font = new Font("Microsoft YaHei", 10F, FontStyle.Bold), ForeColor = DarkText, Location = new Point(8, 10), Anchor = AnchorStyles.Top | AnchorStyles.Left };

        _btnAdd.Text = "＋ 添加"; _btnAdd.AutoSize = false; _btnAdd.Size = new Size(96, 32);
        _btnAdd.Font = new Font("Microsoft YaHei", 10F, FontStyle.Bold);
        _btnAdd.ForeColor = Color.White; _btnAdd.BackColor = PrimaryBlue;
        _btnAdd.FlatStyle = FlatStyle.Flat; _btnAdd.FlatAppearance.BorderSize = 0;
        _btnAdd.Cursor = Cursors.Hand; _btnAdd.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _btnAdd.Click += OnAddClick;
        void LayoutLeftBar() { _btnAdd.Location = new Point(leftBar.ClientSize.Width - _btnAdd.Width - 8, 7); }
        leftBar.SizeChanged += (s, e) => LayoutLeftBar();
        leftBar.Controls.Add(hint); leftBar.Controls.Add(_btnAdd);

        _list.View = View.Details; _list.FullRowSelect = true; _list.MultiSelect = false; _list.HideSelection = false;
        _list.Font = new Font("Microsoft YaHei", 10F); _list.Dock = DockStyle.Fill;
        _list.BackColor = Color.White; _list.BorderStyle = BorderStyle.None;
        _list.Columns.Add("目录地址", 200); _list.Columns.Add("文件数", 90); _list.Columns.Add("状态", 120);
        _list.SizeChanged += (s, e) => { int avail = _list.ClientSize.Width - 90 - 120; if (avail > 60) _list.Columns[0].Width = avail; };

        _list.OwnerDraw = true;
        _list.DrawSubItem += (s, e) =>
        {
            if (e.ColumnIndex == 2)
            {
                var item = _list.Items[e.ItemIndex];
                if (item.SubItems.Count > 1)
                {
                    string cnt = item.SubItems[1].Text;
                    if (cnt == ">10万")
                    {
                        using var br = new SolidBrush(PrimaryBlue);
                        e.Graphics.FillRectangle(br, e.Bounds);
                        TextRenderer.DrawText(e.Graphics, "🔍 继续扫描", BtnFont, e.Bounds, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                        return;
                    }
                    if (cnt == "…正在彻底扫描")
                    {
                        using var br = new SolidBrush(Color.FromArgb(150, 150, 158));
                        e.Graphics.FillRectangle(br, e.Bounds);
                        TextRenderer.DrawText(e.Graphics, "…扫描中", BtnFont, e.Bounds, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                        return;
                    }
                }
            }
            e.DrawDefault = true;
        };
        _list.DrawColumnHeader += (s, e) => e.DrawDefault = true;
        _list.MouseClick += (s, e) =>
        {
            var hit = _list.HitTest(e.Location);
            if (hit.Item != null && hit.SubItem != null)
            {
                int col = hit.Item.SubItems.IndexOf(hit.SubItem);
                if (col == 2 && hit.Item.SubItems.Count > 1 && hit.Item.SubItems[1].Text == ">10万")
                    RescanRow(hit.Item.Index);
            }
        };

        leftContainer.Controls.Add(leftBar, 0, 0);
        leftContainer.Controls.Add(_list, 0, 1);

        var rightContainer = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = Color.White };
        rightContainer.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        rightContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var rightBar = new Panel { Dock = DockStyle.Fill, BackColor = BarBg, Padding = new Padding(8, 7, 8, 7) };

        _btnDelete.Text = "🗑 快速删除"; _btnDelete.AutoSize = false; _btnDelete.Size = new Size(130, 32);
        _btnDelete.Font = new Font("Microsoft YaHei", 10F, FontStyle.Bold);
        _btnDelete.ForeColor = Color.White; _btnDelete.BackColor = ActionBlue;
        _btnDelete.FlatStyle = FlatStyle.Flat; _btnDelete.FlatAppearance.BorderSize = 0;
        _btnDelete.Cursor = Cursors.Hand; _btnDelete.Location = new Point(8, 7);
        _btnDelete.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        _btnDelete.Click += OnDeleteClick;
        rightBar.Controls.Add(_btnDelete);

        _logBox.Multiline = true; _logBox.WordWrap = true; _logBox.Font = new Font("Microsoft YaHei", 9F);
        _logBox.BackColor = Color.White; _logBox.ForeColor = Color.FromArgb(40, 40, 45);
        _logBox.BorderStyle = BorderStyle.None; _logBox.Dock = DockStyle.Fill;

        rightContainer.Controls.Add(rightBar, 0, 0);
        rightContainer.Controls.Add(_logBox, 0, 1);

        mid.Controls.Add(leftContainer, 0, 0);
        mid.Controls.Add(rightContainer, 1, 0);

        _overlay.Dock = DockStyle.Fill; _overlay.BackColor = Color.FromArgb(180, 20, 20, 25); _overlay.Visible = false;
        _overlayCard.AutoSize = false; _overlayCard.Size = new Size(360, 190); _overlayCard.BackColor = Color.White; _overlayCard.BorderStyle = BorderStyle.FixedSingle; _overlayCard.Padding = new Padding(16);
        _overlayLabel.Text = "正在删除，请稍候… (｡•̀ᴗ-)✧"; _overlayLabel.Font = new Font("Microsoft YaHei", 11F, FontStyle.Bold);
        _overlayLabel.ForeColor = DarkText; _overlayLabel.TextAlign = ContentAlignment.MiddleCenter; _overlayLabel.Dock = DockStyle.Top; _overlayLabel.Height = 60;
        _progress.Style = ProgressBarStyle.Continuous; _progress.Size = new Size(320, 22); _progress.Anchor = AnchorStyles.None;
        _overlayHint.Text = "提示：删除中右上角×已禁用；任务管理器仍可强制结束，请等待 100% 完成再关。";
        _overlayHint.Font = new Font("Microsoft YaHei", 8F, FontStyle.Italic);
        _overlayHint.ForeColor = Color.Gray; _overlayHint.TextAlign = ContentAlignment.MiddleCenter;
        _overlayHint.Size = new Size(320, 40); _overlayHint.Anchor = AnchorStyles.None; _overlayHint.Location = new Point(20, 124);
        _overlayCard.Controls.Add(_progress); _overlayCard.Controls.Add(_overlayLabel); _overlayCard.Controls.Add(_overlayHint);
        void LayoutCard() { _overlayCard.Location = new Point((_overlay.ClientSize.Width - _overlayCard.Width) / 2, (_overlay.ClientSize.Height - _overlayCard.Height) / 2); _progress.Location = new Point((_overlayCard.ClientSize.Width - _progress.Width) / 2, 95); }
        _overlay.SizeChanged += (s, e) => LayoutCard();
        _overlay.Controls.Add(_overlayCard);

        Controls.Add(mid);
        Controls.Add(_statusBar);
        Controls.Add(_overlay);
        BuildStatusBar();
        Load += (s, e) => { LayoutLeftBar(); LayoutCard(); _overlay.BringToFront(); };
    }

    void BuildStatusBar()
    {
        // 底部全宽状态栏：一条细分隔线 + 左端版本信息 + 右端「i」关于按钮 + 有更新标记
        _statusBar.Dock = DockStyle.Bottom;
        _statusBar.Height = 34;
        _statusBar.BackColor = BarBg;
        _statusBar.Paint += (s, e) => { using var pen = new Pen(Color.FromArgb(225,225,230)); e.Graphics.DrawLine(pen, 0, 0, _statusBar.Width, 0); };

        // 左端：版本 + 就绪提示（用 Panel 容器使 label 垂直居中）
        var statusLeft = new Panel { Dock = DockStyle.Left, Height = _statusBar.Height, Width = 160, BackColor = Color.Transparent };
        _lblStatus.Text = "v" + AppVersion;
        _lblStatus.Font = new Font("Microsoft YaHei", 8.5F);
        _lblStatus.ForeColor = Color.Gray;
        _lblStatus.AutoSize = true;
        _lblStatus.Location = new Point(10, (_statusBar.Height - 16) / 2);   // 16≈文字高，垂直居中
        statusLeft.Controls.Add(_lblStatus);
        _statusBar.Controls.Add(statusLeft);

        // 右端 i 按钮（无边框、贴状态栏底）
        _btnAbout.Text = "i";
        _btnAbout.Font = new Font("Segoe UI", 15F, FontStyle.Bold);
        _btnAbout.Size = new Size(28, 28);
        _btnAbout.FlatStyle = FlatStyle.Flat;
        _btnAbout.FlatAppearance.BorderSize = 0;            // 无边框
        _btnAbout.FlatAppearance.MouseOverBackColor = Color.FromArgb(235,240,250);
        _btnAbout.FlatAppearance.MouseDownBackColor = Color.FromArgb(210,225,245);
        _btnAbout.BackColor = Color.Transparent;
        _btnAbout.ForeColor = Color.FromArgb(0,120,215);
        _btnAbout.Cursor = Cursors.Hand;
        _btnAbout.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        _btnAbout.Click += OnAboutClick;
        _statusBar.Controls.Add(_btnAbout);

        // 「有更新」标记：i 左侧（状态栏内）
        _lblUpdate.Text = "有更新";
        _lblUpdate.Font = new Font("Microsoft YaHei", 8F, FontStyle.Bold);
        _lblUpdate.AutoSize = true;
        _lblUpdate.ForeColor = Color.White;
        _lblUpdate.BackColor = Color.FromArgb(230,126,34);
        _lblUpdate.Padding = new Padding(7,1,7,1);
        _lblUpdate.Visible = false;
        _lblUpdate.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        _lblUpdate.Cursor = Cursors.Hand;
        _lblUpdate.Click += OnAboutClick;
        _statusBar.Controls.Add(_lblUpdate);

        _statusBar.SizeChanged += (s, e) => LayoutStatusRight();
        LayoutStatusRight();
    }

    void LayoutStatusRight()
    {
        _btnAbout.Location = new Point(_statusBar.ClientSize.Width - _btnAbout.Width - 8, (_statusBar.Height - _btnAbout.Height) / 2);
        _lblUpdate.Location = new Point(_btnAbout.Left - _lblUpdate.Width - 8, (_statusBar.Height - _lblUpdate.Height) / 2);
        _btnAbout.BringToFront();
        _lblUpdate.BringToFront();
    }

    /// 更新左端状态文本（如切换目录/扫描/删除中状态）。
    void SetStatus(string txt) => _lblStatus.Text = txt;

    async void OnAboutClick(object? sender, EventArgs e)
    {
        var f = new AboutForm(this, _latestVersion, _hasUpdate, _updateUrl);
        f.FormClosed += (s, e2) => {
            if (f.WantsExit) { Close(); } // 便携版已替换并重启，旧进程退出
            f.Dispose();
        };
        f.Show();
    }

    /// 启动后后台查 Gitee 最新版本，有新版则亮「有更新」标记。
    void CheckForUpdatesInBackground()
    {
        Updater.LocalVersion = AppVersion;
        Task.Run(async () =>
        {
            try
            {
                var (has, latest, url) = await Updater.CheckLatestAsync();
                _hasUpdate = has; _latestVersion = latest; _updateUrl = url;
                BeginInvoke(() => { _lblUpdate.Visible = has; LayoutStatusRight(); });
            }
            catch { /* 网络失败静默，不阻塞主流程 */ }
        });
    }

    void OnAddClick(object? sender, EventArgs e)
    {
        using var dlg = new FolderBrowserDialog { SelectedPath = _lastDir ?? Directory.GetCurrentDirectory() };
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            _lastDir = dlg.SelectedPath;
            SaveLastDir(_lastDir);
            AddDirectory(dlg.SelectedPath);
        }
    }

    public void AddDirectory(string dir)
    {
        if (!Directory.Exists(dir)) { WriteLog("⚠ 目录不存在：" + dir); return; }
        var item = new ListViewItem(dir);
        item.SubItems.Add("…清点中"); item.SubItems.Add("…");
        _list.Items.Add(item);
        int rowIndex = _list.Items.Count - 1;
        _rowDir[rowIndex] = dir;
        WriteLog("📂 添加目录：" + dir);
        Task.Run(() =>
        {
            var (exists, files, _) = Scanner.CountPartial(dir, Scanner.PartialLimit);
            BeginInvoke(() => UpdateRowAfterPartial(rowIndex, dir, files, exists));
        });
    }

    void UpdateRowAfterPartial(int rowIndex, string dir, long files, bool exists)
    {
        if (rowIndex < 0 || rowIndex >= _list.Items.Count) return;
        var item = _list.Items[rowIndex];
        if (item.SubItems[1].Text == "…正在彻底扫描") return;
        if (!exists)
        {
            item.SubItems[1].Text = "不存在"; item.SubItems[2].Text = "-";
            WriteLog("⚠ 目录已不存在：" + dir); return;
        }
        if (files >= Scanner.PartialLimit)
        {
            item.SubItems[1].Text = ">10万"; item.SubItems[2].Text = "";
            WriteLog("⚠ 文件超 10 万 · " + dir + "（点击该行状态列继续扫描）");
        }
        else
        {
            item.SubItems[1].Text = files.ToString("N0"); item.SubItems[2].Text = "就绪";
            WriteLog("✅ 清点完成：" + files.ToString("N0") + " 个文件 · " + dir);
            // 右键自动模式：清点完成且就绪 → 自动触发删除
            if (_autoMode) { _autoMode = false; BeginInvoke(() => OnDeleteClick(null, null)); }
        }
    }

    void RescanRow(int rowIndex)
    {
        if (_scanning) return;
        if (rowIndex < 0 || rowIndex >= _list.Items.Count) return;
        if (!_rowDir.TryGetValue(rowIndex, out string? dir)) return;
        var item = _list.Items[rowIndex];
        if (item.SubItems[1].Text != ">10万") return;
        item.SubItems[1].Text = "…正在彻底扫描"; item.SubItems[2].Text = "";
        _list.Invalidate(item.Bounds);
        WriteLog("🔍 彻底扫描中：" + dir);
        _scanning = true;
        Task.Run(() =>
        {
            var (exists, files, _) = Scanner.CountAll(dir);
            BeginInvoke(() =>
            {
                _scanning = false;
                if (rowIndex >= _list.Items.Count) return;
                var it = _list.Items[rowIndex];
                if (!exists) { it.SubItems[1].Text = "不存在"; it.SubItems[2].Text = "-"; WriteLog("⚠ 目录已不存在：" + dir); }
                else { it.SubItems[1].Text = files.ToString("N0"); it.SubItems[2].Text = "就绪"; WriteLog("✅ 扫描完成：" + files.ToString("N0") + " 个文件 · " + dir); }
                _list.Invalidate(it.Bounds);
            });
        });
    }

    void OnDeleteClick(object? sender, EventArgs e)
    {
        if (_list.Items.Count == 0)
        {
            MessageBox.Show("请先点击「＋ 添加」选择目录再删除。", "FastDelete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        long totalFiles = 0;
        var targets = new List<string>();
        foreach (var obj in _list.Items)
        {
            var item = (ListViewItem)obj;
            string d = item.SubItems[0].Text;
            string cntText = item.SubItems[1].Text;
            long cnt;
            if (cntText == ">10万") cnt = Scanner.PartialLimit;
            else if (cntText == "…清点中" || cntText == "…正在彻底扫描") cnt = 0;
            else cnt = long.TryParse(cntText.Replace(",", ""), out var v) ? v : 0;
            totalFiles += cnt;
            targets.Add(d);
        }

        if (!ConfirmDialogs.Confirm(totalFiles, this)) { WriteLog("🚫 已取消删除"); return; }

        WriteLog("🗑 确认删除，共 " + totalFiles.ToString("N0") + " 个文件");
        _btnDelete.Enabled = false;
        _deleting = true;
        SetCloseButtonEnabled(false);
        // 进度：100 格，Timer 估算递进 + 每完成目录真实跳
        _progress.Maximum = 100; _progress.Value = 0;
        _progStep = totalFiles > 0 ? Math.Max(1, Math.Min(8, (int)(80000 / totalFiles))) : 3;
        _overlayLabel.Text = "正在删除，请稍候… (｡•̀ᴗ-)✧";
        _overlay.Visible = true; _overlay.BringToFront();
        _progTimer.Start();

        int total = targets.Count;
        Task.Run(() =>
        {
            int dirCount = 0; long fileCount = 0;
            for (int i = 0; i < targets.Count; i++)
            {
                var dir = targets[i];
                var (exists, files, _) = Scanner.CountAll(dir);
                if (!exists) { WriteLog("⚠ 目录已不存在：" + dir); continue; }
                dirCount++; fileCount += files;
                WriteLog("🗑 正在清理：" + dir);
                Deleter.Execute(dir, files, m => _logFile.Write(DateTime.Now.ToString("HH:mm:ss") + "  " + m));
                WriteLog("✅ 已清理：" + dir);
                int done = i + 1;
                BeginInvoke(() => { _progress.Value = Math.Max(_progress.Value, done * 100 / total); });
            }
            BeginInvoke(() =>
            {
                _progTimer.Stop();
                _progress.Value = 100;
                _overlay.Visible = false;
                _btnDelete.Enabled = true;
                _deleting = false;
                SetCloseButtonEnabled(true);
                WriteLog("📊 本次完成：" + dirCount + " 个文件夹、" + fileCount.ToString("N0") + " 个文件");
                _list.Items.Clear();
                _rowDir.Clear();
                try { new ConfettiForm(this.Bounds).Show(this); } catch { }
            });
        });
    }

    static string AppVersion => typeof(MainForm).Assembly.GetName().Version?.ToString(3) ?? "1.0";

    [DllImport("user32.dll")] static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);
    [DllImport("user32.dll")] static extern int EnableMenuItem(IntPtr hMenu, uint uIDEnableItem, uint uCmdState);
    const uint SC_CLOSE = 0xF120, MF_GRAYED = 0x1, MF_ENABLED = 0x0;

    /// <summary>删除中置灰右上角×（保留最小化按钮）；完成后恢复。</summary>
    void SetCloseButtonEnabled(bool enabled)
    {
        try
        {
            IntPtr h = GetSystemMenu(Handle, false);
            if (h != IntPtr.Zero) EnableMenuItem(h, SC_CLOSE, enabled ? MF_ENABLED : MF_GRAYED);
        }
        catch { }
    }

    static string LastDirPath => System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FastDelete", "lastdir.txt");

    static string? LoadLastDir()
    {
        try { if (System.IO.File.Exists(LastDirPath)) return System.IO.File.ReadAllText(LastDirPath).Trim(); } catch { }
        return null;
    }

    static void SaveLastDir(string d)
    {
        try
        {
            var dir = System.IO.Path.GetDirectoryName(LastDirPath);
            if (!string.IsNullOrEmpty(dir)) System.IO.Directory.CreateDirectory(dir);
            System.IO.File.WriteAllText(LastDirPath, d);
        }
        catch { }
    }

    void WriteLog(string msg)
    {
        string line = DateTime.Now.ToString("HH:mm:ss") + "  " + msg;
        if (IsHandleCreated)
        {
            _logLines.Insert(0, line);
            if (_logLines.Count > MaxLogLines) _logLines.RemoveAt(_logLines.Count - 1);
            _logBox.Lines = _logLines.ToArray();
            _logBox.Select(0, 0);
            _logBox.ScrollToCaret();
        }
        _logFile.Write(line);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _logFile.CloseAndHeader();
        _logFile.Dispose();
        base.OnFormClosed(e);
    }
}

// 撒花：从中心迸发爆炸（向外飞散 + 重力下落），2.5 秒自动关闭
sealed class ConfettiForm : Form
{
    struct P { public float x, y, vx, vy; public Color c; public float w, h; }
    readonly List<P> ps = new();
    readonly System.Windows.Forms.Timer t;
    readonly DateTime start;
    static readonly Color[] Colors = new[]
    {
        Color.FromArgb(231,76,60), Color.FromArgb(241,196,15), Color.FromArgb(46,204,113),
        Color.FromArgb(52,152,219), Color.FromArgb(155,89,182), Color.FromArgb(230,126,34),
    };

    public ConfettiForm(Rectangle bounds)
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        Bounds = bounds;
        TransparencyKey = Color.Magenta;
        BackColor = Color.Magenta;
        TopMost = true;
        ShowInTaskbar = false;
        DoubleBuffered = true;
        t = new System.Windows.Forms.Timer { Interval = 30 };
        t.Tick += Tick;
        start = DateTime.Now;
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        Spawn();
        t.Start();
    }

    void Spawn()
    {
        var rnd = new Random();
        float cx = Width / 2f, cy = Height / 2f;  // 从中心（约进度条位置）迸发
        for (int i = 0; i < 200; i++)
        {
            double ang = rnd.NextDouble() * Math.PI * 2;
            float sp = (float)(rnd.NextDouble() * 9 + 5);
            ps.Add(new P
            {
                x = cx, y = cy,
                vx = (float)Math.Cos(ang) * sp,
                vy = (float)Math.Sin(ang) * sp - 3f,  // 略偏上，像爆炸
                c = Colors[rnd.Next(Colors.Length)],
                w = rnd.Next(6, 13),
                h = rnd.Next(8, 18),
            });
        }
    }

    void Tick(object? s, EventArgs e)
    {
        for (int i = 0; i < ps.Count; i++)
        {
            var p = ps[i];
            p.x += p.vx;
            p.y += p.vy;
            p.vy += 0.22f;   // 重力下落
            p.vx *= 0.99f;   // 阻力
            ps[i] = p;
        }
        if ((DateTime.Now - start).TotalSeconds > 2.5) { t.Stop(); Close(); }
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(Color.Magenta);
        foreach (var p in ps)
        {
            if (p.y > Height + 40) continue;
            using var br = new SolidBrush(p.c);
            e.Graphics.FillRectangle(br, p.x, p.y, p.w, p.h);
        }
    }
}
