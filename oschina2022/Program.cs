using RestSharp;
using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;

namespace oschina2022
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            var objId = Environment.ExpandEnvironmentVariables("%objId%");
            var g_user_id = Environment.ExpandEnvironmentVariables("%g_user_id%");
            var oscid = Environment.ExpandEnvironmentVariables("%oscid%").Trim('"');
            Console.WriteLine($"项目ID:{objId}{Environment.NewLine}用户ID:{g_user_id}{Environment.NewLine}:登录信息:{oscid}{Environment.NewLine}。{Environment.NewLine}");
            var proxy = Environment.ExpandEnvironmentVariables("%https_proxy%");
            Console.WriteLine($"CPU数量:{Environment.ProcessorCount} 系统代理:{proxy}");
            CancellationTokenSource tokenSource = new();
            Console.CancelKeyPress += (object sender, ConsoleCancelEventArgs e) =>
            {
                Console.WriteLine("5秒后开始取消...");
                tokenSource.CancelAfter(TimeSpan.FromSeconds(5));
                e.Cancel = true;
            };
           
            var client = new RestClient();
            client.Options.MaxTimeout = (int)TimeSpan.FromMinutes(3).TotalMilliseconds;
            client.AddDefaultHeader("Content-Type", "application/json;charset=utf-8");
            client.AddDefaultHeader("User-Agent", "Mozilla/5.0 (Linux; Android 6.0; Nexus 5 Build/MRA58N) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/108.0.0.0 Mobile Safari/537.36");
            client.AddDefaultHeader("Cookie", $"oscid={oscid}");
            if (!string.IsNullOrEmpty(proxy) && proxy.StartsWith("https", StringComparison.OrdinalIgnoreCase))
            {
                client.Options.Proxy = new WebProxy(proxy);
            }
            var lstcheck = DateTime.MinValue;
            do
            {
                var dt = DateTime.Now;
                Console.WriteLine($"线程{Task.CurrentId}开始计算{dt:yyyy-MM-dd HH:mm.ss.ffff}");
                int _count = 0;
                int _scores = 0;
                ConcurrentBag<PowDto> oscresults = new();
                Parallel.For(1, 31, new ParallelOptions() { MaxDegreeOfParallelism=Environment.ProcessorCount } _ =>
                {
                    var dt2 = DateTime.Now;
                    var osc = Pow(g_user_id, objId, out string sha1, out int scores, tokenSource.Token);
                    if (osc != null)
                    {
                        oscresults.Add(osc);
                        _count++;
                        _scores += scores;
                        Console.WriteLine($"线程{Environment.CurrentManagedThreadId} SHA1:{sha1}  耗时{DateTime.Now.Subtract(dt2).TotalSeconds}");
                    }
                });
                Console.WriteLine($"线程{Task.CurrentId}计算完成耗时{DateTime.Now.Subtract(dt).TotalSeconds}{Environment.NewLine}开始提交{DateTime.Now:yyyy-MM-dd HH:mm.ss.ffff}{Environment.NewLine}");

                Task.Run(() =>
                {
                    try
                    {
                        var dt1 = DateTime.Now;
                        RestRequest rest = new("https://www.oschina.net/action/api/pow", Method.Post)
                        {
                            Timeout = (int)TimeSpan.FromMinutes(3).TotalMilliseconds
                        };
                        rest.AddJsonBody(oscresults.ToArray());
                        var result = client.Execute(rest);
                        var jo = JsonNode.Parse(result.Content ?? "{}");
                        var _integral = jo["data"]?.AsArray().Sum(jn => jn["integral"].AsValue().GetValue<long>());
                        Console.WriteLine($"计算期望热度{_scores}实际获得热度：{_integral} 服务器返回:{(_integral > 0 ? result.StatusCode.ToString() : result.Content ?? result.ErrorMessage)}耗时{DateTime.Now.Subtract(dt1).TotalSeconds} ");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"查询最新热度时遇到异常{ex.Message}");
                    }
                });

                Task.Run(() =>
               {
                   if (DateTime.Now.Subtract(lstcheck).TotalMinutes > 5)
                   {
                       try
                       {
                           lstcheck = DateTime.Now;
                           var dt1 = DateTime.Now;
                           RestRequest req = new($"https://www.oschina.net/action/api/pow_integral?project={objId}", Method.Get);
                           var rep = client.Execute(req, tokenSource.Token);
                           var jo = JsonNode.Parse(rep.Content ?? "{}");
                           var _integral = jo["data"]?["integral"]?.AsValue().GetValue<long>();
                           Console.WriteLine($"最新热度值:{_integral} 服务器返回:{(_integral > 0 ? rep.StatusCode.ToString() : rep.Content ?? rep.ErrorMessage)} 耗时{DateTime.Now.Subtract(dt1).TotalSeconds}");
                       }
                       catch (Exception ex)
                       {
                           Console.WriteLine($"查询最新热度时遇到异常{ex.Message}");
                       }
                   }
               });
            } while (!tokenSource.IsCancellationRequested);
        }

        private static readonly RandomNumberGenerator random = RandomNumberGenerator.Create();

        private static string RandomString(int length)
        {
            var buffer = new byte[length];
            lock (random)
            {
                random.GetNonZeroBytes(buffer);
            }
            return Convert.ToBase64String(buffer);
        }

        private static PowDto Pow(string oscid, string projectid, out string sha1, out int scores, CancellationToken cancellation)
        {
            var counter = 0;
            scores = 0;
            sha1 = string.Empty;
            while (true)
            {
                var token = RandomString(32);
                for (int i = 0; i < 999999; i++)
                {
                    var genkey = projectid + ":" + oscid + ":" + counter + ":" + token;
                    var testres = string.Concat(SHA1.HashData(Encoding.Default.GetBytes(genkey)).Select(b => b.ToString("x2")));
                    if (cancellation.IsCancellationRequested)
                    {
                        return null;
                    }
                    sha1 = testres;
                    if (sha1.StartsWith("000000"))
                    {
                        scores = 10;
                    }
                    else if (sha1.StartsWith("00000"))
                    {
                        scores = 1;
                    }
                    else if (testres.Contains("oschina", StringComparison.OrdinalIgnoreCase))
                    {
                        scores = 10000;
                    }
                    if (scores > 0)
                    {
                        return new PowDto() { user = oscid, project = projectid, token = token, counter = counter };
                    }
                    counter++;
                }
            }
        }
    }

    public class pow_data
    {
        public pow_data()
        {
            integral = 0;
            project = 0;
            counter = 0;
            user = 0;
        }

        public int integral { get; set; }

        public int project { get; set; }

        public int counter { get; set; }

        public int user { get; set; }

        public string token { get; set; }
    }

    public class PowDto
    {
        public string user { get; set; }
        public string project { get; set; }
        public string token { get; set; }
        public int counter { get; set; }
    }
}