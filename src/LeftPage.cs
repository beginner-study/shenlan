using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace DeepBlue
{
    // 展开态 · 左页「今日」：日期页眉、天气、今日安排、即将截止
    public class LeftPage : Control
    {
        private WeatherData _weather;
        private List<ScheduleItem> _todays = new List<ScheduleItem>();
        private List<ScheduleItem> _deadlines = new List<ScheduleItem>();
        private string _footNote = "";

        public LeftPage()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.UserPaint
                | ControlStyles.ResizeRedraw, true);
        }

        public void SetData(Store store, WeatherData weather, string footNote)
        {
            _weather = weather;
            _footNote = footNote ?? "";
            _todays = new List<ScheduleItem>();
            _deadlines = new List<ScheduleItem>();
            if (store != null)
            {
                _todays = ScriptEngine.EventsOn(store.Items, DateTime.Today);
                // 今日安排里已带截止信息的高优先级事项，不再重复进「即将截止」
                HashSet<int> covered = new HashSet<int>();
                foreach (ScheduleItem ev in _todays)
                {
                    if (ev.Priority != "P0" && ev.Priority != "P1") continue;
                    if (ScriptEngine.ParseDate(ev.Due) != null) covered.Add(ev.Id);
                }
                _deadlines = ScriptEngine.DeadlinesIn(
                    store.Items, store.Settings.WindowDays, DateTime.Today, covered);
            }
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            // 全局 1.5 倍：按 404x584 虚拟坐标绘制
            g.ScaleTransform(Ui.Scale, Ui.Scale);
            DateTime today = DateTime.Today;
            int w = (int)Math.Round(Width / Ui.Scale);
            int h = (int)Math.Round(Height / Ui.Scale);
            int pad = 26;

            // 页眉：今日 · 日期（同行，日期右对齐）
            using (Font head = Ui.Kai(17F, true))
                g.DrawString("今 日", head, new SolidBrush(Ui.InkGreen), pad, 18);
            string dateStr = today.Year + "年" + today.Month + "月" + today.Day + "日 · " + ScriptEngine.WeekShortName(today);
            using (Font df = Ui.F(9F))
            {
                SizeF ds = g.MeasureString(dateStr, df);
                g.DrawString(dateStr, df, new SolidBrush(Ui.MutedGreen), w - pad - ds.Width, 26);
            }
            using (Pen gold = new Pen(Color.FromArgb(201, 154, 63), 1.4F))
                g.DrawLine(gold, pad, 54, w - pad, 54);

            int y = 64;

            // 天气块：图标 + 大字行（天气 · 高/低温）+ 细节行（城市 · 降水）
            if (_weather != null && WeatherEngine.IsFresh(_weather))
            {
                // 图标分发与展示用同一个条件串（Text 为空时用天气代码回退）
                string condition = _weather.Text.Length > 0 ? _weather.Text
                    : WeatherEngine.CodeToCn(_weather.Code);
                DrawWeatherIcon(g, condition, pad + 14, y + 14);
                int tmax = (int)Math.Round(_weather.Tmax, MidpointRounding.AwayFromZero);
                int tmin = (int)Math.Round(_weather.Tmin, MidpointRounding.AwayFromZero);
                using (Font wf = Ui.F(12F, FontStyle.Bold))
                    g.DrawString(condition + " · " + tmax + "° / " + tmin + "°",
                        wf, new SolidBrush(Ui.InkGreen), pad + 36, y);
                string detail = _weather.City ?? "";
                if (_weather.PrecipProb >= 30)
                    detail += (detail.Length > 0 ? " · " : "") + "降水概率 " + _weather.PrecipProb + "%";
                if (detail.Length > 0)
                    using (Font df = Ui.F(8.5F))
                        g.DrawString(detail, df, new SolidBrush(Ui.MutedGreen), pad + 36, y + 26);
                y += 50;
            }
            else if (_storeWeatherOn)
            {
                using (Font wf = Ui.F(9F))
                    g.DrawString("天气暂不可用", wf, new SolidBrush(Ui.MutedGreen), pad, y);
                y += 40;
            }

            // 今日安排
            y = DrawSection(g, "今 日 安 排", y + 8, w, pad);
            if (_todays.Count == 0)
            {
                using (Font f = Ui.F(9.5F))
                    g.DrawString("今天没有录入安排，轻装上阵。",
                        f, new SolidBrush(Ui.MutedGreen), pad, y);
                y += 28;
            }
            else
            {
                int limit = Math.Min(_todays.Count, 7);
                for (int i = 0; i < limit; i++)
                {
                    DrawEvent(g, _todays[i], y, w, pad);
                    if (i < limit - 1) DrawDash(g, pad, y + 27, w - pad);
                    y += 34;
                }
                if (_todays.Count > limit)
                {
                    using (Font f = Ui.F(9F))
                        g.DrawString("… 其余 " + (_todays.Count - limit) + " 项见日程管理",
                            f, new SolidBrush(Ui.MutedGreen), pad + 60, y - 2);
                    y += 24;
                }
            }

            // 即将截止
            if (_deadlines.Count > 0)
            {
                y += 6;
                y = DrawSection(g, "即 将 截 止", y, w, pad);
                int limit = Math.Min(_deadlines.Count, 3);
                for (int i = 0; i < limit; i++)
                {
                    ScheduleItem d = _deadlines[i];
                    DateTime? due = ScriptEngine.ParseDate(d.Due);
                    int left = due == null ? 0 : (int)(due.Value.Date - DateTime.Today).TotalDays;
                    string when = left == 0 ? "今天" : left + " 天后";
                    string txt = d.Title + " · " + when;
                    using (Font f = Ui.F(9.5F))
                    using (Brush b = new SolidBrush(
                        d.Priority == "P0" ? Ui.Danger : Ui.InkGreenBody))
                        g.DrawString(txt, f, b, pad, y);
                    if (i < limit - 1) DrawDash(g, pad, y + 21, w - pad);
                    y += 26;
                }
                if (_deadlines.Count > limit)
                {
                    using (Font f = Ui.F(9F))
                        g.DrawString("… 另 " + (_deadlines.Count - limit) + " 项",
                            f, new SolidBrush(Ui.MutedGreen), pad, y);
                    y += 22;
                }
            }

            // 底部注脚
            if (_footNote.Length > 0)
            {
                using (Font f = Ui.F(8.5F))
                    g.DrawString(_footNote, f, new SolidBrush(Ui.MutedGreen), pad, h - 26);
            }

            // 纸页光影：顶部光泽 + 外侧（左缘，靠书壳）内阴影
            using (LinearGradientBrush top = new LinearGradientBrush(
                new Rectangle(0, 0, w, 18), Color.FromArgb(26, 255, 253, 240),
                Color.FromArgb(0, 255, 253, 240), 90F))
                g.FillRectangle(top, new Rectangle(0, 0, w, 18));
            using (LinearGradientBrush l = new LinearGradientBrush(
                new Rectangle(0, 0, 14, h), Color.FromArgb(22, 60, 72, 58),
                Color.FromArgb(0, 60, 72, 58), 0F))
                g.FillRectangle(l, new Rectangle(0, 0, 14, h));
            // 书脊侧（右缘）：页面弯入装订谷的起始暗部，与中缝渐变衔接
            using (LinearGradientBrush r = new LinearGradientBrush(
                new Rectangle(w - 12, 0, 12, h), Color.FromArgb(0, 60, 72, 58),
                Color.FromArgb(26, 60, 72, 58), 0F))
                g.FillRectangle(r, new Rectangle(w - 12, 0, 12, h));
        }

        // 条目间的浅虚线分隔
        private static void DrawDash(Graphics g, int x1, int y, int x2)
        {
            using (Pen p = new Pen(Color.FromArgb(214, 205, 180), 1F))
            {
                p.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                p.DashPattern = new float[] { 3F, 3F };
                g.DrawLine(p, x1, y, x2, y);
            }
        }

        private bool _storeWeatherOn;

        public void SetWeatherFlag(bool on)
        {
            _storeWeatherOn = on;
        }

        private int DrawSection(Graphics g, string title, int y, int w, int pad)
        {
            using (Font f = Ui.F(9F))
                g.DrawString(title, f, new SolidBrush(Ui.MutedGreen), pad, y);
            using (Pen p = new Pen(Color.FromArgb(226, 218, 199), 1F))
                g.DrawLine(p, pad, y + 22, w - pad, y + 22);
            return y + 32;
        }

        private void DrawEvent(Graphics g, ScheduleItem ev, int y, int w, int pad)
        {
            // 时间（晨光金 · 等宽）
            string time = string.IsNullOrEmpty(ev.Time) ? "--:--" : ev.Time;
            using (Font tf = Ui.Mono(11.5F))
                g.DrawString(time, tf, new SolidBrush(Ui.GoldDeep), pad, y);
            // 标题 + 优先级胶囊标签（右对齐）
            using (Font tf = Ui.F(10.5F))
            {
                float tx = pad + 62;
                string title = ev.Title;
                bool drawTag = ev.Priority == "P0" || ev.Priority == "P1";
                float tagW = drawTag ? 40 : 0;
                float maxW = w - pad - tx - tagW - 6;
                SizeF ts = g.MeasureString(title, tf);
                if (ts.Width > maxW)
                {
                    while (title.Length > 1 && g.MeasureString(title + "…", tf).Width > maxW)
                        title = title.Substring(0, title.Length - 1);
                    title += "…";
                }
                g.DrawString(title, tf, new SolidBrush(Ui.InkGreenBody), tx, y);
                if (drawTag)
                {
                    string tag = ev.Priority;
                    Color c = ev.Priority == "P0" ? Ui.Danger : Color.FromArgb(234, 88, 12);
                    Rectangle tagR = new Rectangle(w - pad - 40, y + 1, 40, 17);
                    using (GraphicsPath gp = new GraphicsPath())
                    {
                        int d = 8;
                        gp.AddArc(tagR.X, tagR.Y, d, d, 180, 90);
                        gp.AddArc(tagR.Right - d, tagR.Y, d, d, 270, 90);
                        gp.AddArc(tagR.Right - d, tagR.Bottom - d, d, d, 0, 90);
                        gp.AddArc(tagR.X, tagR.Bottom - d, d, d, 90, 90);
                        gp.CloseFigure();
                        using (SolidBrush bb = new SolidBrush(Color.FromArgb(26, c)))
                            g.FillPath(bb, gp);
                        using (Pen pb = new Pen(Color.FromArgb(70, c), 1F))
                            g.DrawPath(pb, gp);
                    }
                    using (Font gf = Ui.F(8F))
                    using (StringFormat sf = new StringFormat())
                    {
                        sf.Alignment = StringAlignment.Center;
                        sf.LineAlignment = StringAlignment.Center;
                        g.DrawString(tag, gf, new SolidBrush(c), tagR, sf);
                    }
                }
            }
        }

        private void DrawSun(Graphics g, float cx, float cy)
        {
            using (SolidBrush b = new SolidBrush(Color.FromArgb(227, 185, 95)))
                g.FillEllipse(b, cx - 6, cy - 6, 12, 12);
            using (Pen p = new Pen(Color.FromArgb(190, 150, 70), 1.3F))
            {
                for (int i = 0; i < 8; i++)
                {
                    double a = i * Math.PI / 4;
                    g.DrawLine(p,
                        cx + (float)Math.Cos(a) * 9.5F, cy + (float)Math.Sin(a) * 9.5F,
                        cx + (float)Math.Cos(a) * 13.5F, cy + (float)Math.Sin(a) * 13.5F);
                }
            }
        }

        // 按天气实况条件串分发图标（雷 > 雪 > 雨 > 雾 > 阴 > 多云 > 晴）
        private void DrawWeatherIcon(Graphics g, string condition, float cx, float cy)
        {
            if (condition.Contains("雷")) DrawBoltCloud(g, cx, cy);
            else if (condition.Contains("雨")) DrawRainCloud(g, cx, cy);
            else if (condition.Contains("雪")) DrawSnowCloud(g, cx, cy);
            else if (condition.Contains("雾") || condition.Contains("霾") || condition.Contains("沙")) DrawFog(g, cx, cy);
            else if (condition.Contains("阴")) DrawOvercast(g, cx, cy);
            else if (condition.Contains("云")) DrawPartlyCloudy(g, cx, cy);
            else DrawSun(g, cx, cy);
        }

        // 云朵剪影：三圆 + 底座，底边对齐
        private static void CloudBody(Graphics g, float cx, float cy, Color fill)
        {
            using (SolidBrush b = new SolidBrush(fill))
            {
                g.FillEllipse(b, cx - 12, cy - 3, 11, 9);
                g.FillEllipse(b, cx - 6, cy - 8, 12, 11);
                g.FillEllipse(b, cx + 1, cy - 3, 11, 9);
                g.FillRectangle(b, cx - 11, cy + 1, 21, 5);
            }
        }

        private static void DrawOvercast(Graphics g, float cx, float cy)
        {
            CloudBody(g, cx, cy, Color.FromArgb(178, 190, 177));
        }

        private static void DrawPartlyCloudy(Graphics g, float cx, float cy)
        {
            // 右上小金阳 + 前侧浮云
            using (SolidBrush b = new SolidBrush(Color.FromArgb(227, 185, 95)))
                g.FillEllipse(b, cx + 3, cy - 9, 9, 9);
            using (Pen p = new Pen(Color.FromArgb(190, 150, 70), 1.2F))
            {
                for (int i = 0; i < 4; i++)
                {
                    double a = Math.PI / 4 + i * Math.PI / 2;
                    g.DrawLine(p,
                        cx + 7.5F + (float)Math.Cos(a) * 6.5F, cy - 4.5F + (float)Math.Sin(a) * 6.5F,
                        cx + 7.5F + (float)Math.Cos(a) * 9F, cy - 4.5F + (float)Math.Sin(a) * 9F);
                }
            }
            CloudBody(g, cx - 2, cy + 2, Color.FromArgb(196, 207, 190));
        }

        private static void DrawRainCloud(Graphics g, float cx, float cy)
        {
            CloudBody(g, cx, cy, Color.FromArgb(184, 196, 182));
            using (Pen p = new Pen(Color.FromArgb(116, 143, 165), 1.6F))
            {
                p.StartCap = LineCap.Round; p.EndCap = LineCap.Round;
                g.DrawLine(p, cx - 7, cy + 9, cx - 10, cy + 17);
                g.DrawLine(p, cx + 1, cy + 9, cx - 2, cy + 17);
                g.DrawLine(p, cx + 9, cy + 9, cx + 6, cy + 17);
            }
        }

        private static void DrawSnowCloud(Graphics g, float cx, float cy)
        {
            CloudBody(g, cx, cy, Color.FromArgb(184, 196, 182));
            using (SolidBrush b = new SolidBrush(Color.FromArgb(158, 174, 164)))
            {
                g.FillEllipse(b, cx - 9, cy + 11, 3.4F, 3.4F);
                g.FillEllipse(b, cx - 1, cy + 13, 3.4F, 3.4F);
                g.FillEllipse(b, cx + 7, cy + 11, 3.4F, 3.4F);
            }
        }

        private static void DrawBoltCloud(Graphics g, float cx, float cy)
        {
            CloudBody(g, cx, cy, Color.FromArgb(174, 187, 176));
            using (Pen p = new Pen(Color.FromArgb(214, 168, 74), 1.8F))
            {
                p.StartCap = LineCap.Round; p.EndCap = LineCap.Round;
                g.DrawLine(p, cx + 2, cy + 7, cx - 3, cy + 13);
                g.DrawLine(p, cx - 3, cy + 13, cx + 3, cy + 13);
                g.DrawLine(p, cx + 3, cy + 13, cx - 1, cy + 19);
            }
        }

        private static void DrawFog(Graphics g, float cx, float cy)
        {
            using (Pen p = new Pen(Color.FromArgb(150, 165, 152), 2F))
            {
                p.StartCap = LineCap.Round; p.EndCap = LineCap.Round;
                g.DrawLine(p, cx - 11, cy - 6, cx + 11, cy - 6);
                g.DrawLine(p, cx - 8, cy, cx + 12, cy);
                g.DrawLine(p, cx - 11, cy + 6, cx + 8, cy + 6);
            }
        }
    }
}
