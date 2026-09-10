using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;

namespace DeepBlue
{
    public class WeatherData
    {
        public string City = "";
        public string Source = "open-meteo";
        public string Location = "";
        public double Lat;
        public double Lon;
        public string Text = "";
        public string FetchDate = "";
        public int Code = -1;
        public double Tmax;
        public double Tmin;
        public int PrecipProb;

        public bool IsQWeather
        {
            get { return Source == "qweather"; }
        }
    }

    public class CityHit
    {
        public string Name = "";
        public string Display = "";
        public string LocationId = "";
        public double Lat;
        public double Lon;

        public override string ToString() { return Display; }
    }

    public static class WeatherEngine
    {
        private const string ApiForecast = "https://api.open-meteo.com/v1/forecast";
        private const string ApiGeocode = "https://geocoding-api.open-meteo.com/v1/search";
        private const int FetchTimeoutMs = 6000;
        private const int SearchTimeoutMs = 8000;

        public static string LastError = null;
        public static string LastResponse = null;

        static WeatherEngine()
        {
            try
            {
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            }
            catch (Exception) { }
        }

        private static string CachePath
        {
            get { return Path.Combine(Store.DataDir, "weather.json"); }
        }

        public static WeatherData LoadCache()
        {
            try
            {
                if (!File.Exists(CachePath)) return null;
                JavaScriptSerializer ser = new JavaScriptSerializer();
                WeatherData d = ser.Deserialize<WeatherData>(File.ReadAllText(CachePath));
                if (d != null)
                {
                    if (d.City == null) d.City = "";
                    if (d.Source == null) d.Source = "open-meteo";
                    if (d.Location == null) d.Location = "";
                    if (d.Text == null) d.Text = "";
                }
                return d;
            }
            catch (Exception) { return null; }
        }

        public static void SaveCache(WeatherData d)
        {
            try
            {
                if (d == null) return;
                if (!Directory.Exists(Store.DataDir)) Directory.CreateDirectory(Store.DataDir);
                JavaScriptSerializer ser = new JavaScriptSerializer();
                File.WriteAllText(CachePath, ser.Serialize(d));
            }
            catch (Exception) { }
        }

        public static bool IsFresh(WeatherData d)
        {
            if (d == null) return false;
            return d.FetchDate == DateTime.Today.ToString("yyyy-MM-dd");
        }

        public static bool Matches(WeatherData d, AppSettings s)
        {
            if (d == null) return false;
            if (s.WeatherSource == "qweather")
            {
                return d.IsQWeather && d.City == s.WeatherCity && d.Location == s.QwLocation;
            }
            return !d.IsQWeather && d.City == s.WeatherCity
                && Math.Abs(d.Lat - s.WeatherLat) < 0.001
                && Math.Abs(d.Lon - s.WeatherLon) < 0.001;
        }

        public static WeatherData Fetch(AppSettings s)
        {
            if (s.WeatherSource == "qweather") return FetchQWeather(s);
            return Fetch(s.WeatherCity, s.WeatherLat, s.WeatherLon);
        }

        public static WeatherData Fetch(string city, double lat, double lon)
        {
            LastError = null;
            try
            {
                System.Globalization.CultureInfo inv =
                    System.Globalization.CultureInfo.InvariantCulture;
                string url = ApiForecast +
                    "?latitude=" + lat.ToString("0.####", inv) +
                    "&longitude=" + lon.ToString("0.####", inv) +
                    "&daily=weather_code,temperature_2m_max,temperature_2m_min,precipitation_probability_max" +
                    "&timezone=auto&forecast_days=1";
                string json = HttpGet(url, FetchTimeoutMs);
                LastResponse = json;
                Dictionary<string, object> root = ParseObj(json);
                if (root == null) return null;
                Dictionary<string, object> daily = root["daily"] as Dictionary<string, object>;
                if (daily == null) return null;

                WeatherData d = new WeatherData();
                d.City = city;
                d.Lat = lat;
                d.Lon = lon;
                d.FetchDate = DateTime.Today.ToString("yyyy-MM-dd");
                d.Code = ToInt(First(daily, "weather_code"), -1);
                d.Tmax = ToDouble(First(daily, "temperature_2m_max"), double.NaN);
                d.Tmin = ToDouble(First(daily, "temperature_2m_min"), double.NaN);
                d.PrecipProb = ToInt(First(daily, "precipitation_probability_max"), 0);
                if (double.IsNaN(d.Tmax) || double.IsNaN(d.Tmin)) return null;
                return d;
            }
            catch (Exception ex)
            {
                LastError = ex.GetType().Name + ": " + ex.Message;
                return null;
            }
        }

