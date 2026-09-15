using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DeepBlue
{
    // 展开态 · 一本打开的书：深林绿硬壳包裹米白对页
    public class BookPanel : Panel
    {
        [DllImport("user32.dll")] private static extern bool ReleaseCapture();
        [DllImport("user32.dll")] private static extern IntPtr SendMessage(
            IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HTCAPTION = 2;

        public readonly LeftPage PageLeft;
        public readonly RichTextBox Rtb;
        public readonly Button BtnPause;
        public readonly Button BtnStop;
        public readonly Button BtnReplay;
        public readonly Label LblStatus;
        private readonly GoldBar _progress;
        private readonly TopBar _top;

        public event EventHandler DotsClick;
        public event EventHandler CloseClick;

        // 展开窗口 960x684：书壳 840x615 @ (60,22)，纸页 812x587 @ (74,36)
        public BookPanel()
        {
            BackColor = Ui.ForestMist;
            Dock = DockStyle.Fill;

            _top = new TopBar();
            _top.Bounds = new Rectangle(0, 0, 960, 30);
            _top.DotsClick += delegate
            {
                EventHandler h = DotsClick; if (h != null) h(this, EventArgs.Empty);
            };
            _top.CloseClick += delegate
            {
                EventHandler h = CloseClick; if (h != null) h(this, EventArgs.Empty);
            };
            _top.SendToBack();
            Controls.Add(_top);

            Panel shell = new Panel();
            shell.Bounds = new Rectangle(60, 22, 840, 615);
            shell.BackColor = Ui.ForestShell;
            shell.Paint += delegate (object s, PaintEventArgs e)
            {
                RoundBorder(e.Graphics, shell.ClientRectangle,
                    Ui.ForestShell, Ui.ForestShellHi);
            };
            Controls.Add(shell);

            Panel paper = new Panel();
            paper.Bounds = new Rectangle(74, 36, 812, 587);
            paper.BackColor = Ui.Paper;
            shell.Controls.Add(paper);

            PageLeft = new LeftPage();
            PageLeft.Bounds = new Rectangle(0, 0, 404, 587);
            paper.Controls.Add(PageLeft);

            // 书脊阴影（双页中缝）
            Panel spine = new Panel();
            spine.Bounds = new Rectangle(404, 0, 14, 587);
            spine.Paint += delegate (object s, PaintEventArgs e)
            {
                using (LinearGradientBrush b = new LinearGradientBrush(
                    spine.ClientRectangle, Color.FromArgb(0, 3, 30, 18),
                    Color.FromArgb(0, 3, 30, 18), 0F))
                {
                    ColorBlend cb = new ColorBlend(3);
                    cb.Colors = new Color[] {
                        Color.FromArgb(0, 3, 30, 18),
                        Color.FromArgb(70, 3, 30, 18),
                        Color.FromArgb(0, 3, 30, 18) };
                    cb.Positions = new float[] { 0F, 0.5F, 1F };
                    b.InterpolationColors = cb;
                    e.Graphics.FillRectangle(b, spine.ClientRectangle);
                }
            };
            paper.Controls.Add(spine);

            // 右页：页眉 + 文稿
            Panel right = new Panel();
            right.Bounds = new Rectangle(418, 0, 394, 587);
            right.BackColor = Ui.Paper;
            right.Paint += delegate (object s, PaintEventArgs e)
            {
                using (Pen gold = new Pen(Color.FromArgb(201, 154, 63), 1.4F))
                    e.Graphics.DrawLine(gold, 22, 54, right.Width - 22, 54);
            };
            paper.Controls.Add(right);

            Label head = new Label();
            head.Text = "晨 间 播 报";
            head.Font = Ui.Kai(17F, true);
            head.ForeColor = Ui.InkGreen;
            head.BackColor = Color.Transparent;
            head.AutoSize = true;
            head.Location = new Point(22, 16);
            right.Controls.Add(head);

            Rtb = new RichTextBox();
            Rtb.Bounds = new Rectangle(22, 66, 350, 505);
            Rtb.BorderStyle = BorderStyle.None;
            Rtb.BackColor = Ui.Paper;
            Rtb.ForeColor = Ui.InkGreenBody;
            Rtb.Font = Ui.F(11F);
            Rtb.ReadOnly = true;
            Rtb.TabStop = false;
            Rtb.HideSelection = false;
            Rtb.DetectUrls = false;
            Rtb.WordWrap = true;
            right.Controls.Add(Rtb);

            // 纸页外侧内阴影（书装订的暗部）
            Panel innerShadow = new Panel();
            innerShadow.Bounds = paper.ClientRectangle;
            innerShadow.Paint += delegate (object s, PaintEventArgs e)
            {
                Graphics g = e.Graphics;
                using (LinearGradientBrush l = new LinearGradientBrush(
                    new Rectangle(0, 0, 40, 587), Color.FromArgb(36, 6, 44, 29),
                    Color.FromArgb(0, 6, 44, 29), 0F))
                    g.FillRectangle(l, new Rectangle(0, 0, 40, 587));
                using (LinearGradientBrush r = new LinearGradientBrush(
                    new Rectangle(772, 0, 40, 587), Color.FromArgb(0, 6, 44, 29),
                    Color.FromArgb(36, 6, 44, 29), 0F))
                    g.FillRectangle(r, new Rectangle(772, 0, 40, 587));
            };
            paper.Controls.Add(innerShadow);
            innerShadow.SendToBack();

            // 底部控制条（书下 · 晨雾绿上）
            BtnPause = GhostBtn("暂 停");
            BtnPause.Bounds = new Rectangle(310, 644, 86, 32);
            Controls.Add(BtnPause);

            BtnStop = GhostBtn("停 止", true);
            BtnStop.Bounds = new Rectangle(406, 644, 86, 32);
            Controls.Add(BtnStop);

            BtnReplay = GhostBtn("重新播报");
            BtnReplay.Bounds = new Rectangle(390, 640, 110, 40);
            BtnReplay.Visible = false;
            Controls.Add(BtnReplay);

            _progress = new GoldBar();
            _progress.Bounds = new Rectangle(516, 653, 240, 14);
            Controls.Add(_progress);

            LblStatus = new Label();
            LblStatus.Text = "";
            LblStatus.Font = Ui.F(9F);
            LblStatus.ForeColor = Ui.MutedGreen;
            LblStatus.AutoSize = true;
            LblStatus.Location = new Point(768, 650);
            LblStatus.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            Controls.Add(LblStatus);
        }

        private static void RoundBorder(Graphics g, Rectangle r, Color fill, Color hi)
        {
            // 圆角描边（fill 已由 BackColor 承担，这里画亮边）
            using (GraphicsPath p = new GraphicsPath())
            {
                int d = 18;
                p.AddArc(r.X + 1, r.Y + 1, d, d, 180, 90);
                p.AddArc(r.Right - d - 1, r.Y + 1, d, d, 270, 90);
                p.AddArc(r.Right - d - 1, r.Bottom - d - 1, d, d, 0, 90);
                p.AddArc(r.X + 1, r.Bottom - d - 1, d, d, 90, 90);
                p.CloseFigure();
                using (Pen pen = new Pen(Color.FromArgb(120, hi), 1F))
                    g.DrawPath(pen, p);
            }
        }

        public void SetProgress(int cur, int total)
        {
            _progress.Set(cur, total);
        }

        // 播报进行中才显示进度条；重新播报按钮由 MainForm 控制
        public void SetVisibleForPlaying(bool playing)
        {
            _progress.Visible = playing;
        }

        private static Button GhostBtn(string text, bool danger)
        {
            Button b = new Button();
            b.Text = text;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 1;
            b.FlatAppearance.BorderColor = danger
                ? Color.FromArgb(226, 178, 168) : Color.FromArgb(205, 216, 199);
            b.FlatAppearance.MouseOverBackColor = danger
                ? Color.FromArgb(244, 228, 224) : Color.FromArgb(226, 236, 224);
            b.BackColor = Color.FromArgb(250, 252, 247);
            b.ForeColor = danger ? Ui.Danger : Ui.InkGreen;
            b.Font = Ui.F(9.5F);
            b.Cursor = Cursors.Hand;
            b.TabStop = false;
            return b;
        }

        private static Button GhostBtn(string text) { return GhostBtn(text, false); }

        // 自绘金色进度条
        private class GoldBar : Control
        {
            private float _p;

            public GoldBar()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint
                    | ControlStyles.OptimizedDoubleBuffer
                    | ControlStyles.UserPaint
                    | ControlStyles.ResizeRedraw, true);
            }

            public void Set(int cur, int total)
            {
                _p = total <= 0 ? 0 : (cur + 1) / (float)total;
                if (_p < 0) _p = 0;
                if (_p > 1) _p = 1;
                Invalidate();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.HighQuality;
                using (GraphicsPath p = new GraphicsPath())
                {
                    p.AddArc(0, 2, 10, 10, 180, 90);
                    p.AddArc(Width - 11, 2, 10, 10, 270, 90);
                    p.AddArc(Width - 11, Height - 12, 10, 10, 0, 90);
                    p.AddArc(0, Height - 12, 10, 10, 90, 90);
                    p.CloseFigure();
                    using (SolidBrush bg = new SolidBrush(Color.FromArgb(222, 229, 214)))
                        g.FillPath(bg, p);
                    RectangleF fill = new RectangleF(2, 4,
                        Math.Max(0, (Width - 4) * _p), Height - 8);
                    if (fill.Width > 4)
                    {
                        using (GraphicsPath fp = new GraphicsPath())
                        {
                            fp.AddArc(0, 2, 10, 10, 180, 90);
                            fp.AddArc(fill.Width - 9, 2, 10, 10, 270, 90);
                            fp.AddArc(fill.Width - 9, Height - 12, 10, 10, 0, 90);
                            fp.AddArc(0, Height - 12, 10, 10, 90, 90);
                            fp.CloseFigure();
                            using (LinearGradientBrush gold = new LinearGradientBrush(
                                fill, Ui.GoldHi, Ui.GoldDeep, 0F))
                                g.FillPath(gold, fp);
                        }
                    }
                }
            }
        }

        // 顶条：拖动 + 右上玻璃钮
        private class TopBar : Control
        {
            public event EventHandler DotsClick;
            public event EventHandler CloseClick;
            private int _hover = -1;
            private readonly Rectangle[] _hits = new Rectangle[2];

            public TopBar()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint
                    | ControlStyles.OptimizedDoubleBuffer
                    | ControlStyles.UserPaint
                    | ControlStyles.ResizeRedraw, true);
                _hits[0] = new Rectangle(960 - 56, 6, 34, 24); // ×
                _hits[1] = new Rectangle(960 - 100, 6, 34, 24); // ···
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.HighQuality;
                for (int i = 0; i < 2; i++)
                {
                    Rectangle r = _hits[i];
                    using (GraphicsPath p = new GraphicsPath())
                    {
                        p.AddArc(r.X, r.Y, 8, 8, 180, 90);
                        p.AddArc(r.Right - 8, r.Y, 8, 8, 270, 90);
                        p.AddArc(r.Right - 8, r.Bottom - 8, 8, 8, 0, 90);
                        p.AddArc(r.X, r.Bottom - 8, 8, 8, 90, 90);
                        p.CloseFigure();
                        using (SolidBrush b = new SolidBrush(Color.FromArgb(
                            _hover == i ? 66 : 40, 255, 255, 255)))
                            g.FillPath(b, p);
                        using (Pen pen = new Pen(Color.FromArgb(
                            _hover == i ? 150 : 90, 255, 255, 255), 1F))
                            g.DrawPath(pen, p);
                    }
                    using (Brush t = new SolidBrush(Color.FromArgb(225, 240, 235)))
                    using (Font f = Ui.F(9.5F))
                    using (StringFormat sf = new StringFormat())
                    {
                        sf.Alignment = StringAlignment.Center;
                        sf.LineAlignment = StringAlignment.Center;
                        g.DrawString(i == 0 ? "×" : "···", f, t, r, sf);
                    }
                }
            }

            private int Hit(Point pt)
            {
                for (int i = 0; i < 2; i++) if (_hits[i].Contains(pt)) return i;
                return -1;
            }

            protected override void OnMouseMove(MouseEventArgs e)
            {
                int h = Hit(e.Location);
                if (h != _hover) { _hover = h; Invalidate(); }
                Cursor = h >= 0 ? Cursors.Hand : Cursors.Default;
                base.OnMouseMove(e);
            }

            protected override void OnMouseLeave(EventArgs e)
            {
                _hover = -1; Invalidate();
                base.OnMouseLeave(e);
            }

            protected override void OnMouseDown(MouseEventArgs e)
            {
                Focus();
                if (e.Button == MouseButtons.Left && Hit(e.Location) < 0)
                {
                    ReleaseCapture();
                    SendMessage(FindForm().Handle, WM_NCLBUTTONDOWN,
                        (IntPtr)HTCAPTION, IntPtr.Zero);
                    return;
                }
                base.OnMouseDown(e);
            }

            protected override void OnMouseClick(MouseEventArgs e)
            {
                int h = Hit(e.Location);
                if (h == 1 && DotsClick != null) DotsClick(this, e);
                else if (h == 0 && CloseClick != null) CloseClick(this, e);
                base.OnMouseClick(e);
            }
        }
    }
}
