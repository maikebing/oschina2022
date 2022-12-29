using RestSharp;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace oschina2022
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var objId = Environment.ExpandEnvironmentVariables("%objId%");
            var g_user_id = Environment.ExpandEnvironmentVariables("%g_user_id%");
            var oscid = Environment.ExpandEnvironmentVariables("%oscid%").Trim('"') ;
            var pow_batch = 5;
            if (int.TryParse(Environment.ExpandEnvironmentVariables("%pow_batch%"), out   pow_batch))
            {
                pow_batch = pow_batch < 5 ? 5 : pow_batch;
            }
            Console.WriteLine($"项目ID:{objId}{Environment.NewLine}用户ID:{g_user_id}{Environment.NewLine}:登录信息:{oscid}{Environment.NewLine}{pow_batch}个数据提交一次。{Environment.NewLine}");
          


            var dt = DateTime.Now;
            Console.WriteLine($"开始计算{dt.ToString("yyyy-MM-dd HH:mm.ss.ffff")}");
            ConcurrentQueue<Oscresult> oscresults = new ConcurrentQueue<Oscresult>();
           
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            var popt = new ParallelOptions()
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount,
            };
            Console.CancelKeyPress += (object sender, ConsoleCancelEventArgs e) =>
            {
                Console.WriteLine("5秒后开始取消");
                tokenSource.CancelAfter(TimeSpan.FromSeconds(5));
                e.Cancel = true;
            };
            Parallel.For(1, pow_batch, popt, _ =>
            {
                var osc = Pow(g_user_id, objId, out string sha1, out int scores, tokenSource.Token);
                if (osc != null)
                {
                    oscresults.Append(osc);
                    Console.WriteLine($"通过{osc.token}算得{sha1} 热度值{scores}.");
                }
            });
            Console.WriteLine($"计算完成耗时{DateTime.Now.Subtract(dt).TotalSeconds}{Environment.NewLine}开始提交{DateTime.Now.ToString("yyyy-MM-dd HH:mm.ss.ffff")}{Environment.NewLine}");
            var dt1 = DateTime.Now;

            var client = new RestSharp.RestClient();
            var proxy = Environment.ExpandEnvironmentVariables("%https_proxy%");
            if (string.IsNullOrEmpty(proxy))
            {
                client.Options.Proxy = new WebProxy(proxy);
            }
            RestRequest rest = new RestRequest("https://www.oschina.net/action/api/pow",    Method.Post);
            rest.AddHeader("Content-Type", "application/json;charset=utf-8");
            rest.AddHeader("User-Agent", "Mozilla/5.0 (Linux; Android 6.0; Nexus 5 Build/MRA58N) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/108.0.0.0 Mobile Safari/537.36");
            rest.AddHeader("Cookie", $"oscid={oscid}");
            rest.AddJsonBody(oscresults.ToArray());
            var result= client.Execute(rest);
            Console.WriteLine($"{result.StatusCode} {result.Content}" );
            Console.WriteLine($"提交完成耗时{DateTime.Now.Subtract(dt1).TotalSeconds}{Environment.NewLine}");


        }
 

        static string  RandomString(int length)
        {
            const string characters = "abcdefghijklmnopqrstuvwxyz0123456789";
            return new string(Enumerable.Repeat(characters, length)
              .Select(s => s[Random.Shared.Next(s.Length)]).ToArray());
        }

        private static Oscresult Pow(string oscid, string projectid,out string sha1, out int scores,CancellationToken cancellation)
        {
            var IsOk = true;
            var counter = 0;
            scores = 0;
            sha1 = string.Empty;
            while (IsOk)
            {
                var token = RandomString(24);
                for (int i = 0; i < 999999; i++)
                {
                    var genkey = projectid + ":" + oscid + ":" + counter + ":" + token;
                    var testres =string.Join("", SHA1.HashData(Encoding.Default.GetBytes(genkey)).Select(b=>b.ToString("x2")));
                    if (cancellation.IsCancellationRequested)
                    {
                        return null;
                    }
                    if (testres.StartsWith("00000") || testres.ToLower().Contains("oschina", StringComparison.OrdinalIgnoreCase))
                    {
                        sha1 = testres;
                        if (sha1.StartsWith("000000"))
                        {
                            scores = 10;
                        }
                        else if (testres.ToLower().Contains("oschina", StringComparison.OrdinalIgnoreCase))
                        {
                            scores = 10000;
                        }
                        else
                        {
                            scores = 1;
                        }
                        return new Oscresult(oscid, projectid, token, counter);
                    }
                    counter++;
                  
                }
            }
            return null;
        }
    }
}