        private static WeatherData FetchQWeather(AppSettings s)
        {
            LastError = null;
            if (s.QwHost.Length == 0 || s.QwKey.Length == 0 || s.QwLocation.Length == 0)
            {
                LastError = "QWeather config incomplete (host/key/location)";
                return null;
            }
            try
            {
                Dictionary<string, string> headers = new Dictionary<string, string>();
                headers["X-QW-Api-Key"] = s.QwKey;
                string url = "https://" + s.QwHost + "/v7/weather/3d" +
                    "?location=" + Uri.EscapeDataString(s.QwLocation) + "&lang=zh";
                string json = HttpGet(url, FetchTimeoutMs, headers);
                LastResponse = json;
                Dictionary<string, object> root = ParseObj(json);
                if (root == null) return null;
                string code = Str(root, "code");
                if (code != "200")
                {
                    LastError = "QWeather API code " + code + " " + Str(root, "error");
                    return null;
                }
                object dailyObj;
                if (!root.TryGetValue("daily", out dailyObj)) return null;
                object[] dailyArr = dailyObj as object[];
                if (dailyArr == null || dailyArr.Length == 0) return null;
                Dictionary<string, object> day = dailyArr[0] as Dictionary<string, object>;
                if (day == null) return null;

                WeatherData d = new WeatherData();
                d.City = s.WeatherCity;
                d.Source = "qweather";
                d.Location = s.QwLocation;
                d.Text = Str(day, "textDay");
                d.FetchDate = DateTime.Today.ToString("yyyy-MM-dd");
                d.Tmax = ToDouble(Val(day, "tempMax"), double.NaN);
                d.Tmin = ToDouble(Val(day, "tempMin"), double.NaN);
                d.PrecipProb = ToInt(Val(day, "precipProb"), 0);
                if (d.Text.Length == 0 || double.IsNaN(d.Tmax) || double.IsNaN(d.Tmin)) return null;
                return d;
            }
            catch (Exception ex)
            {
                LastError = ex.GetType().Name + ": " + ex.Message;
                return null;
            }
        }

        public static List<CityHit> SearchCity(string keyword, AppSettings s)
        {
            if (s.WeatherSource == "qweather") return SearchQWeather(keyword, s);
            return SearchOpenMeteo(keyword);
        }

        private static List<CityHit> SearchOpenMeteo(string keyword)
        {
            List<CityHit> hits = new List<CityHit>();
            if (string.IsNullOrEmpty(keyword)) return hits;
            string kw = keyword.Trim();
            if (kw.Length == 0) return hits;
            try
            {
                string url = ApiGeocode + "?name=" + Uri.EscapeDataString(kw) +
                    "&count=8&language=zh&format=json";
                string json = HttpGet(url, SearchTimeoutMs);
                Dictionary<string, object> root = ParseObj(json);
                if (root == null) return hits;
                object resultsObj;
                if (!root.TryGetValue("results", out resultsObj)) return hits;
                object[] results = resultsObj as object[];
                if (results == null) return hits;
                foreach (object o in results)
                {
                    Dictionary<string, object> r = o as Dictionary<string, object>;
                    if (r == null) continue;
                    CityHit h = new CityHit();
                    h.Name = Str(r, "name");
                    h.Lat = ToDouble(Val(r, "latitude"), double.NaN);
                    h.Lon = ToDouble(Val(r, "longitude"), double.NaN);
                    if (h.Name.Length == 0 || double.IsNaN(h.Lat) || double.IsNaN(h.Lon)) continue;
                    string admin1 = Str(r, "admin1");
                    string country = Str(r, "country");
                    StringBuilder disp = new StringBuilder(h.Name);
                    if (admin1.Length > 0 && admin1 != h.Name) disp.Append(" · ").Append(admin1);
                    if (country.Length > 0) disp.Append("，").Append(country);
                    h.Display = disp.ToString();
                    hits.Add(h);
                }
            }
            catch (Exception ex)
            {
                LastError = ex.GetType().Name + ": " + ex.Message;
            }
            return hits;
        }

