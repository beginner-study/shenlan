using System;
using System.Drawing;
using System.Windows.Forms;

namespace DeepBlue
{
    public static class Ui
    {
        public static readonly Color Bg = Color.FromArgb(244, 247, 252);
        public static readonly Color Card = Color.White;
        public static readonly Color Ink = Color.FromArgb(22, 35, 58);
        public static readonly Color Muted = Color.FromArgb(91, 107, 132);
        public static readonly Color Rule = Color.FromArgb(226, 232, 244);
        public static readonly Color Accent = Color.FromArgb(29, 78, 216);
        public static readonly Color AccentDark = Color.FromArgb(26, 68, 196);
        public static readonly Color AccentSoft = Color.FromArgb(214, 230, 255);
        public static readonly Color Danger = Color.FromArgb(220, 38, 38);
        public static readonly Color DangerSoft = Color.FromArgb(254, 226, 226);
        public static readonly Color Ok = Color.FromArgb(22, 163, 74);
        public static readonly Color OkSoft = Color.FromArgb(219, 242, 229);

        public static readonly string FontFamily = "Microsoft YaHei UI";

        public static Font F(float size)
        {
            return new Font(FontFamily, size, FontStyle.Regular, GraphicsUnit.Point);
        }

        public static Font F(float size, FontStyle style)
        {
            return new Font(FontFamily, size, style, GraphicsUnit.Point);
        }

        public static Color PriorityColor(string p)
        {
            if (p == "P0") return Danger;
            if (p == "P1") return Color.FromArgb(234, 88, 12);
            if (p == "P2") return Color.FromArgb(91, 107, 132);
            return Color.FromArgb(148, 163, 184);
        }

        public static Button PrimaryButton(string text, int width, int height)
        {
            Button b = new Button();
            b.Text = text;
            b.Width = width;
            b.Height = height;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = AccentDark;
            b.BackColor = Accent;
            b.ForeColor = Color.White;
            b.Font = F(height >= 44 ? 12F : 9F, height >= 44 ? FontStyle.Bold : FontStyle.Regular);
            b.Cursor = Cursors.Hand;
            b.TabStop = false;
            return b;
        }

        public static Button GhostButton(string text, int width, int height)
        {
            Button b = new Button();
            b.Text = text;
            b.Width = width;
            b.Height = height;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 1;
            b.FlatAppearance.BorderColor = Rule;
            b.FlatAppearance.MouseOverBackColor = Bg;
            b.BackColor = Card;
            b.ForeColor = Ink;
            b.Font = F(9F);
            b.Cursor = Cursors.Hand;
            b.TabStop = false;
            return b;
        }

        public static Button DangerGhostButton(string text, int width, int height)
        {
            Button b = GhostButton(text, width, height);
            b.ForeColor = Danger;
            b.FlatAppearance.MouseOverBackColor = DangerSoft;
            return b;
        }

        public static Label CardTitle(string text)
        {
            Label l = new Label();
            l.Text = text;
            l.AutoSize = true;
            l.Font = F(10F, FontStyle.Bold);
            l.ForeColor = Ink;
            return l;
        }

        public static Label FieldLabel(string text)
        {
            Label l = new Label();
            l.Text = text;
            l.AutoSize = true;
            l.Font = F(9F);
            l.ForeColor = Muted;
            return l;
        }

        public static void PaintCardBorder(object sender, PaintEventArgs e, bool dashed)
        {
            Control c = (Control)sender;
            using (Pen p = new Pen(dashed ? Accent : Rule, dashed ? 1.6F : 1F))
            {
                if (dashed) p.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                e.Graphics.DrawRectangle(p, 0, 0, c.Width - 1, c.Height - 1);
            }
        }

        public static void PaintItemBorder(object sender, PaintEventArgs e)
        {
            Control c = (Control)sender;
            using (Pen p = new Pen(Rule, 1F))
            {
                e.Graphics.DrawRectangle(p, 0, 0, c.Width - 1, c.Height - 1);
            }
        }
    }
}
