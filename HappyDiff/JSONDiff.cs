using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ANDREICSLIB.ClassExtras;
using ANDREICSLIB.Extracters;
using Newtonsoft.Json.Linq;

namespace HappyDiff
{
    public enum JSONProblem
    {
        WithAPI1, WithAPI2, Both
    }

    public enum JSONWarnLevel
    {
        Warn, Error, Fatal
    }

    public class JSONDiffRow
    {
        public JSONDiffRow(JSONProblem problemType, JSONWarnLevel warnLevel, string message, Exception ex = null)
        {
            ProblemType = problemType;
            WarnLevel = warnLevel;
            Message = message;
            Exception = ex?.ToString();
        }

        public JSONProblem ProblemType { get; set; }
        public JSONWarnLevel WarnLevel { get; set; }
        public string Message { get; set; }
        public string Exception { get; set; }
    }

    public class JSONDiff
    {
        public JSONDiff(string api1, string api2)
        {
            API1 = api1;
            API2 = api2;
            Messages = new List<JSONDiffRow>();
        }

        public string API1 { get; set; }
        public string API2 { get; set; }
        public string RowCount { get; set; }
        public List<JSONDiffRow> Messages { get; set; }



        public void AddItemAPI1(JSONWarnLevel w, string message, Exception ex)
        {
            Messages.Add(new JSONDiffRow(JSONProblem.WithAPI1, w, message, ex));
        }

        public void AddItemAPI2(JSONWarnLevel w, string message, Exception ex)
        {
            Messages.Add(new JSONDiffRow(JSONProblem.WithAPI2, w, message, ex));
        }

        public void AddItemBoth(JSONWarnLevel w, string message)
        {
            Messages.Add(new JSONDiffRow(JSONProblem.Both, w, message));
        }
        public void AddItemAPI1(JSONWarnLevel w, string message)
        {
            Messages.Add(new JSONDiffRow(JSONProblem.WithAPI1, w, message));
        }
        public void AddItemAPI2(JSONWarnLevel w, string message)
        {
            Messages.Add(new JSONDiffRow(JSONProblem.WithAPI2, w, message));
        }
    }

    public static class JTokenHelpers
    {
        public static string GetFieldHash(this JToken token)
        {
            var phi = token.Children().Cast<JProperty>().Select(s => s.Name).ToArray();
            var str = string.Join("", phi);
            var hash = str.GetHashCode().ToString();
            return hash;
        }
    }


    public static class JSONDiffHelpers
    {
        public static async Task<JSONDiff> Process(APICall data)
        {
            JToken d1;
            JToken d2;
            JSONExtract j = new JSONExtract();
            var jsonresultitem = new JSONDiff(data.API1, data.API2);
            try
            {
                d1 = await GetApi(j, data.API1);
            }
            catch (Exception ex)
            {
                jsonresultitem.AddItemAPI1(JSONWarnLevel.Fatal, "error reading url", ex);
                return jsonresultitem;
            }

            try
            {
                d2 = await GetApi(j, data.API2);
            }
            catch (Exception ex)
            {
                jsonresultitem.AddItemAPI2(JSONWarnLevel.Fatal, "error reading url", ex);
                return jsonresultitem;
            }

            JsonDifferenceReport(d1, d2, jsonresultitem, "root");

            if (jsonresultitem.Messages.Any())
            {
                //filter based on parent count
                var messages = jsonresultitem.Messages.GroupBy(s => s.Message).Select(s => s).ToList();
                foreach (var g in messages)
                {
                    var gc = g.Count();
                    foreach (var m in g.Skip(1))
                    {
                        jsonresultitem.Messages.Remove(m);
                    }
                    g.First().Message = g.First().Message + $"[reoccured {gc} times]";
                }
            }
            return jsonresultitem;
        }

        public static async Task<List<JSONDiff>> Process(List<APICall> data)
        {

            var jsonresult = new List<JSONDiff>();
            try
            {
                foreach (var d in data)
                {
                    var ret = await Process(d);
                    jsonresult.Add(ret);
                }
            }
            catch (Exception ex)
            {
                throw;
            }
            return jsonresult;
        }

        public static async Task<JToken> GetApi(JSONExtract j, string url)
        {
            var raw = await j.DownloadPage(url, null, "GET");
            JToken token = JToken.Parse(raw);
            return token;
        }


        public static List<string> GetKeys(JToken j)
        {
            var ch = j.Children().OfType<JProperty>().Select(s => s.Name).Distinct().ToList();
            return ch;
        }


        public static void JsonDifferenceReport(JToken API1, JToken API2, JSONDiff j, string path)
        {
            if (null == API1)
                throw new ArgumentNullException("API1");
            if (null == API2)
                throw new ArgumentNullException("API2");

            var t1 = API1.GetType();
            var t2 = API2.GetType();
            if (t1 != t2)
            {
                j.AddItemBoth(JSONWarnLevel.Error, $"api one and api two have a different key value type. api one type={t1.Name}, api two type={t2.Name}");
                return;
            }

            if (API1 is JArray && API2 is JArray)
            {
                var ja1 = API1 as JArray;
                var ja2 = API2 as JArray;

                Enumerable.Range(0, ja1.Count).ToList().ForEach(s => JsonDifferenceReport(ja1[s], ja2[s], j, path + "array"));
                return;
            }

            var keys1 = GetKeys(API1);
            var keys2 = GetKeys(API2);

            var ints = ListExtras.Intersect(keys1, keys2);

            if (ints.TwoOnly.Any())
                j.AddItemAPI1(JSONWarnLevel.Warn, "api doesnt have:" + string.Join(",", ints.TwoOnly));

            if (ints.OneOnly.Any())
                j.AddItemAPI2(JSONWarnLevel.Warn, "api doesnt have:" + string.Join(",", ints.OneOnly));

            foreach (var key in ints.SameElements)
            {
                JToken v1 = API1[key];
                JToken v2 = API2[key];

                t1 = v1.GetType();
                t2 = v2.GetType();
                if (t1 != t2)
                    j.AddItemBoth(JSONWarnLevel.Error, $"api one and api two have a different key value type. key={key}, api one type={t1.Name}, api two type={t2.Name}");
                else
                {
                    if (v1 is JValue)
                    {
                        var val1 = v1.Value<object>().ToString();
                        var val2 = v2.Value<object>().ToString();
                        if (val1 != val2)
                        {
                            j.AddItemBoth(JSONWarnLevel.Warn, $"api one and api two value mismatch. key={key}, api one val={v1.Value<object>()}, api two val={v2.Value<object>()}");
                        }
                    }
                    else if (v1 is JArray)
                    {
                        var ja1 = v1 as JArray;
                        var ja2 = v2 as JArray;

                        Enumerable.Range(0, Math.Max(ja1.Count, ja2.Count)).ToList().ForEach(s =>
                        {
                            if (s >= ja1.Count || ja1[s] == null)
                                j.AddItemAPI1(JSONWarnLevel.Error, "api doesnt have item in array:" + string.Join(",", ints.TwoOnly));
                            else if (s >= ja2.Count || ja2[s] == null)
                                j.AddItemAPI2(JSONWarnLevel.Error, "api doesnt have item in array:" + string.Join(",", ints.OneOnly));
                            else

                                JsonDifferenceReport(ja1[s], ja2[s], j, path + $"array[{key}]");
                        });
                    }
                    //class
                    else if (v1 is JObject)
                    {
                        JsonDifferenceReport(v1 as JObject, v2 as JObject, j, path + $"object[{key}]");
                    }
                }
            }
        }
    }
}
