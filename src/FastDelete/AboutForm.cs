using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace FastDelete;

/// <summary>关于窗口：开源信息、版本号、两个仓库地址；可触发检查/应用更新。</summary>
public sealed class AboutForm : Form
{
    readonly Button _btnUpdate = new();
    readonly Label _lblUpdate = new();
    readonly Label _lblStatus = new();

    /// 更新应用后，调用方据此关闭主窗体（便携版即将重启自身）。
    public bool WantsExit { get; private set; }

    public AboutForm(Form owner, string latest, bool hasUpdate, string updateUrl)
    {
        Text = "关于 FastDelete";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        // 关键：CenterParent 必须在 Owner 赋值之后才生效，且要用 Size 而非 ClientSize 计算
        Owner = owner;
        StartPosition = FormStartPosition.Manual;   // 手动定位到主窗体正中（CenterParent 在 FixedDialog 下有时失效）
        MaximizeBox = MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(420, 300);
        Font = new Font("Microsoft YaHei", 9.5F);
        BackColor = Color.White;

        var ver = typeof(AboutForm).Assembly.GetName().Version?.ToString(3) ?? "1.0";
        string openSrc = "开源：代码见下方两个仓库（MIT 风格）";
        string github  = "github.com/delphuy/FastDelete";
        string gitee    = "gitee.com/delphuy/FastDelete";

        // 单一纵向分区：Row0 信息区 + Row1 按钮区（按钮与文字上下排列，非左右）
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(20, 16, 20, 12) };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        // ===== 信息区：标题/副标题/版本/开源 + 两个仓库地址（字号小、间距小） =====
        var info = new TableLayoutPanel { ColumnCount = 1, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        info.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var title = new Label { Text = "FastDelete", Font = new Font("Microsoft YaHei", 17F, FontStyle.Bold), ForeColor = Color.FromArgb(0,120,215), AutoSize = true };
        var sub   = new Label { Text = "极速批量删除工具 · Windows x64", Font = new Font("Microsoft YaHei", 9F), ForeColor = Color.Gray, AutoSize = true, Margin = new Padding(0, 3, 0, 8) };
        var verL  = new Label { Text = "v" + ver, Font = new Font("Microsoft YaHei", 9F), AutoSize = true };
        var srcL  = new Label { Text = openSrc, ForeColor = Color.FromArgb(90,90,96), AutoSize = true, Margin = new Padding(0, 1, 0, 8), Font = new Font("Microsoft YaHei", 8.5F) };

        // 两个仓库地址：小字号、小间距、左对齐纵排（不居中、不占满）
        var ghL = new LinkLabel { Text = github, Font = new Font("Microsoft YaHei", 8.5F), AutoSize = true, LinkColor = Color.FromArgb(0,120,215), ActiveLinkColor = Color.FromArgb(0,90,170), LinkBehavior = LinkBehavior.HoverUnderline, Margin = new Padding(0, 0, 0, 2) };
        var gtL = new LinkLabel { Text = gitee, Font = new Font("Microsoft YaHei", 8.5F), AutoSize = true, LinkColor = Color.FromArgb(0,120,215), ActiveLinkColor = Color.FromArgb(0,90,170), LinkBehavior = LinkBehavior.HoverUnderline, Margin = new Padding(0, 0, 0, 0) };

        void AddRow(Control c) { info.Controls.Add(c, 0, info.Controls.Count); }
        AddRow(title); AddRow(sub); AddRow(verL); AddRow(srcL); AddRow(ghL); AddRow(gtL);

        // ===== 按钮区：按钮在上，版本提示在下（上下排列）=====
        var btnArea = new Panel { Dock = DockStyle.Fill };
        var btnCol = new TableLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 1, Padding = new Padding(0) };
        btnCol.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        _btnUpdate.Text = hasUpdate ? "立即升级到 " + latest : "检查更新";
        _btnUpdate.Font = new Font("Microsoft YaHei", 9.5F, FontStyle.Bold);
        _btnUpdate.ForeColor = Color.White; _btnUpdate.BackColor = hasUpdate ? Color.FromArgb(230,126,34) : Color.FromArgb(0,120,215);
        _btnUpdate.FlatStyle = FlatStyle.Flat; _btnUpdate.FlatAppearance.BorderSize = 0;
        _btnUpdate.Size = new Size(130, 32);
        _btnUpdate.Cursor = Cursors.Hand;
        _btnUpdate.Click += OnUpdateClick;

