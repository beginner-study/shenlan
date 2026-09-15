using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;

namespace DeepBlue
{
    // 封面资源：插画加载 + 大篆金字合成
    // 合成后的位图同时用于封面态绘制与 WPF 3D 翻页贴图，保证两处视觉一致
    public static class Assets
    {
        private static Bitmap _cover;

        public static Bitmap Cover
        {
            get
            {
                if (_cover == null) _cover = BuildCover();
                return _cover;
            }
        }

        private static Bitmap LoadRaw()
        {
            try
            {
                string path = Ui.AssetPath("cover-forest.jpg");
                if (path != null)
                {
                    using (Bitmap b = new Bitmap(path))
                    {
                        return new Bitmap(b); // 释放文件句柄
                    }
                }
            }
            catch (Exception) { }
            return null;
        }

        // 封面 3:4，基准 450x600，含金字
        private static Bitmap BuildCover()
        {
            Bitmap raw = LoadRaw();
            if (raw == null) raw = BuildFallback();
            Bitmap canvas = new Bitmap(450, 600, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(canvas))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                g.DrawImage(raw, new Rectangle(0, 0, 450, 600));
                RemoveWatermark(canvas);
                ApplyRoundedCorners(canvas, 8);
                PaintTitle(g);
            }
            raw.Dispose();
            return canvas;
        }

        private static Bitmap BuildFallback()
        {
            // 封面缺失时的深林绿兜底（纵向渐变 + 金束光）
            Bitmap b = new Bitmap(450, 600, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(b))
            using (LinearGradientBrush bg = new LinearGradientBrush(
                new Rectangle(0, 0, 450, 600),
                Color.FromArgb(24, 84, 56), Color.FromArgb(8, 42, 27), 90F))
            {
                g.FillRectangle(bg, 0, 0, 450, 600);
                using (GraphicsPath beam = new GraphicsPath())
                {
                    beam.AddPolygon(new Point[] {
                        new Point(450, 40), new Point(450, 300), new Point(120, 600), new Point(60, 600) });
                    using (PathGradientBrush pgb = new PathGradientBrush(beam))
                    {
                        pgb.CenterColor = Color.FromArgb(90, 227, 185, 95);
                        pgb.SurroundColors = new Color[] { Color.FromArgb(0, 227, 185, 95) };
                        g.FillPath(pgb, beam);
                    }
                }
            }
            return b;
        }

        // 插画右下角"AI生成"水印（原图 768x1024 约 (640,940)-(768,1010)）：
        // 折算 450x600 画布约 (375,548)-(450,596)。取左侧同高度植被镜像克隆 + 羽化覆盖。
        private static void RemoveWatermark(Bitmap canvas)
        {
            int x = 360, y = 540, w = 90, h = 60;
            Rectangle src = new Rectangle(x - w - 6, y, w, h);
            if (src.X < 0 || src.Right > canvas.Width || y + h > canvas.Height) return;
            using (Bitmap patch = new Bitmap(w, h, PixelFormat.Format32bppArgb))
            {
                using (Graphics pg = Graphics.FromImage(patch))
                {
                    pg.DrawImage(canvas, new Rectangle(0, 0, w, h), src, GraphicsUnit.Pixel);
                }
                patch.RotateFlip(RotateFlipType.RotateNoneFlipX);
                FeatherEdges(patch, 8);
                using (Graphics g = Graphics.FromImage(canvas))
                {
                    g.DrawImage(patch, new Point(x, y));
                }
            }
        }

        private static void FeatherEdges(Bitmap patch, int feather)
        {
            int w = patch.Width, h = patch.Height;
            for (int py = 0; py < h; py++)
            {
                for (int px = 0; px < w; px++)
                {
                    int m = Math.Min(Math.Min(px, w - 1 - px), Math.Min(py, h - 1 - py));
                    if (m >= feather) continue;
                    Color c = patch.GetPixel(px, py);
                    patch.SetPixel(px, py, Color.FromArgb(
                        c.A * (m + 1) / feather, c.R, c.G, c.B));
                }
            }
        }

