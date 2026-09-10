using System;
using System.Collections.Generic;

namespace DeepBlue
{
    internal static class WeatherTest
    {
        internal static void Run()
        {
            Store store = Store.Load();
            AppSettings s = store.Settings;
            Console.WriteLine("[配置] 数据源=" + s.WeatherSource +
                " 城市=" + (s.WeatherCity.Length > 0 ? s.WeatherCity : "(未设置)") +
                (s.WeatherSource == "qweather"
                    ? " Host=" + (s.QwHost.Length > 0 ? "已填" : "未填") +
                      " Key=" + (s.QwKey.Length > 0 ? "已填" : "未填") +
                      " LocationID=" + (s.QwLocation.Length > 0 ? s.QwLocation : "未填")
                    : ""));

            Console.WriteLine("[1] 城市搜索: 北京");
            List<CityHit> hits = WeatherEngine.SearchCity("北京", s);
            foreach (CityHit h in hits) Console.WriteLine("    " + h.Display +
                (h.LocationId.Length > 0 ? "  [ID " + h.LocationId + "]" : "") +
                "  " + h.Lat + "," + h.Lon);
            Console.WriteLine("    共 " + hits.Count + " 条");

            Console.WriteLine("[2] 天气拉取");
            WeatherData d = WeatherEngine.Fetch(s);
            if (d == null)
            {
                Console.WriteLine("    失败: " + (WeatherEngine.LastError ?? "未知错误"));
                Console.WriteLine("    原始响应: " + (WeatherEngine.LastResponse ?? "(无)"));
                Environment.ExitCode = 1;
                return;
            }
            Console.WriteLine("    City=" + d.City + " Source=" + d.Source +
                " Text=" + d.Text + " Code=" + d.Code +
                " Tmax=" + d.Tmax + " Tmin=" + d.Tmin + " Precip=" + d.PrecipProb);
            Console.WriteLine("    播报稿: " + WeatherEngine.Describe(d));
            Console.WriteLine("    卡片行: " + WeatherEngine.CardLine(d));
            Console.WriteLine("[3] 完成");
        }
    }
}