        private static List<CityHit> SearchQWeather(string keyword, AppSettings s)
        {
            List<CityHit> hits = new List<CityHit>();
            if (string.IsNullOrEmpty(keyword)) return hits;
            string kw = keyword.Trim();
            if (kw.Length == 0) return hits;
            if (s.QwHost.Length == 0 || s.QwKey.Length == 0)
            {
                LastError = "QWeather host/key not set";
                return hits;
            }
            try
            {
                Dictionary<string, string> headers = new Dictionary<string, string>();
                headers["X-QW-Api-Key"] = s.QwKey;
                string url = "https://" + s.QwHost + "/geo/v2/city/lookup" +
                    "?location=" + Uri.EscapeDataString(kw) + "&lang=zh";
                string json = HttpGet(url, SearchTimeoutMs, headers);
                Dictionary<string, object> root = ParseObj(json);
                if (root == null) return hits;
                if (Str(root, "code") != "200")
                {
                    LastError = "QWeather API code " + Str(root, "code");
                    return hits;
                }
                object locObj;
                if (!root.TryGetValue("location", out locObj)) return hits;
                object[] locs = locObj as object[];
                if (locs == null) return hits;
                foreach (object o in locs)
                {
                    Dictionary<string, object> r = o as Dictionary<string, object>;
                    if (r == null) continue;
                    CityHit h = new CityHit();
                    h.Name = Str(r, "name");
                    h.LocationId = Str(r, "id");
                    h.Lat = ToDouble(Val(r, "lat"), double.NaN);
                    h.Lon = ToDouble(Val(r, "lon"), double.NaN);
                    if (h.Name.Length == 0 || h.LocationId.Length == 0) continue;
                    string adm1 = Str(r, "adm1");
                    string adm2 = Str(r, "adm2");
                    string country = Str(r, "country");
                    StringBuilder disp = new StringBuilder(h.Name);
                    if (adm1.Length > 0 && adm1 != h.Name) disp.Append(" · ").Append(adm1);
                    if (adm2.Length > 0 && adm2 != h.Name && adm2 != adm1)
                    {
                        disp.Append(" ").Append(adm2);
                    }
                    if (country.Length > 0) disp.Append("，").Append(country);
                    h.Display = disp.ToString();
                    hits.Add(h);
                }
            }
            catch (Exception ex)
            {
                LastError = ex.GetType().Name + ": " + ex.Message;
            }
            return hits;
        }

        public static string CodeToCn(int code)
        {
            switch (code)
            {
                case 0: return "晴";
                case 1: return "晴间少云";
                case 2: return "多云";
                case 3: return "阴";
                case 45: return "有雾";
                case 48: return "冻雾";
                case 51: return "轻毛毛雨";
                case 53: return "毛毛雨";
                case 55: return "浓毛毛雨";
                case 56: return "轻冻毛毛雨";
                case 57: return "浓冻毛毛雨";
                case 61: return "小雨";
                case 63: return "中雨";
                case 65: return "大雨";
                case 66: return "轻冻雨";
                case 67: return "重冻雨";
                case 71: return "小雪";
                case 73: return "中雪";
                case 75: return "大雪";
                case 77: return "雪粒";
                case 80: return "小阵雨";
                case 81: return "中阵雨";
                case 82: return "强阵雨";
                case 85: return "小阵雪";
                case 86: return "大阵雪";
                case 95: return "雷阵雨";
                case 96: return "雷阵雨伴冰雹";
                case 99: return "强雷雨伴冰雹";
                default: return "天气";
            }
        }

