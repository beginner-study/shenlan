using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace DeepBlue
{
    // 「···」内嵌菜单：极简毛玻璃悬浮（日程管理 / 设置）
    public class GlassMenu : Form
    {
        public event Action<int> ItemChosen;

        private readonly string[] _items;
        private readonly Color _ink;
        private int _hover = -1;

        private const int TopPad = 12;   // 顶部留白（不可再大，否则显得多一栏空白）
        private const int RowH = 46;     // 行高（字号 11px 档）

        public GlassMenu(string[] items, Color ink)
        {
            _items = items;
            _ink = ink;

            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            BackColor = Color.FromArgb(248, 250, 246);
            Font = Ui.F(11F);
            ClientSize = new Size(156, TopPad + _items.Length * RowH + 8);

            SetStyle(ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.UserPaint, true);
        }

        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            Close();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x80; // WS_EX_TOOLWINDOW：不出现在 Alt+Tab
                return cp;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.HighQuality;

            using (GraphicsPath p = new GraphicsPath())
            {
                int d = 16;
                p.AddArc(0, 0, d, d, 180, 90);
                p.AddArc(Width - d - 1, 0, d, d, 270, 90);
                p.AddArc(Width - d - 1, Height - d - 1, d, d, 0, 90);
                p.AddArc(0, Height - d - 1, d, d, 90, 90);
                p.CloseFigure();
                // 近似毛玻璃：高不透明白 + 淡金内衬
                using (SolidBrush b = new SolidBrush(Color.FromArgb(242, 248, 243)))
                    g.FillPath(b, p);
                using (Pen edge = new Pen(Color.FromArgb(190, 213, 196), 1F))
                    g.DrawPath(edge, p);
            }

            using (StringFormat sf = new StringFormat())
            {
                sf.Alignment = StringAlignment.Near;
                sf.LineAlignment = StringAlignment.Center;
                for (int i = 0; i < _items.Length; i++)
                {
                    Rectangle r = new Rectangle(10, TopPad + i * RowH, Width - 20, RowH);
                    if (i == _hover)
                    {
                        using (GraphicsPath hp = new GraphicsPath())
                        {
                            int d = 10;
                            hp.AddArc(r.X, r.Y, d, d, 180, 90);
                            hp.AddArc(r.Right - d, r.Y, d, d, 270, 90);
                            hp.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
                            hp.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
                            hp.CloseFigure();
                            using (SolidBrush hb = new SolidBrush(Color.FromArgb(220, 234, 224)))
                                g.FillPath(hb, hp);
                        }
                    }
                    // 前缀点：日程=金、设置=绿
                    using (Brush dot = new SolidBrush(i == 0 ? Ui.Gold : Color.FromArgb(46, 110, 74)))
                        g.FillEllipse(dot, r.X + 8, r.Y + (RowH - 8) / 2, 8, 8);
                    using (Brush t = new SolidBrush(i == _hover ? _ink : Ui.InkGreenBody))
                        g.DrawString(_items[i], Font, t,
                            new RectangleF(r.X + 26, r.Y, r.Width - 30, r.Height), sf);
                }
            }
        }

        private int Hit(Point pt)
        {
            for (int i = 0; i < _items.Length; i++)
                if (new Rectangle(0, TopPad + i * RowH, Width, RowH).Contains(pt)) return i;
            return -1;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            int h = Hit(e.Location);
            if (h != _hover) { _hover = h; Invalidate(); }
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hover = -1; Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            int h = Hit(e.Location);
            if (h >= 0)
            {
                Action<int> cb = ItemChosen;
                Close();
                if (cb != null) cb(h);
            }
            base.OnMouseClick(e);
        }
    }
}
