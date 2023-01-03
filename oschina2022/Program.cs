using RestSharp;
using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;
using System.Text;
namespace oschina2022
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var objId = Environment.ExpandEnvironmentVariables("%objId%");
            var g_user_id = Environment.ExpandEnvironmentVariables("%g_user_id%");
            var oscid = Environment.ExpandEnvironmentVariables("%oscid%").Trim('"');
            Console.WriteLine($"项目ID:{objId}{Environment.NewLine}用户ID:{g_user_id}{Environment.NewLine}:登录信息:{oscid}{Environment.NewLine}。{Environment.NewLine}");
            var proxy = Environment.ExpandEnvironmentVariables("%https_proxy%");
            Console.WriteLine($"CPU数量:{Environment.ProcessorCount} 系统代理:{proxy}");
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            Console.CancelKeyPress += (object sender, ConsoleCancelEventArgs e) =>
            {
                Console.WriteLine("5秒后开始取消...");
                tokenSource.CancelAfter(TimeSpan.FromSeconds(5));
                e.Cancel = true;
            };
            var client = new RestClient();
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
                Console.WriteLine($"线程{Task.CurrentId}开始计算{dt.ToString("yyyy-MM-dd HH:mm.ss.ffff")}");
                int _count = 0;
                int _scores = 0;
                ConcurrentBag<PowDto> oscresults = new ConcurrentBag<PowDto>();
                Random _random = new Random(DateTime.Now.Second * 100 + DateTime.Now.Microsecond + DateTime.Now.Day + Random.Shared.Next(1000));
                Parallel.For(1, 31, _ =>
                {
                    var dt2 = DateTime.Now;
                    var osc = Pow(g_user_id, objId, out string sha1, out int scores, tokenSource.Token, _random);
                    if (osc != null)
                    {
                        oscresults.Add(osc);
                        _count++;
                        _scores += scores;
                        Console.WriteLine($"线程{Thread.CurrentThread.ManagedThreadId}算得热度值{scores}，耗时{DateTime.Now.Subtract(dt2).TotalSeconds}");
                    }
                });
                Console.WriteLine($"线程{Task.CurrentId}计算完成耗时{DateTime.Now.Subtract(dt).TotalSeconds}{Environment.NewLine}开始提交{DateTime.Now.ToString("yyyy-MM-dd HH:mm.ss.ffff")}{Environment.NewLine}");

                var pow = Task.Run(() =>
                {
                    var dt1 = DateTime.Now;
                    RestRequest rest = new RestRequest("https://www.oschina.net/action/api/pow", Method.Post);
                    rest.AddJsonBody(oscresults.ToArray());
                    var result = client.Execute(rest);
                    var pr = System.Text.Json.JsonSerializer.Deserialize<pow_result>(result.Content);
                    if (pr.result && pr.data?.Count > 0)
                    {
                        var _sc = pr.data.Sum(p => p.integral);
                        Console.WriteLine($"线程{Task.CurrentId}提交完成,总数:{_count},期望热度值:{_scores}，获得热度值{_sc}.耗时{DateTime.Now.Subtract(dt1).TotalSeconds}");
                    }
                    else
                    {
                        Console.WriteLine($"{result.StatusCode} {result.Content}");
                    }
                });

                var pow2 = Task.Run(() =>
                {
                    if (DateTime.Now.Subtract(lstcheck).TotalMinutes > 5)
                    {
                        lstcheck = DateTime.Now;
                        var dt1 = DateTime.Now;
                        RestRequest req = new RestRequest($"https://www.oschina.net/action/api/pow_integral?project={objId}", Method.Get);
                        var rep = client.Execute(req, tokenSource.Token);
                        Console.WriteLine($"最新热度值:{rep.StatusCode} {rep.Content}耗时{DateTime.Now.Subtract(dt1).TotalSeconds}");
                    }
                });
                Task.WaitAll(pow, pow2);
            } while (!tokenSource.IsCancellationRequested);
        }
      

        static string RandomString(int length, Random _random)
        {
            const string characters = "abcdefghijklmnopqrstuvwxyz0123456789";
            return new string(Enumerable.Repeat(characters, length)
              .Select(s => s[_random.Next(s.Length)]).ToArray());
        }

        private static PowDto Pow(string oscid, string projectid, out string sha1, out int scores, CancellationToken cancellation, Random _random)
        {
            var IsOk = true;
            var counter = 0;
            scores = 0;
            sha1 = string.Empty;
            while (IsOk)
            {
                var token =   RandomString(16, _random);
                for (int i = 0; i < 999999; i++)
                {
                    var genkey = projectid + ":" + oscid + ":" + counter + ":" + token;
                    var testres = string.Join("", SHA1.HashData(Encoding.Default.GetBytes(genkey)).Select(b => b.ToString("x2")));
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
                        return new PowDto(oscid, projectid, token, counter);
                    }
                    counter++;
                }

            }
            return null;
        }
    }
}