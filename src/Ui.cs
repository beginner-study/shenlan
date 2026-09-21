using System;
using System.Drawing;
using System.Drawing.Text;
using System.Windows.Forms;

namespace DeepBlue
{
    public static class Ui
    {
        // 全局界面缩放：初始态 452x602→678x903，展开态 960x684→1440x1026（1.5 倍）
        // 自绘 OnPaint 里用 Graphics.ScaleTransform(Scale,Scale) 按原坐标画；
        // 子控件 Bounds / 命中区 / 字号要显式乘 Scale
        public const float Scale = 1.5F;

        public static int X(float v) { return (int)Math.Round(v * Scale); }

        public static Rectangle XR(float x, float y, float w, float h)
        {
            return new Rectangle(X(x), X(y), X(w), X(h));
        }

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

        // 方案一 · 森林绿金基调（与封面插画同源）
        public static readonly Color CanvasIvory = Color.FromArgb(242, 239, 230);  // 象牙米白（封面画布）
        public static readonly Color FrameGreen = Color.FromArgb(50, 64, 50);      // 深绿（画布边框/胶囊钮）
        public static readonly Color FrameGreenHi = Color.FromArgb(58, 74, 58);    // 深绿亮边
        public static readonly Color ForestMist = Color.FromArgb(237, 242, 231);   // 晨雾绿（兼容旧引用）
        public static readonly Color ForestShell = Color.FromArgb(32, 52, 38);     // 深林绿（书壳/展开态画布/胶囊钮）
        public static readonly Color ForestShellHi = Color.FromArgb(44, 66, 50);   // 书壳亮边
        public static readonly Color Paper = Color.FromArgb(247, 243, 227);        // 米白纸页（暖）
        public static readonly Color PaperEdge = Color.FromArgb(224, 217, 188);    // 纸页描边
        public static readonly Color InkGreen = Color.FromArgb(31, 61, 44);         // 页眉深绿
        public static readonly Color InkGreenBody = Color.FromArgb(55, 66, 59);    // 正文墨绿
        public static readonly Color MutedGreen = Color.FromArgb(122, 139, 125);   // 辅助灰绿
        public static readonly Color Gold = Color.FromArgb(201, 154, 63);          // 晨光金
        public static readonly Color GoldHi = Color.FromArgb(227, 185, 95);         // 金亮
        public static readonly Color GoldDeep = Color.FromArgb(160, 120, 38);      // 金深
        public static readonly Color GoldSoft = Color.FromArgb(240, 224, 160);     // 当前句暖金底
        public static readonly Color GoldInk = Color.FromArgb(74, 90, 42);          // 高亮句墨色

        public static readonly string FontFamily = "Microsoft YaHei UI";

        private static FontFamily _sealFamily;

        // 像素字号：程序声明 DPI 感知后，Point 单位会随系统缩放变大导致布局溢出，
        // 统一按 96dpi 基准（pt*4/3=px）锁定像素，与设计稿 1:1
        public static Font F(float size)
        {
            return new Font(FontFamily, size * 4f / 3f, FontStyle.Regular, GraphicsUnit.Pixel);
        }

        public static Font F(float size, FontStyle style)
        {
            return new Font(FontFamily, size * 4f / 3f, style, GraphicsUnit.Pixel);
        }

        // 私有大篆字体（OFL 开源，随软件分发，无需用户安装）
        public static FontFamily SealFamily()
        {
            if (_sealFamily != null) return _sealFamily;
            try
            {
                string path = AssetPath("JFZSKSealScript-V2.5.ttf");
                if (path != null)
                {
                    PrivateFontCollection pfc = new PrivateFontCollection();
                    pfc.AddFontFile(path);
                    _sealFamily = pfc.Families[0];
                }
            }
            catch (Exception) { }
            if (_sealFamily == null) _sealFamily = System.Drawing.FontFamily.GenericSerif;
            return _sealFamily;
        }

        public static Font Seal(float sizePx)
        {
            return new Font(SealFamily(), sizePx, FontStyle.Regular, GraphicsUnit.Pixel);
        }

        // 页眉题字：楷体
        public static Font Kai(float sizePx, bool bold)
        {
            return new Font("KaiTi", sizePx, bold ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Pixel);
        }

        public static Font Mono(float sizePx)
        {
            return new Font("Consolas", sizePx, FontStyle.Regular, GraphicsUnit.Pixel);
        }

        // 资源搜索：安装目录 / assets 子目录
        public static string AssetPath(string name)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string p1 = System.IO.Path.Combine(baseDir, name);
            if (System.IO.File.Exists(p1)) return p1;
            string p2 = System.IO.Path.Combine(baseDir, "assets", name);
            if (System.IO.File.Exists(p2)) return p2;
            return null;
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
