using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DeepBlue
{
    // 方案一 · 初始态：合上的书（封面即整个窗口）
    // 一切控件（··· / × / 翻开新的一页）皆为封面插画的一部分
    public class CoverPanel : Control
    {
        [DllImport("user32.dll")] private static extern bool ReleaseCapture();
        [DllImport("user32.dll")] private static extern IntPtr SendMessage(
            IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HTCAPTION = 2;

        public event EventHandler OpenClick;
        public event EventHandler DotsClick;
        public event EventHandler CloseClick;

        private bool _openEnabled;
        private int _hover = -1; // 0=··· 1=× 2=按钮
        private readonly Rectangle[] _hits = new Rectangle[3];

        public CoverPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.UserPaint
                | ControlStyles.ResizeRedraw, true);
            Cursor = Cursors.Hand;

            _hits[0] = new Rectangle(452 - 96, 18, 40, 40);  // ×
            _hits[1] = new Rectangle(452 - 50, 18, 40, 40);  // ···
            _hits[2] = new Rectangle(106, 516, 240, 54);      // 翻开新的一页
        }

        public bool OpenEnabled
        {
            get { return _openEnabled; }
            set { _openEnabled = value; Invalidate(); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            // 封面插画 + 大篆金字（合成位图）
            Bitmap cover = Assets.Cover;
            g.DrawImage(cover, new Rectangle(1, 1, 450, 600));

            DrawGlassChip(g, _hits[1], _hover == 1);
            DrawGlassChip(g, _hits[0], _hover == 0);

            // ×
            using (Pen p = new Pen(Color.FromArgb(235, 248, 244), 1.8F))
            {
                p.StartCap = LineCap.Round; p.EndCap = LineCap.Round;
                Rectangle r = _hits[0];
                g.DrawLine(p, r.X + 15, r.Y + 15, r.X + 25, r.Y + 25);
                g.DrawLine(p, r.X + 25, r.Y + 15, r.X + 15, r.Y + 25);
            }
            // ···
            using (Brush b = new SolidBrush(Color.FromArgb(235, 248, 244)))
            {
                Rectangle r = _hits[1];
                for (int i = 0; i < 3; i++)
                {
                    g.FillEllipse(b, r.X + 11 + i * 5, r.Y + 18, 4.4F, 4.4F);
                }
            }

            DrawOpenButton(g, _hits[2], _hover == 2, _openEnabled);
        }

        // 半透明白玻璃圆钮
        private void DrawGlassChip(Graphics g, Rectangle r, bool hover)
        {
            using (GraphicsPath p = RoundPath(r, 12))
            {
                using (SolidBrush b = new SolidBrush(
                    Color.FromArgb(hover ? 70 : 48, 255, 255, 255)))
                    g.FillPath(b, p);
                using (Pen pen = new Pen(Color.FromArgb(hover ? 170 : 105, 255, 255, 255), 1F))
                    g.DrawPath(pen, p);
            }
        }

        // 「翻开新的一页」：墨绿玻璃底 + 描金细边 + 柔光
        private void DrawOpenButton(Graphics g, Rectangle r, bool hover, bool enabled)
        {
            int glow = !enabled ? 0 : hover ? 130 : 80;
            using (GraphicsPath halo = new GraphicsPath())
            {
                halo.AddEllipse(r.X - 7, r.Y - 7, r.Width + 14, r.Height + 14);
                using (PathGradientBrush pg = new PathGradientBrush(halo))
                {
                    pg.CenterColor = Color.FromArgb(glow, 255, 230, 160);
                    pg.SurroundColors = new Color[] { Color.FromArgb(0, 255, 230, 160) };
                    if (glow > 0) g.FillPath(pg, halo);
                }
            }
            using (GraphicsPath p = RoundPath(r, 26))
            {
                using (LinearGradientBrush bg = new LinearGradientBrush(
                    r, Color.FromArgb(235, 11, 46, 30), Color.FromArgb(200, 6, 32, 20), 90F))
                    g.FillPath(bg, p);
                using (Pen gold = new Pen(
                    enabled ? Color.FromArgb(hover ? 255 : 205, 227, 185, 95)
                            : Color.FromArgb(120, 160, 150, 120), 1.2F))
                    g.DrawPath(gold, p);
            }
            using (StringFormat sf = new StringFormat())
            {
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;
                using (SolidBrush t = new SolidBrush(enabled
                    ? Color.FromArgb(hover ? 255 : 235, 244, 224, 170)
                    : Color.FromArgb(150, 200, 195, 175)))
                {
                    g.DrawString("翻 开 新 的 一 页",
                        new Font("Microsoft YaHei UI", 11.5F, FontStyle.Bold),
                        t, new RectangleF(r.X, r.Y + 1, r.Width, r.Height), sf);
                }
            }
        }

        private static GraphicsPath RoundPath(Rectangle r, int rad)
        {
            GraphicsPath p = new GraphicsPath();
            int d = rad * 2;
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        private int HitTest(Point pt)
        {
            for (int i = 0; i < _hits.Length; i++)
                if (_hits[i].Contains(pt)) return i;
            return -1;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            int h = HitTest(e.Location);
            if (h != _hover)
            {
                _hover = h;
                Invalidate();
                Cursor = (h >= 0 && (h != 2 || _openEnabled)) ? Cursors.Hand : Cursors.Default;
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            if (_hover != -1) { _hover = -1; Invalidate(); Cursor = Cursors.Default; }
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            Focus();
            if (e.Button == MouseButtons.Left)
            {
                if (HitTest(e.Location) < 0)
                {
                    // 非控件区：拖动窗口
                    ReleaseCapture();
                    SendMessage(FindForm().Handle, WM_NCLBUTTONDOWN, (IntPtr)HTCAPTION, IntPtr.Zero);
                    return;
                }
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            int h = HitTest(e.Location);
            if (h == 1 && DotsClick != null) DotsClick(this, e);
            else if (h == 0 && CloseClick != null) CloseClick(this, e);
            else if (h == 2 && _openEnabled && OpenClick != null) OpenClick(this, e);
            base.OnMouseClick(e);
        }
    }
}
