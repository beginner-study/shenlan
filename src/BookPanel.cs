using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DeepBlue
{
    // 展开态 · 一本打开的书：整窗深林绿，只有左右两页纸面是米白
    // 全局 1.5 倍缩放：窗口 1440x1026，四角圆角（Region）。
    // 子控件 Bounds/字体用 Ui.X() 实算；自绘 OnPaint 里 ScaleTransform 后按 960x684 虚拟坐标画。
    // 虚拟几何（绿边为 1/4 版的 1.5 倍）：纸页 (20,15,920,647) 圆角；
    // 左页 440 + 中缝 56 + 右页 424；···/× 在右页右上角
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
        public readonly Label LblStatus;
        private readonly GoldBar _progress;
        private readonly TopBar _top;
        private readonly Label _head;
        private readonly ChipButton _btnDots;
        private readonly ChipButton _btnClose;

        public event EventHandler DotsClick;
        public event EventHandler CloseClick;

        public BookPanel()
        {
            BackColor = Ui.ForestShell;
            Dock = DockStyle.Fill;

            // 顶条：仅覆盖顶部绿边（不遮纸页），承担窗口拖动
            _top = new TopBar();
            _top.Bounds = new Rectangle(0, 0, Ui.X(960), Ui.X(10));
            Controls.Add(_top);

            // 书壳：与整窗同色（深林绿），承载纸页与页堆叠线
            Panel shell = new Panel();
            shell.Bounds = new Rectangle(0, 0, Ui.X(960), Ui.X(684));
            shell.BackColor = Ui.ForestShell;
            // 四周绿边可拖动窗口
            shell.MouseDown += delegate (object s, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left)
                {
                    ReleaseCapture();
                    SendMessage(Handle, WM_NCLBUTTONDOWN, (IntPtr)HTCAPTION, IntPtr.Zero);
                }
            };
            shell.Paint += delegate (object s, PaintEventArgs e)
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.ScaleTransform(Ui.Scale, Ui.Scale);
                // 纸页下缘的页堆叠线（一叠纸的侧面）
                using (Pen p1 = new Pen(Color.FromArgb(214, 204, 176), 1F))
                    g.DrawLine(p1, 36, 665, 924, 665);
                using (Pen p2 = new Pen(Color.FromArgb(206, 196, 168), 1F))
                    g.DrawLine(p2, 40, 668, 920, 668);
                using (Pen p3 = new Pen(Color.FromArgb(198, 188, 160), 1F))
                    g.DrawLine(p3, 44, 671, 916, 671);
            };
            Controls.Add(shell);

            // 纸页（对开双页 + 中缝）：圆角纸面浮在绿底上
            Panel paper = new Panel();
            paper.Bounds = Ui.XR(20, 15, 920, 647);
            paper.BackColor = Ui.Paper;
            SetRoundRegion(paper, Ui.X(14));
            paper.Paint += delegate (object s, PaintEventArgs e)
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.ScaleTransform(Ui.Scale, Ui.Scale);
                using (GraphicsPath pr = RoundPath(new Rectangle(1, 1, 918, 645), 14, 0))
                using (Pen edge = new Pen(Color.FromArgb(150, Ui.PaperEdge), 1.2F))
                    g.DrawPath(edge, pr);
            };
            shell.Controls.Add(paper);

            PageLeft = new LeftPage();
            PageLeft.Bounds = new Rectangle(0, 0, Ui.X(440), Ui.X(647));
            paper.Controls.Add(PageLeft);

            // 中缝（书脊）：页面弯入装订谷的圆柱形明暗
            Panel spine = new Panel();
            spine.Bounds = new Rectangle(Ui.X(440), 0, Ui.X(56), Ui.X(647));
            spine.BackColor = Ui.Paper;
            spine.Paint += delegate (object s, PaintEventArgs e)
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.ScaleTransform(Ui.Scale, Ui.Scale);
                Rectangle r = new Rectangle(0, 0, 56, 647); // 虚拟坐标
                int cx = r.Width / 2;
                using (LinearGradientBrush b = new LinearGradientBrush(
                    r, Color.FromArgb(0, 58, 70, 56), Color.FromArgb(0, 58, 70, 56), 0F))
                {
                    ColorBlend cb = new ColorBlend(5);
                    cb.Colors = new Color[] {
                        Color.FromArgb(0, 58, 70, 56),
                        Color.FromArgb(66, 58, 70, 56),
                        Color.FromArgb(120, 40, 52, 42),
                        Color.FromArgb(66, 58, 70, 56),
                        Color.FromArgb(0, 58, 70, 56) };
                    cb.Positions = new float[] { 0F, 0.36F, 0.5F, 0.64F, 1F };
                    b.InterpolationColors = cb;
                    g.FillRectangle(b, r);
                }
                // 装订谷核心暗线
                using (Pen core = new Pen(Color.FromArgb(130, 34, 45, 36), 2F))
                    g.DrawLine(core, cx, 2, cx, r.Height - 2);
            };
            paper.Controls.Add(spine);

            // 右页：页眉 + 文稿 + 页底控制条 + 右上角 ···/×
            Panel right = new Panel();
            right.Bounds = new Rectangle(Ui.X(496), 0, Ui.X(424), Ui.X(647));
            right.BackColor = Ui.Paper;
            paper.Controls.Add(right);

            _btnDots = new ChipButton("···");
            _btnDots.Bounds = Ui.XR(340, 8, 34, 24);
            _btnDots.Click += delegate
            {
                EventHandler h = DotsClick; if (h != null) h(this, EventArgs.Empty);
            };
            right.Controls.Add(_btnDots);

            _btnClose = new ChipButton("×");
            _btnClose.Bounds = Ui.XR(382, 8, 34, 24);
            _btnClose.Click += delegate
            {
                EventHandler h = CloseClick; if (h != null) h(this, EventArgs.Empty);
            };
            right.Controls.Add(_btnClose);

            Label head = new Label();
            head.Text = "";
            head.Font = Ui.Kai(17F * Ui.Scale, true);
            head.ForeColor = Ui.InkGreen;
            head.BackColor = Color.Transparent;
            head.AutoSize = true;
            head.Location = new Point(Ui.X(26), Ui.X(16));
            right.Controls.Add(head);
            _head = head;

            Rtb = new RichTextBox();
            Rtb.Bounds = Ui.XR(26, 64, 372, 488);
            Rtb.BorderStyle = BorderStyle.None;
            Rtb.BackColor = Ui.Paper;
            Rtb.ForeColor = Ui.InkGreenBody;
            Rtb.Font = Ui.F(11F * Ui.Scale);
            Rtb.ReadOnly = true;
            Rtb.TabStop = false;
            // false 时系统会用蓝色渲染原生选区，盖住暖金高亮——必须关掉
            Rtb.HideSelection = true;
            // 用户点击文稿框时立即清除选区，避免出现蓝色高亮
            Rtb.GotFocus += delegate { Rtb.DeselectAll(); };
            Rtb.DetectUrls = false;
            Rtb.WordWrap = true;
            right.Controls.Add(Rtb);

            // 页底控制条（收进右页内）
            LblStatus = new Label();
            LblStatus.Text = "";
            LblStatus.Font = Ui.F(8.5F * Ui.Scale);
            LblStatus.ForeColor = Ui.MutedGreen;
            LblStatus.BackColor = Color.Transparent;
            LblStatus.AutoSize = true;
            LblStatus.Location = new Point(Ui.X(26), Ui.X(577));
            right.Controls.Add(LblStatus);

            // 白底彩色字胶囊按钮：暂停/继续=蓝、停止=红
            BtnPause = CapsuleBtn("暂 停", Ui.Accent, Ui.AccentSoft);
            BtnPause.Bounds = Ui.XR(228, 592, 84, 32);
            ApplyCapsuleRegion(BtnPause);
            right.Controls.Add(BtnPause);

            BtnStop = CapsuleBtn("停 止", Ui.Danger, Ui.DangerSoft);
            BtnStop.Bounds = Ui.XR(322, 592, 84, 32);
            ApplyCapsuleRegion(BtnStop);
            right.Controls.Add(BtnStop);

            _progress = new GoldBar();
            _progress.Bounds = Ui.XR(26, 600, 150, 10);
            right.Controls.Add(_progress);

            // 右页光影：顶部光泽 + 外侧（右缘，靠书壳）内阴影 + 页眉金线（虚拟 424x647）
            right.Paint += delegate (object s, PaintEventArgs e)
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.ScaleTransform(Ui.Scale, Ui.Scale);
                int w = (int)Math.Round(right.Width / Ui.Scale);
                int h = (int)Math.Round(right.Height / Ui.Scale);
                using (Pen gold = new Pen(Color.FromArgb(201, 154, 63), 1.4F))
                    g.DrawLine(gold, 26, 54, w - 26, 54);
                using (LinearGradientBrush top = new LinearGradientBrush(
                    new Rectangle(0, 0, w, 18), Color.FromArgb(26, 255, 253, 240),
                    Color.FromArgb(0, 255, 253, 240), 90F))
                    g.FillRectangle(top, new Rectangle(0, 0, w, 18));
                using (LinearGradientBrush edge = new LinearGradientBrush(
                    new Rectangle(w - 14, 0, 14, h),
                    Color.FromArgb(0, 60, 72, 58), Color.FromArgb(22, 60, 72, 58), 0F))
                    g.FillRectangle(edge, new Rectangle(w - 14, 0, 14, h));
            };
        }

        // 展开窗口四角圆角：绿边四角随窗体裁圆
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Form f = FindForm();
            if (f != null && f.Width > 0 && f.Height > 0)
            {
                using (GraphicsPath p = new GraphicsPath())
                {
                    int d = Ui.X(24);
                    p.AddArc(0, 0, d, d, 180, 90);
                    p.AddArc(f.Width - d, 0, d, d, 270, 90);
                    p.AddArc(f.Width - d, f.Height - d, d, d, 0, 90);
                    p.AddArc(0, f.Height - d, d, d, 90, 90);
                    p.CloseFigure();
                    f.Region = new Region(p);
                }
            }
        }

        // 右页标题：按问候语时段动态设置（晨间/午间/下午/晚间播报）
        public void SetHeadTitle(string title)
        {
            string spaced = "";
            foreach (char c in title)
            {
                if (spaced.Length > 0) spaced += " ";
                spaced += c;
            }
            _head.Text = spaced;
        }

        public void SetProgress(int cur, int total)
        {
            _progress.Set(cur, total);
        }

        // 播报进行中才显示进度条
        public void SetVisibleForPlaying(bool playing)
        {
            _progress.Visible = playing;
        }

        // 圆角矩形路径
        private static GraphicsPath RoundPath(Rectangle r, int d, int inset)
        {
            GraphicsPath p = new GraphicsPath();
            int x = r.X + inset, y = r.Y + inset;
            int w = r.Width - inset * 2, h = r.Height - inset * 2;
            p.AddArc(x, y, d, d, 180, 90);
            p.AddArc(x + w - d, y, d, d, 270, 90);
            p.AddArc(x + w - d, y + h - d, d, d, 0, 90);
            p.AddArc(x, y + h - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        private static void SetRoundRegion(Control c, int d)
        {
            using (GraphicsPath p = new GraphicsPath())
            {
                p.AddArc(0, 0, d, d, 180, 90);
                p.AddArc(c.Width - d, 0, d, d, 270, 90);
                p.AddArc(c.Width - d, c.Height - d, d, d, 0, 90);
                p.AddArc(0, c.Height - d, d, d, 90, 90);
                p.CloseFigure();
                c.Region = new Region(p);
            }
        }

        // 白底彩色字胶囊按钮（fore=字色，hover=悬停底色）
        private static Button CapsuleBtn(string text, Color fore, Color hover)
        {
            Button b = new Button();
            b.Text = text;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.BackColor = Color.FromArgb(250, 252, 247);
            b.FlatAppearance.MouseOverBackColor = hover;
            b.ForeColor = fore;
            b.Font = Ui.F(9.5F * Ui.Scale);
            b.Cursor = Cursors.Hand;
            b.TabStop = false;
            return b;
        }

        // 给按钮裁胶囊外形（Region 按钮尺寸）
        private static void ApplyCapsuleRegion(Control c)
        {
            if (c.Width <= 0 || c.Height <= 0) return;
            using (GraphicsPath p = new GraphicsPath())
            {
                int d = c.Height - 1;
                p.AddArc(0, 0, d, d, 180, 90);
                p.AddArc(c.Width - d - 1, 0, d, d, 270, 90);
                p.AddArc(c.Width - d - 1, c.Height - d - 1, d, d, 0, 90);
                p.AddArc(0, c.Height - d - 1, d, d, 90, 90);
                p.CloseFigure();
                c.Region = new Region(p);
            }
        }

        // 自绘金色进度条（琥珀渐变 · 胶囊槽；按控件实际尺寸绘制）
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
                int d = Height - 1;
                using (GraphicsPath p = new GraphicsPath())
                {
                    p.AddArc(0, 0, d, d, 180, 90);
                    p.AddArc(Width - d - 1, 0, d, d, 270, 90);
                    p.AddArc(Width - d - 1, Height - d - 1, d, d, 0, 90);
                    p.AddArc(0, Height - d - 1, d, d, 90, 90);
                    p.CloseFigure();
                    using (SolidBrush bg = new SolidBrush(Ui.PaperEdge))
                        g.FillPath(bg, p);
                    RectangleF fill = new RectangleF(1, 1.5F,
                        Math.Max(0, (Width - 2) * _p), Height - 3);
                    if (fill.Width > d)
                    {
                        using (GraphicsPath fp = new GraphicsPath())
                        {
                            fp.AddArc(0, 0, d, d, 180, 90);
                            fp.AddArc(fill.Width - d, 0, d, d, 270, 90);
                            fp.AddArc(fill.Width - d, Height - d - 1, d, d, 0, 90);
                            fp.AddArc(0, Height - d - 1, d, d, 90, 90);
                            fp.CloseFigure();
                            using (LinearGradientBrush gold = new LinearGradientBrush(
                                fill, Ui.GoldHi, Ui.GoldDeep, 0F))
                                g.FillPath(gold, fp);
                        }
                    }
                }
            }
        }

        // 右页右上角的玻璃小钮（··· / ×）：白纸上的深绿玻璃
        private class ChipButton : Control
        {
            private bool _hover;
            private readonly string _glyph;

            public ChipButton(string glyph)
            {
                _glyph = glyph;
                SetStyle(ControlStyles.AllPaintingInWmPaint
                    | ControlStyles.OptimizedDoubleBuffer
                    | ControlStyles.UserPaint
                    | ControlStyles.ResizeRedraw, true);
                Cursor = Cursors.Hand;
                TabStop = false;
            }

            protected override void OnMouseEnter(EventArgs e)
            {
                _hover = true; Invalidate();
                base.OnMouseEnter(e);
            }

            protected override void OnMouseLeave(EventArgs e)
            {
                _hover = false; Invalidate();
                base.OnMouseLeave(e);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.ScaleTransform(Ui.Scale, Ui.Scale);
                Rectangle r = new Rectangle(0, 0,
                    (int)Math.Round(Width / Ui.Scale),
                    (int)Math.Round(Height / Ui.Scale));
                using (GraphicsPath p = RoundPath(r, 8, 0))
                {
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(
                        _hover ? 56 : 30, 50, 64, 50)))
                        g.FillPath(b, p);
                    using (Pen pen = new Pen(Color.FromArgb(
                        _hover ? 150 : 90, 50, 64, 50), 1F))
                        g.DrawPath(pen, p);
                }
                using (Brush t = new SolidBrush(Color.FromArgb(52, 68, 52)))
                using (Font f = Ui.F(9.5F))
                using (StringFormat sf = new StringFormat())
                {
                    sf.Alignment = StringAlignment.Center;
                    sf.LineAlignment = StringAlignment.Center;
                    g.DrawString(_glyph, f, t, r, sf);
                }
            }
        }

        // 顶条：透明拖动条（按钮已移入右页）
        private class TopBar : Control
        {
            public TopBar()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint
                    | ControlStyles.OptimizedDoubleBuffer
                    | ControlStyles.UserPaint
                    | ControlStyles.ResizeRedraw, true);
                Cursor = Cursors.Default;
                TabStop = false;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                // 透明：仅承担顶部拖动
            }

            protected override void OnMouseDown(MouseEventArgs e)
            {
                Focus();
                if (e.Button == MouseButtons.Left)
                {
                    ReleaseCapture();
                    SendMessage(FindForm().Handle, WM_NCLBUTTONDOWN,
                        (IntPtr)HTCAPTION, IntPtr.Zero);
                    return;
                }
                base.OnMouseDown(e);
            }
        }
    }
}
