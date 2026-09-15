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
                _deadlines = ScriptEngine.DeadlinesIn(
                    store.Items, store.Settings.WindowDays, DateTime.Today);
            }
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            DateTime today = DateTime.Today;
            int w = Width, h = Height;
            int pad = 24;

            // 页眉：今日 · 日期
            using (Font head = Ui.Kai(17F, true))
                g.DrawString("今 日", head, new SolidBrush(Ui.InkGreen), pad, 18);
            string dateStr = today.Month + "月" + today.Day + "日 · " + ScriptEngine.WeekShortName(today);
            using (Font df = Ui.F(9F))
            {
                SizeF ds = g.MeasureString(dateStr, df);
                g.DrawString(dateStr, df, new SolidBrush(Ui.MutedGreen), w - pad - ds.Width, 26);
            }
            using (Pen gold = new Pen(Color.FromArgb(201, 154, 63), 1.4F))
                g.DrawLine(gold, pad, 54, w - pad, 54);

            int y = 66;

            // 天气行
            if (_weather != null && WeatherEngine.IsFresh(_weather))
            {
                DrawSun(g, pad + 10, y + 11);
                string line = WeatherEngine.CardLine(_weather);
                using (Font wf = Ui.F(10F))
                    g.DrawString(line, wf, new SolidBrush(Ui.InkGreenBody), pad + 30, y);
                y += 40;
            }
            else if (_storeWeatherOn)
            {
                using (Font wf = Ui.F(9F))
                    g.DrawString("天气暂不可用", wf, new SolidBrush(Ui.MutedGreen), pad + 30, y);
                y += 40;
            }

            // 今日安排
            y = DrawSection(g, "今 日 安 排", y, w, pad);
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
                    y += 36;
                }
                if (_todays.Count > limit)
                {
                    using (Font f = Ui.F(9F))
                        g.DrawString("… 其余 " + (_todays.Count - limit) + " 项见日程管理",
                            f, new SolidBrush(Ui.MutedGreen), pad + 58, y - 4);
                    y += 24;
                }
            }

            // 即将截止
            if (_deadlines.Count > 0)
            {
                y += 10;
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
                    g.DrawString(_footNote, f, new SolidBrush(Ui.MutedGreen), pad, h - 30);
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
            // 标题 + 优先级标签
            using (Font tf = Ui.F(10.5F))
            {
                float tx = pad + 58;
                string title = ev.Title;
                SizeF ts = g.MeasureString(title, tf);
                bool drawTag = ev.Priority == "P0" || ev.Priority == "P1";
                float maxW = w - pad - tx - (drawTag ? 46 : 0);
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
                    using (Brush bb = new SolidBrush(Color.FromArgb(30, c)))
                        g.FillRectangle(bb, w - pad - 40, y + 1, 40, 17);
                    using (Font gf = Ui.F(8F))
                        g.DrawString(tag, gf, new SolidBrush(c), w - pad - 40, y + 2);
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
    }
}
