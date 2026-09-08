using System;
using System.Collections.Generic;
using System.IO;

namespace DeepBlue
{
    public static class SelfTest
    {
        public static void Run()
        {
            List<string> log = new List<string>();
            int failures = 0;

            Store.OverrideDir = System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "testdata");
            try { System.IO.Directory.CreateDirectory(Store.OverrideDir); }
            catch (Exception) { }

            Action<string, bool> check = delegate (string name, bool ok)
            {
                log.Add((ok ? "[PASS] " : "[FAIL] ") + name);
                if (!ok) failures++;
            };

            try
            {
                check("问候语-早(8点)=早上好", ScriptEngine.Greeting(8) == "早上好");
                check("问候语-午(12点)=中午好", ScriptEngine.Greeting(12) == "中午好");
                check("问候语-下午(15点)=下午好", ScriptEngine.Greeting(15) == "下午好");
                check("问候语-晚(21点)=晚上好", ScriptEngine.Greeting(21) == "晚上好");

                check("时间口语-10:00=上午10点", ScriptEngine.TimeToSpeech("10:00") == "上午10点");
                check("时间口语-09:30=上午9点半", ScriptEngine.TimeToSpeech("09:30") == "上午9点半");
                check("时间口语-14:30=下午2点半", ScriptEngine.TimeToSpeech("14:30") == "下午2点半");
                check("时间口语-20:00=晚上8点", ScriptEngine.TimeToSpeech("20:00") == "晚上8点");
                check("时间口语-11:15=上午11点15分", ScriptEngine.TimeToSpeech("11:15") == "上午11点15分");

                check("时间校验-09:30合法", ScriptEngine.IsValidTime("09:30"));
                check("时间校验-25:00非法", !ScriptEngine.IsValidTime("25:00"));
                check("时间校验-9:3非法", !ScriptEngine.IsValidTime("9:3"));

                DateTime today = DateTime.Today;

                ScheduleItem weekly = new ScheduleItem();
                weekly.RecurType = "weekly";
                weekly.Weekday = (int)today.DayOfWeek;
                check("每周重复-当天命中", ScriptEngine.RecursOn(weekly, today));
                check("每周重复-次日不命中", !ScriptEngine.RecursOn(weekly, today.AddDays(1)));

                ScheduleItem monthly = new ScheduleItem();
                monthly.RecurType = "monthly";
                monthly.Monthday = today.Day;
                check("每月重复-当日命中", ScriptEngine.RecursOn(monthly, today));

                ScheduleItem daily = new ScheduleItem();
                daily.RecurType = "daily";
                DateTime? next = ScriptEngine.NextOccurrence(daily, today);
                check("每天重复-下次发生=今天", next != null && next.Value.Date == today);

                List<ScheduleItem> items = SampleData(today);

                List<ScheduleItem> todays = ScriptEngine.EventsOn(items, today);
                bool hasP1 = false, hasDaily = false;
                foreach (ScheduleItem t in todays)
                {
                    if (t.Title == "产品评审会") hasP1 = true;
                    if (t.Title == "英语打卡") hasDaily = true;
                }
                check("今日安排-包含单日事项", hasP1);
                check("今日安排-包含每天重复事项", hasDaily);
                check("今日安排-排除已完成事项", todays.TrueForAll(delegate (ScheduleItem t) { return !t.Done; }));
                if (todays.Count >= 2)
                {
                    check("今日安排-按时间升序",
                        string.CompareOrdinal(todays[0].Time, todays[todays.Count - 1].Time) <= 0);
                }

                List<ScheduleItem> dls = ScriptEngine.DeadlinesIn(items, 7, today);
                bool hasP0 = false, hasP2 = false;
                foreach (ScheduleItem d in dls)
                {
                    if (d.Title == "季度报告") hasP0 = true;
                    if (d.Title == "物业费缴纳") hasP2 = true;
                }
                check("截止提醒-包含窗口内P0", hasP0);
                check("截止提醒-排除P2(窗口内)", !hasP2);

                AppSettings s = new AppSettings();
                List<string> script = ScriptEngine.Compose(items, s);
                check("播报稿-非空", script != null && script.Count > 0);
                check("播报稿-以问候开头", script[0].StartsWith(ScriptEngine.Greeting(DateTime.Now.Hour)));
                check("播报稿-含今日安排计数", Join(script).Contains("你今天共有"));
                check("播报稿-含结束语", script[script.Count - 1].Contains("深蓝祝你度过顺利的一天"));
                check("播报稿-含P0前缀(重要)", Join(script).Contains("重要，季度报告"));

                s.SecToday = false;
                s.SecDue = false;
                List<string> script2 = ScriptEngine.Compose(items, s);
                check("段落开关-关闭后只剩日期+结束语", script2.Count == 2);

                s.SecDate = false;
                s.SecToday = false;
                s.SecDue = false;
                List<string> script3 = ScriptEngine.Compose(items, s);
                check("全关-仅结束语", script3.Count == 1);

                Store store = new Store();
                store.Items = items;
                store.NextId = 100;
                store.Settings.WindowDays = 14;
                store.Save();
                if (Store.LastSaveError != null) log.Add("[INFO] Save error: " + Store.LastSaveError);

                Store loaded = Store.Load();
                check("存储-条目数往返一致", loaded.Items.Count == items.Count);
                check("存储-提醒窗口往返一致", loaded.Settings.WindowDays == 14);
                check("存储-NextId往返一致", loaded.NextId == 100);
                check("存储-临时字段未序列化",
                    !File.ReadAllText(Path.Combine(Store.DataDir, "data.json")).Contains("ConfirmUntil"));
            }
            catch (Exception ex)
            {
                failures++;
                log.Add("[FAIL] 异常: " + ex.Message);
                log.Add(ex.StackTrace);
            }