        _lblUpdate.Text = hasUpdate ? ("最新 " + latest + "（当前 " + ver + "）") : ("当前 " + ver);
        _lblUpdate.Font = new Font("Microsoft YaHei", 8.5F);
        _lblUpdate.ForeColor = Color.Gray;
        _lblUpdate.AutoSize = true;

        _lblStatus.Text = "";
        _lblStatus.Font = new Font("Microsoft YaHei", 8.5F);
        _lblStatus.ForeColor = Color.FromArgb(90,90,96);
        _lblStatus.AutoSize = true;

        // 按钮 / 版本提示 / 状态 三行纵排，整体在按钮区水平居中
        var center = new Panel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        center.Controls.Add(_btnUpdate);
        center.Controls.Add(_lblUpdate);
        center.Controls.Add(_lblStatus);
        _btnUpdate.Location = new Point(0, 0);
        _lblUpdate.Location = new Point(0, 38);
        _lblStatus.Location = new Point(0, 58);
        _lblUpdate.Anchor = AnchorStyles.None;
        _lblStatus.Anchor = AnchorStyles.None;
        // center 自动宽度=按钮宽，水平居中；纵向三行
        center.Paint += (s, e) => { e.Graphics.Clear(Color.White); };

        btnArea.Controls.Add(center);
        void CenterBtn() {
            center.Left = Math.Max(0, (btnArea.ClientSize.Width - center.Width) / 2);
            center.Top  = (btnArea.ClientSize.Height - center.Height) / 2;
        }
        btnArea.SizeChanged += (s, e) => CenterBtn();
        CenterBtn();

        root.Controls.Add(info, 0, 0);
        root.Controls.Add(btnArea, 0, 1);
        Controls.Add(root);

        ghL.LinkClicked += (s, e) => OpenUrl("https://" + github);
        gtL.LinkClicked += (s, e) => OpenUrl("https://" + gitee);

        // 主窗体正中定位（Manual 最可靠）
        PositionCenterOnOwner(owner);
    }

    void PositionCenterOnOwner(Form owner)
    {
        // 在主窗体客户区正中放本窗体
        int x = owner.Left + (owner.Width - Width) / 2;
        int y = owner.Top + (owner.Height - Height) / 2;
        // 夹到屏幕内（避免主窗体贴边时弹窗跑出屏幕）
        var scr = Screen.FromHandle(owner.Handle).WorkingArea;
        if (x < scr.Left + 4) x = scr.Left + 4;
        if (y < scr.Top + 4) y = scr.Top + 4;
        if (x + Width > scr.Right - 4) x = scr.Right - 4 - Width;
        if (y + Height > scr.Bottom - 4) y = scr.Bottom - 4 - Height;
        Location = new Point(x, y);
    }

    static void OpenUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return;
        try { Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true }); } catch { }
    }

    async void OnUpdateClick(object? sender, EventArgs e)
    {
        _btnUpdate.Enabled = false;
        _lblStatus.Text = "正在检查 / 下载…";
        _lblStatus.Visible = true;
        string latest = Updater.LocalVersion;
        bool hasUpdate = false;
        string url = "";
        try
        {
            var (has, ver, u) = await Updater.CheckLatestAsync();
            hasUpdate = has; latest = ver; url = u;
            if (!hasUpdate)
            {
                _lblStatus.Text = "已是最新版本 (" + ver + ")";
                _btnUpdate.Enabled = true;
                return;
            }
            _lblStatus.Text = "发现 " + ver + "，开始下载…";
            bool ok = await Updater.ApplyUpdateAsync(url,
                p => _lblStatus.Text = p > 0 ? $"下载中 {p*100:0}%…" : "下载中…",
                msg => _lblStatus.Text = msg,
                this);
            if (!ok) _lblStatus.Text = "更新失败，请重试或手动下载。";
            else { _lblStatus.Text = "更新完成。"; WantsExit = Updater.IsPortable; }
        }
        catch (Exception ex)
        {
            _lblStatus.Text = "检查失败：" + ex.Message;
        }
        finally { _btnUpdate.Enabled = true; }
    }
}