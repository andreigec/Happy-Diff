using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ANDREICSLIB.Helpers;
using NLog;

namespace HappyDiff
{
    public static class Controller
    {
        public static ICache c = new LocalJSONCache("cache");


        public static async Task Run()
        {
            var l = LogManager.GetLogger("main");
            var items = await Controller.GetApis();
            l.Info($"Starting execution\r\ntests:");
            foreach (var t in items)
            {
                l.Trace("");
                l.Info($"{t.Name} API1=:{t.API1}");
                l.Info($"{t.Name} API2=:{t.API2}");
                var res = await JSONDiffHelpers.Process(t);
                ProcessResults(res, l, t);
                l.Trace("");
            }
            l.Info($"\r\nFinished execution");
        }

        public static async Task<List<APICall>> GetApis()
        {
            var c1 = (await c.Get<List<APICall>>("apis"));
            var items = (c1?.OrderBy(s => s.Name).ToList()) ?? new List<APICall>();
            return items;
        }

        public static async Task SetApis(List<APICall> items)
        {
            await c.Set("apis", items);
        }

        public static void ProcessResults(JSONDiff jd, Logger l, APICall t)
        {
            if (jd.Messages.Any())
            {
                l.Info($"--Issues--");
                jd.Messages.ForEach(s2 =>
                {
                    var mess = s2.Message?.Trim();
                    var exp = s2.Exception?.Trim();
                    var m = $"{t.Name} {s2.ProblemType} | {mess} | {exp}";
                    m = m.Replace("\r\n", "");
                    switch (s2.WarnLevel)
                    {
                        case JSONWarnLevel.Warn:
                            l.Warn(m);
                            break;
                        case JSONWarnLevel.Error:
                            l.Error(m);
                            break;
                        case JSONWarnLevel.Fatal:
                            l.Fatal(m);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                });
                l.Info($"-----------");
            }
            else
            {
                l.Info($"--Success--");
                l.Info($"-----------");
            }
        }
    }
}