        public static string CnPercent(int p)
        {
            if (p <= 0) return "百分之零";
            if (p >= 100) return "百分之百";
            string[] digits = { "零", "一", "二", "三", "四", "五", "六", "七", "八", "九" };
            StringBuilder sb = new StringBuilder("百分之");
            int tens = p / 10;
            int ones = p % 10;
            if (tens == 1) sb.Append("十");
            else if (tens > 1) sb.Append(digits[tens]).Append("十");
            if (ones > 0) sb.Append(digits[ones]);
            return sb.ToString();
        }

        public static string Describe(WeatherData d)
        {
            if (d == null) return "";
            string condition = d.Text.Length > 0 ? d.Text : CodeToCn(d.Code);
            int tmax = (int)Math.Round(d.Tmax, MidpointRounding.AwayFromZero);
            int tmin = (int)Math.Round(d.Tmin, MidpointRounding.AwayFromZero);
            StringBuilder sb = new StringBuilder();
            if (!string.IsNullOrEmpty(d.City)) sb.Append(d.City);
            sb.Append("今天").Append(condition)
              .Append("，最高").Append(tmax).Append("度，最低").Append(tmin).Append("度");
            if (d.PrecipProb >= 30) sb.Append("，降水概率").Append(CnPercent(d.PrecipProb));
            sb.Append("。");
            return sb.ToString();
        }

        public static string CardLine(WeatherData d)
        {
            if (d == null) return "";
            int tmax = (int)Math.Round(d.Tmax, MidpointRounding.AwayFromZero);
            int tmin = (int)Math.Round(d.Tmin, MidpointRounding.AwayFromZero);
            System.Globalization.CultureInfo inv =
                System.Globalization.CultureInfo.InvariantCulture;
            string condition = d.Text.Length > 0 ? d.Text : CodeToCn(d.Code);
            string s = d.City + " " + condition + " " +
                tmin.ToString(inv) + "~" + tmax.ToString(inv) + "°C";
            if (d.PrecipProb >= 30) s += " · 降水概率" + d.PrecipProb + "%";
            return s;
        }

        private static string HttpGet(string url, int timeoutMs)
        {
            return HttpGet(url, timeoutMs, null);
        }

        private static string HttpGet(string url, int timeoutMs, Dictionary<string, string> headers)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "GET";
            req.Timeout = timeoutMs;
            req.ReadWriteTimeout = timeoutMs;
            req.UserAgent = "DeepBlue/" + AppInfo.Version;
            req.AllowAutoRedirect = true;
            if (headers != null)
            {
                foreach (KeyValuePair<string, string> kv in headers)
                {
                    req.Headers[kv.Key] = kv.Value;
                }
            }
            using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
            using (Stream s = resp.GetResponseStream())
            using (StreamReader sr = new StreamReader(s, Encoding.UTF8))
            {
                return sr.ReadToEnd();
            }
        }

        private static Dictionary<string, object> ParseObj(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            JavaScriptSerializer ser = new JavaScriptSerializer();
            return ser.DeserializeObject(json) as Dictionary<string, object>;
        }

        private static object First(Dictionary<string, object> daily, string key)
        {
            object arrObj;
            if (!daily.TryGetValue(key, out arrObj)) return null;
            object[] arr = arrObj as object[];
            if (arr == null || arr.Length == 0) return null;
            return arr[0];
        }

        private static object Val(Dictionary<string, object> r, string key)
        {
            object o;
            return r.TryGetValue(key, out o) ? o : null;
        }

        private static string Str(Dictionary<string, object> r, string key)
        {
            object o = Val(r, key);
            return o == null ? "" : o.ToString();
        }

        private static int ToInt(object o, int fallback)
        {
            if (o is int) return (int)o;
            if (o is double) return (int)Math.Round((double)o, MidpointRounding.AwayFromZero);
            if (o is decimal) return (int)Math.Round((decimal)o, MidpointRounding.AwayFromZero);
            if (o is string)
            {
                int i;
                if (int.TryParse((string)o, out i)) return i;
            }
            return fallback;
        }

        private static double ToDouble(object o, double fallback)
        {
            if (o is int) return (int)o;
            if (o is double) return (double)o;
            if (o is decimal) return (double)(decimal)o;
            if (o is string)
            {
                double v;
                if (double.TryParse((string)o, out v)) return v;
            }
            return fallback;
        }
    }
}