        // 大篆「深蓝」金字：画面正中、随右上丁达尔光束方向，双层金晕 + 三段鎏金渐变 + 深绿投影
        private static void PaintTitle(Graphics g)
        {
            FontFamily seal = Ui.SealFamily();
            float size = 150F;
            float gap = size * 0.14F;
            using (StringFormat sf = new StringFormat())
            {
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;
                sf.FormatFlags = StringFormatFlags.NoWrap | StringFormatFlags.NoClip;
                sf.Trimming = StringTrimming.None;

                SizeF m = g.MeasureString("深", Ui.Seal(size), PointF.Empty, sf);
                float cw = Math.Max(m.Width, size * 0.55F);
                float total = cw * 2 + gap;
                // 大篆字面偏下：cy 上移 0.39×size 令字形中心落在画面正中 (300)
                float cx = 225F, cy = 300F - size * 0.39F;
                float top = cy - size * 0.62F;
                float boxH = size * 1.5F;

                RectangleF r1 = new RectangleF(cx - total / 2, top, cw, boxH);
                RectangleF r2 = new RectangleF(cx - total / 2 + cw + gap, top, cw, boxH);
                DrawSealChar(g, seal, "深", r1, size, sf);
                DrawSealChar(g, seal, "蓝", r2, size, sf);
            }
        }

        private static void DrawSealChar(Graphics g, FontFamily fam, string ch,
            RectangleF box, float size, StringFormat sf)
        {
            // 1. 双层金色光晕：宽幅柔光（呼应林间光束）+ 紧贴亮晕，克制以免糊化笔画
            DrawHalo(g, fam, ch, box, size, 1.30F, 58, sf);
            DrawHalo(g, fam, ch, box, size, 1.14F, 100, sf);

            using (GraphicsPath p = new GraphicsPath())
            {
                p.AddString(ch, fam, 0, size, box, sf);

                // 2. 深绿投影（字形沉入林影）
                using (Matrix shift = new Matrix())
                {
                    shift.Translate(3.5F, 7F);
                    p.Transform(shift);
                    using (SolidBrush sh = new SolidBrush(Color.FromArgb(165, 3, 28, 16)))
                        g.FillPath(sh, p);
                    shift.Translate(-3.5F, -7F);
                    p.Transform(shift);
                }

                // 3. 三段鎏金主体：光束来向（右上）亮白金 → 纯金 → 深金，65° 顺光渐变
                using (LinearGradientBrush gold = new LinearGradientBrush(
                    box, Color.FromArgb(255, 254, 246),
                    Color.FromArgb(198, 142, 58), 65F))
                {
                    ColorBlend cb = new ColorBlend(3);
                    cb.Colors = new Color[] {
                        Color.FromArgb(255, 254, 246),
                        Color.FromArgb(247, 217, 126),
                        Color.FromArgb(198, 142, 58) };
                    cb.Positions = new float[] { 0F, 0.52F, 1F };
                    gold.InterpolationColors = cb;
                    g.FillPath(gold, p);
                }
                using (Pen edge = new Pen(Color.FromArgb(160, 139, 92, 32), 1.2F))
                    g.DrawPath(edge, p);
            }
        }

        private static void DrawHalo(Graphics g, FontFamily fam, string ch,
            RectangleF box, float size, float scale, int alpha, StringFormat sf)
        {
            using (GraphicsPath halo = new GraphicsPath())
            {
                float gx = box.Width * (scale - 1F) / 2F;
                float gy = box.Height * (scale - 1F) / 2F;
                RectangleF hb = new RectangleF(box.X - gx, box.Y - gy,
                    box.Width * scale, box.Height * scale);
                halo.AddString(ch, fam, 0, size * scale, hb, sf);
                using (PathGradientBrush pg = new PathGradientBrush(halo))
                {
                    pg.CenterColor = Color.FromArgb(alpha, 255, 238, 186);
                    pg.SurroundColors = new Color[] { Color.FromArgb(0, 255, 238, 186) };
                    g.FillPath(pg, halo);
                }
            }
        }

        private static GraphicsPath RoundedRect(int x, int y, int w, int h, int r)
        {
            GraphicsPath p = new GraphicsPath();
            p.AddArc(x, y, r * 2, r * 2, 180, 90);
            p.AddArc(x + w - r * 2, y, r * 2, r * 2, 270, 90);
            p.AddArc(x + w - r * 2, y + h - r * 2, r * 2, r * 2, 0, 90);
            p.AddArc(x, y + h - r * 2, r * 2, r * 2, 90, 90);
            p.CloseFigure();
            return p;
        }

        // 把位图四角抹成透明（用于无缝融入任何底色）
        private static void ApplyRoundedCorners(Bitmap bmp, int r)
        {
            using (GraphicsPath p = RoundedRect(0, 0, bmp.Width, bmp.Height, r))
            using (Region clip = new Region(p))
            {
                Rectangle full = new Rectangle(0, 0, bmp.Width, bmp.Height);
                using (Region outside = new Region(full))
                {
                    outside.Exclude(clip);
                    using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.CompositingMode = CompositingMode.SourceCopy;
                    g.FillRegion(Brushes.Transparent, outside);
                }
                }
            }
        }
    }
}
