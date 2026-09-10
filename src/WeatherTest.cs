using System;
using System.Collections.Generic;

namespace DeepBlue
{
    internal static class WeatherTest
    {
        internal static void Run()
        {
            Console.WriteLine("[1] 城市搜索: 北京");
            List<CityHit> hits = WeatherEngine.SearchCity("北京");
            foreach (CityHit h in hits) Console.WriteLine("    " + h.Display + "  " + h.Lat + "," + h.Lon);
            Console.WriteLine("    共 " + hits.Count + " 条");

            Console.WriteLine("[2] 天气拉取: 39.9075, 116.3972");
            WeatherData d = WeatherEngine.Fetch("北京市", 39.9075, 116.3972);
            if (d == null)
            {
                Console.WriteLine("    失败: " + (WeatherEngine.LastError ?? "未知错误"));
                Console.WriteLine("    原始响应: " + (WeatherEngine.LastResponse ?? "(无)"));
                Environment.ExitCode = 1;
                return;
            }
            Console.WriteLine("    City=" + d.City + " Code=" + d.Code +
                " Tmax=" + d.Tmax + " Tmin=" + d.Tmin + " Precip=" + d.PrecipProb);
            Console.WriteLine("    播报稿: " + WeatherEngine.Describe(d));
            Console.WriteLine("    卡片行: " + WeatherEngine.CardLine(d));
            Console.WriteLine("[3] 完成");
        }
    }
}
