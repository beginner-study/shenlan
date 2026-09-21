using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;

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

                List<string> sysOnly = new List<string>();
                sysOnly.Add("Microsoft Huihui Desktop");
                sysOnly.Add("Microsoft Zira Desktop");
                check("默认语音-无自然语音返回空",
                    VoicePicker.PickDefault(sysOnly) == "");

                List<string> withNatural = new List<string>();
                withNatural.Add("Microsoft Huihui Desktop");
                withNatural.Add("Microsoft Zira Desktop");
                withNatural.Add("Microsoft Xiaoxiao");
                check("默认语音-存在晓晓优先晓晓",
                    VoicePicker.PickDefault(withNatural) == "Microsoft Xiaoxiao");

                List<string> onlineOnly = new List<string>();
                onlineOnly.Add("Microsoft Huihui Desktop");
                onlineOnly.Add("Microsoft Xiaoxiao (Natural) - Chinese (Mainland) (Online)");
                check("默认语音-忽略在线语音",
                    VoicePicker.PickDefault(onlineOnly) == "");

                check("天气代码-0=晴", WeatherEngine.CodeToCn(0) == "晴");
                check("天气代码-3=阴", WeatherEngine.CodeToCn(3) == "阴");
                check("天气代码-61=小雨", WeatherEngine.CodeToCn(61) == "小雨");
                check("天气代码-95=雷阵雨", WeatherEngine.CodeToCn(95) == "雷阵雨");
                check("天气代码-未知=天气", WeatherEngine.CodeToCn(123) == "天气");

                check("降水口语-30=百分之三十", WeatherEngine.CnPercent(30) == "百分之三十");
                check("降水口语-55=百分之五十五", WeatherEngine.CnPercent(55) == "百分之五十五");
                check("降水口语-100=百分之百", WeatherEngine.CnPercent(100) == "百分之百");

                WeatherData wd = new WeatherData();
                wd.City = "北京市";
                wd.Code = 0;
                wd.Tmax = 28.6;
                wd.Tmin = 15.4;
                wd.PrecipProb = 0;
                wd.FetchDate = ScriptEngine.ToDateStr(DateTime.Today);
                check("天气描述-晴无降水",
                    WeatherEngine.Describe(wd) == "北京市今天晴，最高29度，最低15度。");

                wd.Code = 61;
                wd.PrecipProb = 60;
                check("天气描述-含降水概率",
                    WeatherEngine.Describe(wd) == "北京市今天小雨，最高29度，最低15度，降水概率百分之六十。");

                wd.PrecipProb = 10;
                check("天气描述-低降水不提及",
                    !WeatherEngine.Describe(wd).Contains("降水概率"));

                WeatherData stale = new WeatherData();
                stale.City = "北京市";
                stale.FetchDate = ScriptEngine.ToDateStr(DateTime.Today.AddDays(-1));
                check("天气缓存-昨日数据过期", !WeatherEngine.IsFresh(stale));
                check("天气缓存-今日数据新鲜", WeatherEngine.IsFresh(wd));
                check("天气缓存-空数据过期", !WeatherEngine.IsFresh(null));

                WeatherEngine.SaveCache(wd);
                WeatherData wc = WeatherEngine.LoadCache();
                check("天气缓存-文件往返一致",
                    wc != null && wc.City == "北京市" && wc.Code == 61 && wc.PrecipProb == 10 &&
                    WeatherEngine.IsFresh(wc));

                WeatherData qw = new WeatherData();
                qw.City = "北京市";
                qw.Source = "qweather";
                qw.Location = "101010100";
                qw.Text = "多云";
                qw.Tmax = 27;
                qw.Tmin = 14;
                qw.FetchDate = ScriptEngine.ToDateStr(DateTime.Today);
                check("天气描述-和风Text优先",
                    WeatherEngine.Describe(qw) == "北京市今天多云，最高27度，最低14度。");

                AppSettings qs = new AppSettings();
                qs.WeatherSource = "qweather";
                qs.WeatherCity = "北京市";
                qs.QwLocation = "101010100";
                check("缓存匹配-和风设置匹配和风数据", WeatherEngine.Matches(qw, qs));
                check("缓存匹配-和风设置不匹配OpenMeteo数据", !WeatherEngine.Matches(wd, qs));
                qs.WeatherSource = "open-meteo";
                check("缓存匹配-OpenMeteo设置不匹配和风数据", !WeatherEngine.Matches(qw, qs));

                AppSettings qon = new AppSettings();
                qon.WeatherOn = true;

                check("和风坐标-经度在前两位小数",
                    WeatherEngine.BuildQwCoords(39.9075, 116.3972) == "116.4,39.91");
                check("和风坐标-零值",
                    WeatherEngine.BuildQwCoords(0, 0) == "0,0");

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
                bool hasP0 = false, hasP2 = false, hasP1in3 = false, hasP1in4 = false;
                foreach (ScheduleItem d in dls)
                {
                    if (d.Title == "季度报告") hasP0 = true;
                    if (d.Title == "物业费缴纳") hasP2 = true;
                    if (d.Title == "报销单提交") hasP1in3 = true;
                    if (d.Title == "P1四天后截止") hasP1in4 = true;
                }
                check("截止提醒-包含窗口内P0", hasP0);
                check("截止提醒-排除P2(窗口内)", !hasP2);
                check("截止提醒-P1提前3天内包含", hasP1in3);
                check("截止提醒-P1超过3天排除", !hasP1in4);
                check("截止提醒-P0仍提前7天", dls.Exists(delegate (ScheduleItem d)
                {
                    return d.Title == "车险续保办理";
                }));

                AppSettings ws = new AppSettings();
                ws.WeatherOn = true;
                List<string> wscript = ScriptEngine.Compose(items, ws, wd);
                check("播报稿-含天气段", Join(wscript).Contains("北京市今天小雨"));
                check("播报稿-天气段无额外降水字样", !Join(wscript).Contains("降水概率"));
                check("播报稿-天气段位于日期之后", wscript[1].StartsWith("北京市今天小雨"));
                check("播报稿-天气开关关闭不播报",
                    !Join(ScriptEngine.Compose(items, new AppSettings(), wd)).Contains("北京市今天"));
                check("播报稿-天气数据过期不播报",
                    !Join(ScriptEngine.Compose(items, ws, stale)).Contains("北京市今天"));
                check("播报稿-和风天气段",
                    Join(ScriptEngine.Compose(items, qon, qw)).Contains("北京市今天多云"));

                AppSettings s = new AppSettings();
                List<string> script = ScriptEngine.Compose(items, s);
                check("播报稿-非空", script != null && script.Count > 0);
                check("播报稿-以日期开头", script[0].StartsWith("今天是"));
                check("播报稿-含今日安排计数", Join(script).Contains("你今天共有"));
                check("播报稿-时间点句式",
                    Join(script).Contains("晚上6点时需要开展PMD方案拓展"));
                check("播报稿-紧急句",
                    Join(script).Contains("中午12点时需要开展测试用案例") &&
                    Join(script).Contains("该任务紧急程度高，请于明天晚上6点前完成"));
                check("播报稿-紧急任务不重复进截止提醒",
                    !Join(script).Contains("测试用案例，"));
                check("截止提醒-P1窗口内正常提醒",
                    Join(script).Contains("报销单提交"));
                check("播报稿-含结束语", script[script.Count - 1].Contains("深蓝祝你度过顺利的一天"));
                check("播报稿-含P0前缀(重要)", Join(script).Contains("重要，季度报告"));

                DateTime? dt = ScriptEngine.ParseDate("2026-01-02 18:00");
                check("日期解析-支持带时刻",
                    dt != null && dt.Value.Hour == 18 && dt.Value.Minute == 0);
                check("日期解析-纯日期仍支持",
                    ScriptEngine.ParseDate("2026-01-02") != null &&
                    ScriptEngine.ParseDate("2026-01-02").Value.Hour == 0);
                check("截止口语-明天带时刻",
                    ScriptEngine.DueSpeech(today.AddDays(1).AddHours(18), today) == "明天晚上6点");
                check("截止口语-今天零点",
                    ScriptEngine.DueSpeech(today, today) == "今天");
                check("过期判定-昨日未完成", ScriptEngine.IsExpired(items.Find(
                    delegate (ScheduleItem x) { return x.Id == 14; }), today));
                check("过期判定-已完成不算", !ScriptEngine.IsExpired(items.Find(
                    delegate (ScheduleItem x) { return x.Id == 10; }), today));
                check("过期判定-重复事项不算", !ScriptEngine.IsExpired(items.Find(
                    delegate (ScheduleItem x) { return x.Id == 5; }), today));
                check("过期-今日安排不含过期事项", !ScriptEngine.EventsOn(items, today).Exists(
                    delegate (ScheduleItem x) { return x.Id == 14; }));

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
                store.Settings.WeatherOn = true;
                store.Settings.WeatherCity = "北京市";
                store.Settings.WeatherLat = 39.9075;
                store.Settings.WeatherLon = 116.3972;
                store.Settings.WeatherSource = "qweather";
                store.Settings.QwHost = "abc123.re.qweatherapi.com";
                store.Settings.QwKey = "testkey123";
                store.Settings.QwLocation = "101010100";
                store.Save();
                if (Store.LastSaveError != null) log.Add("[INFO] Save error: " + Store.LastSaveError);

                Store loaded = Store.Load();
                check("存储-条目数往返一致", loaded.Items.Count == items.Count);
                check("存储-提醒窗口往返一致", loaded.Settings.WindowDays == 14);
                check("存储-NextId往返一致", loaded.NextId == 100);
                check("存储-天气设置往返一致",
                    loaded.Settings.WeatherOn && loaded.Settings.WeatherCity == "北京市" &&
                    Math.Abs(loaded.Settings.WeatherLat - 39.9075) < 0.0001 &&
                    Math.Abs(loaded.Settings.WeatherLon - 116.3972) < 0.0001);
                check("存储-和风配置往返一致",
                    loaded.Settings.WeatherSource == "qweather" &&
                    loaded.Settings.QwHost == "abc123.re.qweatherapi.com" &&
                    loaded.Settings.QwKey == "testkey123" &&
                    loaded.Settings.QwLocation == "101010100");
                check("存储-临时字段未序列化",
                    !File.ReadAllText(Path.Combine(Store.DataDir, "data.json")).Contains("ConfirmUntil"));

                // 开机自启动：直接操作用户真实 HKCU Run 键，先记录原值，finally 恢复
                const string runKeyPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";
                object runOriginal = null;
                try
                {
                    using (RegistryKey k = Registry.CurrentUser.OpenSubKey(runKeyPath, false))
                    {
                        if (k != null) runOriginal = k.GetValue("DeepBlue");
                    }
                }
                catch (Exception) { }
                try
                {
                    check("自启动-写入后已启用",
                        AutoStart.SetEnabled(true) && AutoStart.IsEnabled());
                    string runExpected = "\"" +
                        System.Windows.Forms.Application.ExecutablePath + "\"";
                    string runActual = null;
                    using (RegistryKey k = Registry.CurrentUser.OpenSubKey(runKeyPath, false))
                    {
                        if (k != null) runActual = k.GetValue("DeepBlue") as string;
                    }
                    check("自启动-键值带引号指向自身", runActual == runExpected);
                    check("自启动-关闭后未启用",
                        AutoStart.SetEnabled(false) && !AutoStart.IsEnabled());
                    using (RegistryKey k = Registry.CurrentUser.OpenSubKey(runKeyPath, false))
                    {
                        check("自启动-键值已移除",
                            k == null || k.GetValue("DeepBlue") == null);
                    }
                    check("自启动-关闭幂等", AutoStart.SetEnabled(false));
                }
                finally
                {
                    try
                    {
                        using (RegistryKey k = Registry.CurrentUser.OpenSubKey(runKeyPath, true))
                        {
                            if (k != null)
                            {
                                if (runOriginal == null) k.DeleteValue("DeepBlue", false);
                                else k.SetValue("DeepBlue", runOriginal);
                            }
                        }
                    }
                    catch (Exception) { }
                }
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

            items.Add(new ScheduleItem());
            items[items.Count - 1].Id = 11;
            items[items.Count - 1].Title = "P1四天后截止";
            items[items.Count - 1].Due = ScriptEngine.ToDateStr(today.AddDays(4));
            items[items.Count - 1].Priority = "P1";

            items.Add(new ScheduleItem());
            items[items.Count - 1].Id = 12;
            items[items.Count - 1].Title = "PMD方案拓展";
            items[items.Count - 1].Date = ScriptEngine.ToDateStr(today);
            items[items.Count - 1].Time = "18:00";
            items[items.Count - 1].Priority = "P1";

            items.Add(new ScheduleItem());
            items[items.Count - 1].Id = 13;
            items[items.Count - 1].Title = "测试用案例";
            items[items.Count - 1].Date = ScriptEngine.ToDateStr(today);
            items[items.Count - 1].Time = "12:00";
            items[items.Count - 1].Due = today.AddDays(1).ToString("yyyy-MM-dd") + " 18:00";
            items[items.Count - 1].Priority = "P1";

            items.Add(new ScheduleItem());
            items[items.Count - 1].Id = 14;
            items[items.Count - 1].Title = "昨天已过期的事项";
            items[items.Count - 1].Date = ScriptEngine.ToDateStr(today.AddDays(-1));
            items[items.Count - 1].Time = "09:00";
            items[items.Count - 1].Priority = "P2";

            return items;
        }
    }
}
