using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace DeepBlue
{
    public static class ScriptEngine
    {
        private static readonly string[] WeekNames =
            { "星期日", "星期一", "星期二", "星期三", "星期四", "星期五", "星期六" };
        private static readonly string[] WeekShort =
            { "周日", "周一", "周二", "周三", "周四", "周五", "周六" };

        public static string WeekName(DateTime d)
        {
            return WeekNames[(int)d.DayOfWeek];
        }

        public static string WeekShortName(DateTime d)
        {
            return WeekShort[(int)d.DayOfWeek];
        }

        public static string CnDate(DateTime d)
        {
            return d.Year + "年" + d.Month + "月" + d.Day + "日";
        }

        public static DateTime? ParseDate(string s)
        {
            if (string.IsNullOrEmpty(s)) return null;
            DateTime d;
            // 支持带时刻的截止（yyyy-MM-dd HH:mm）与纯日期两种格式
            if (DateTime.TryParseExact(s, "yyyy-MM-dd HH:mm", null,
                System.Globalization.DateTimeStyles.None, out d))
                return d;
            if (DateTime.TryParseExact(s, "yyyy-MM-dd", null,
                System.Globalization.DateTimeStyles.None, out d))
                return d;
            return null;
        }

        public static string ToDateStr(DateTime d)
        {
            return d.ToString("yyyy-MM-dd");
        }

        public static bool IsValidTime(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            return Regex.IsMatch(s, "^([01]?[0-9]|2[0-3]):[0-5][0-9]$");
        }

        public static string Greeting(int hour)
        {
            if (hour >= 5 && hour < 11) return "早上好";
            if (hour >= 11 && hour < 13) return "中午好";
            if (hour >= 13 && hour < 18) return "下午好";
            return "晚上好";
        }

        public static string TimeToSpeech(string time)
        {
            if (string.IsNullOrEmpty(time)) return "";
            string[] p = time.Split(':');
            int h = int.Parse(p[0]);
            int m = int.Parse(p[1]);
            string period;
            if (h < 6) period = "凌晨";
            else if (h < 12) period = "上午";
            else if (h == 12) period = "中午";
            else if (h < 18) period = "下午";
            else period = "晚上";
            int h12 = h > 12 ? h - 12 : h;
            string minute;
            if (m == 30) minute = "半";
            else if (m == 0) minute = "";
            else minute = m + "分";
            return period + h12 + "点" + minute;
        }

        // 截止时刻口语：今天/明天/后天/M月D日，带时刻则追加（18:00 → 晚上6点）
        public static string DueSpeech(DateTime due, DateTime today)
        {
            int left = (int)(due.Date - today.Date).TotalDays;
            string day;
            if (left == 0) day = "今天";
            else if (left == 1) day = "明天";
            else if (left == 2) day = "后天";
            else day = due.Month + "月" + due.Day + "日";
            if (due.TimeOfDay > TimeSpan.Zero)
                day += TimeToSpeech(due.ToString("HH:mm"));
            return day;
        }

        public static bool RecursOn(ScheduleItem it, DateTime day)
        {
            if (!it.IsRecurring) return false;
            if (it.RecurType == "daily") return true;
            if (it.RecurType == "weekly") return (int)day.DayOfWeek == it.Weekday;
            if (it.RecurType == "monthly") return day.Day == it.Monthday;
            return false;
        }

        public static DateTime? OccursOn(ScheduleItem it, DateTime day)
        {
            if (it.IsRecurring)
                return RecursOn(it, day) ? (DateTime?)day : null;
            DateTime? d = ParseDate(it.Date);
            if (d != null && d.Value.Date == day.Date) return day;
            return null;
        }

        public static DateTime? NextOccurrence(ScheduleItem it, DateTime from)
        {
            if (!it.IsRecurring) return ParseDate(it.Date);
            for (int i = 0; i < 400; i++)
            {
                DateTime d = from.Date.AddDays(i);
                if (RecursOn(it, d)) return d;
            }
            return null;
        }

        public static bool IsToday(ScheduleItem it, DateTime today)
        {
            if (it.Done) return false;
            if (it.IsRecurring) return RecursOn(it, today);
            DateTime? d = ParseDate(it.Date);
            return d != null && d.Value.Date == today.Date;
        }

        // 单日、非重复、发生日已过且未勾完成 → 已过期（日程管理划线置底，可一键清理）
        public static bool IsExpired(ScheduleItem it, DateTime today)
        {
            if (it.Done || it.IsRecurring) return false;
            DateTime? d = ParseDate(it.Date);
            return d != null && d.Value.Date < today.Date;
        }

        public static List<ScheduleItem> EventsOn(List<ScheduleItem> items, DateTime day)
        {
            List<ScheduleItem> result = new List<ScheduleItem>();
            foreach (ScheduleItem it in items)
            {
                if (it.Done) continue;
                if (it.IsRecurring)
                {
                    if (RecursOn(it, day)) result.Add(it);
                }
                else
                {
                    DateTime? d = ParseDate(it.Date);
                    if (d != null && d.Value.Date == day.Date) result.Add(it);
                }
            }
            result.Sort(delegate (ScheduleItem a, ScheduleItem b)
            {
                return string.CompareOrdinal(a.Time ?? "99:99", b.Time ?? "99:99");
            });
            return result;
        }

        public static List<ScheduleItem> DeadlinesIn(List<ScheduleItem> items, int windowDays, DateTime today)
        {
            return DeadlinesIn(items, windowDays, today, null);
        }

        // 提醒窗口：P0 提前 windowDays 天，P1 只提前 3 天；excludeIds 用于
        // 排除今日安排里已连同截止一起播报过的事项（避免重复提醒）
        public static List<ScheduleItem> DeadlinesIn(List<ScheduleItem> items, int windowDays, DateTime today, HashSet<int> excludeIds)
        {
            List<KeyValuePair<DateTime, ScheduleItem>> hits =
                new List<KeyValuePair<DateTime, ScheduleItem>>();
            foreach (ScheduleItem it in items)
            {
                if (it.Done) continue;
                if (excludeIds != null && excludeIds.Contains(it.Id)) continue;
                if (it.Priority != "P0" && it.Priority != "P1") continue;
                DateTime? due = ParseDate(it.Due);
                if (due == null) continue;
                int win = it.Priority == "P0" ? windowDays : Math.Min(windowDays, 3);
                int left = (int)(due.Value.Date - today.Date).TotalDays;
                if (left < 0 || left > win) continue;
                hits.Add(new KeyValuePair<DateTime, ScheduleItem>(due.Value.Date, it));
            }
            hits.Sort(delegate (KeyValuePair<DateTime, ScheduleItem> a, KeyValuePair<DateTime, ScheduleItem> b)
            {
                int c = a.Key.CompareTo(b.Key);
                if (c != 0) return c;
                return string.CompareOrdinal(a.Value.Title, b.Value.Title);
            });
            List<ScheduleItem> result = new List<ScheduleItem>();
            foreach (KeyValuePair<DateTime, ScheduleItem> kv in hits) result.Add(kv.Value);
            return result;
        }

        public static List<string> Compose(List<ScheduleItem> items, AppSettings s)
        {
            return Compose(items, s, null);
        }

        public static List<string> Compose(List<ScheduleItem> items, AppSettings s, WeatherData weather)
        {
            DateTime now = DateTime.Now;
            DateTime today = now.Date;
            List<string> parts = new List<string>();

            if (s.SecDate)
            {
                parts.Add("今天是" + CnDate(today) + "，" + WeekName(today) + "。");
            }

            if (s.WeatherOn && weather != null && WeatherEngine.IsFresh(weather))
            {
                string w = WeatherEngine.Describe(weather);
                if (w.Length > 0) parts.Add(w);
            }

            HashSet<int> dueMentioned = new HashSet<int>();

            if (s.SecToday)
            {
                List<ScheduleItem> todays = EventsOn(items, today);
                if (todays.Count == 0)
                {
                    parts.Add("你今天没有录入任何安排。");
                }
                else
                {
                    parts.Add("你今天共有" + todays.Count + "项安排。");
                    foreach (ScheduleItem ev in todays)
                    {
                        string line = ev.Priority == "P0" ? "重要，" : "";
                        if (!string.IsNullOrEmpty(ev.Time))
                            line += TimeToSpeech(ev.Time) + "时需要开展" + ev.Title;
                        else
                            line += "另外，" + ev.Title;
                        if (!string.IsNullOrEmpty(ev.Note)) line += "，" + ev.Note;
                        parts.Add(line + "。");
                        // 今天要进行且有截止日的高优先级任务：紧跟紧急句，
                        // 并在下方截止提醒段落中排除（避免重复播报）
                        DateTime? due = ParseDate(ev.Due);
                        if (due != null && (ev.Priority == "P0" || ev.Priority == "P1"))
                        {
                            parts.Add("该任务紧急程度高，请于" + DueSpeech(due.Value, today) + "前完成。");
                            dueMentioned.Add(ev.Id);
                        }
                    }
                }
            }

            if (s.SecDue)
            {
                List<ScheduleItem> dls = DeadlinesIn(items, s.WindowDays, today, dueMentioned);
                if (dls.Count > 0)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append("另外提醒你，未来").Append(s.WindowDays)
                      .Append("天内有").Append(dls.Count).Append("件事即将截止：");
                    foreach (ScheduleItem ev in dls)
                    {
                        DateTime due = ParseDate(ev.Due).Value;
                        int left = (int)(due.Date - today.Date).TotalDays;
                        sb.Append(ev.Priority == "P0" ? "重要，" : "");
                        sb.Append(ev.Title).Append("，");
                        sb.Append(due.Month).Append("月").Append(due.Day).Append("日截止，");
                        if (left == 0) sb.Append("就是今天");
                        else sb.Append("还剩").Append(left).Append("天");
                        sb.Append("；");
                    }
                    sb.Length = sb.Length - 1;
                    sb.Append("。请及时处理。");
                    parts.Add(sb.ToString());
                }
            }

            parts.Add("以上就是今天的全部内容，深蓝祝你度过顺利的一天。");
            return parts;
        }
    }
}
