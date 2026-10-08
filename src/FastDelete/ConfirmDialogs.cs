using System;
using System.Drawing;
using System.Windows.Forms;

namespace FastDelete;

public static class ConfirmDialogs
{
    public static bool Confirm(long M, IWin32Window? owner = null)
    {
        if (M == 0)
        {
            using var f = new ConfirmForm("（；´д｀）欸？这里空空如也，没有东西可以删呀～", "知道啦", null, owner, ConfirmStyle.Info);
            f.ShowDialog();
            return false;
        }

        if (M < 100)
        {
            using var f = new ConfirmForm("（￣ε￣）就这点儿也配动用牛刀？", "少罗嗦，快删！", "那算了", owner, ConfirmStyle.Warn);
            f.ShowDialog();
            return f.ChoseYes;
        }

        using var f2 = new ConfirmForm("（｡ŏ_ŏ）清理一旦开始就找不回来咯，真的想好了吗？", "赶紧删别啰嗦", "我再想想", owner, ConfirmStyle.Danger);
        f2.ShowDialog();
        return f2.ChoseYes;
    }

    enum ConfirmStyle { Info, Warn, Danger }

    sealed class ConfirmForm : Form
    {
        public bool ChoseYes { get; private set; }

        public ConfirmForm(string message, string yesText, string? noText, IWin32Window? owner, ConfirmStyle style)
        {
            Text = "FastDelete";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = owner is null ? FormStartPosition.CenterScreen : FormStartPosition.CenterParent;
            MaximizeBox = MinimizeBox = false;
            ShowInTaskbar = false;
            if (owner != null) Owner = (Form)owner;
            Font = new Font("Microsoft YaHei", 10F);
            ClientSize = new Size(500, 210);
            BackColor = Color.FromArgb(248, 249, 251);

            Color yesColor = style switch
            {
                ConfirmStyle.Info => Color.FromArgb(0, 120, 215),
                ConfirmStyle.Warn => Color.FromArgb(30, 80, 160),
                ConfirmStyle.Danger => Color.FromArgb(30, 80, 160),
                _ => Color.FromArgb(30, 80, 160),
            };

            var lbl = new Label
            {
                Text = message,
                AutoSize = false,
                Font = new Font("Microsoft YaHei", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 40, 50),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(20, 28),
                Size = new Size(460, 90),
            };

            void SetupBtn(Button b, Color bg, Color fg)
            {
                b.AutoSize = false;
                b.Font = new Font("Microsoft YaHei", 10F, FontStyle.Bold);
                b.FlatStyle = FlatStyle.Flat;
                b.FlatAppearance.BorderSize = 0;
                b.Cursor = Cursors.Hand;
                b.Height = 34;
                b.BackColor = bg;
                b.ForeColor = fg;
            }

            if (string.IsNullOrEmpty(noText))
            {
                var b1 = new Button { Text = yesText, Size = new Size(140, 34), Location = new Point(180, 140) };
                SetupBtn(b1, Color.FromArgb(150, 150, 158), Color.White);
                b1.Click += (s, e) => { ChoseYes = true; Close(); };
                Controls.Add(lbl);
                Controls.Add(b1);
                b1.Focus();
            }
            else
            {
                var b1 = new Button { Text = yesText, Size = new Size(160, 34), Location = new Point(70, 140) };
                SetupBtn(b1, yesColor, Color.White);
                var b2 = new Button { Text = noText, Size = new Size(160, 34), Location = new Point(270, 140) };
                SetupBtn(b2, Color.FromArgb(220, 222, 226), Color.FromArgb(60, 60, 65));
                b1.Click += (s, e) => { ChoseYes = true; Close(); };
                b2.Click += (s, e) => Close();
                Controls.Add(lbl);
                Controls.Add(b1);
                Controls.Add(b2);
                b1.Focus();
            }
        }
    }
}