            string outPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "selftest.txt");
            try { File.WriteAllLines(outPath, log.ToArray()); }
            catch (Exception) { }
            foreach (string line in log) Console.WriteLine(line);

            Environment.ExitCode = failures == 0 ? 0 : 1;
        }

        private static string Join(List<string> list)
        {
            return string.Join("\n", list.ToArray());
        }

        private static List<ScheduleItem> SampleData(DateTime today)
        {
            List<ScheduleItem> items = new List<ScheduleItem>();
            items.Add(new ScheduleItem());
            items[items.Count - 1].Id = 1;
            items[items.Count - 1].Title = "产品评审会";
            items[items.Count - 1].Date = ScriptEngine.ToDateStr(today);
            items[items.Count - 1].Time = "10:00";
            items[items.Count - 1].Priority = "P1";
            items[items.Count - 1].Note = "准备演示环境";

            items.Add(new ScheduleItem());
            items[items.Count - 1].Id = 2;
            items[items.Count - 1].Title = "与客户电话会议";
            items[items.Count - 1].Date = ScriptEngine.ToDateStr(today);
            items[items.Count - 1].Time = "14:30";
            items[items.Count - 1].Priority = "P1";

            items.Add(new ScheduleItem());
            items[items.Count - 1].Id = 3;
            items[items.Count - 1].Title = "健身";
            items[items.Count - 1].Date = ScriptEngine.ToDateStr(today);
            items[items.Count - 1].Time = "20:00";
            items[items.Count - 1].Priority = "P3";

            items.Add(new ScheduleItem());
            items[items.Count - 1].Id = 4;
            items[items.Count - 1].Title = "团队周会";
            items[items.Count - 1].RecurType = "weekly";
            items[items.Count - 1].Weekday = (int)today.DayOfWeek;
            items[items.Count - 1].Time = "09:30";
            items[items.Count - 1].Priority = "P2";

            items.Add(new ScheduleItem());
            items[items.Count - 1].Id = 5;
            items[items.Count - 1].Title = "英语打卡";
            items[items.Count - 1].RecurType = "daily";
            items[items.Count - 1].Time = "21:30";
            items[items.Count - 1].Priority = "P3";

            items.Add(new ScheduleItem());
            items[items.Count - 1].Id = 6;
            items[items.Count - 1].Title = "季度报告";
            items[items.Count - 1].Due = ScriptEngine.ToDateStr(today.AddDays(2));
            items[items.Count - 1].Priority = "P0";
            items[items.Count - 1].Note = "提交给部门总监";

            items.Add(new ScheduleItem());
            items[items.Count - 1].Id = 7;
            items[items.Count - 1].Title = "报销单提交";
            items[items.Count - 1].Due = ScriptEngine.ToDateStr(today.AddDays(3));
            items[items.Count - 1].Priority = "P1";

            items.Add(new ScheduleItem());
            items[items.Count - 1].Id = 8;
            items[items.Count - 1].Title = "物业费缴纳";
            items[items.Count - 1].Due = ScriptEngine.ToDateStr(today.AddDays(5));
            items[items.Count - 1].Priority = "P2";

            items.Add(new ScheduleItem());
            items[items.Count - 1].Id = 9;
            items[items.Count - 1].Title = "车险续保办理";
            items[items.Count - 1].Due = ScriptEngine.ToDateStr(today.AddDays(6));
            items[items.Count - 1].Priority = "P0";

            items.Add(new ScheduleItem());
            items[items.Count - 1].Id = 10;
            items[items.Count - 1].Title = "已完成的旧事项";
            items[items.Count - 1].Date = ScriptEngine.ToDateStr(today);
            items[items.Count - 1].Priority = "P1";
            items[items.Count - 1].Done = true;

            return items;
        }
    }
}
