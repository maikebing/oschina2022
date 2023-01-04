using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

RandomNumberGenerator random = RandomNumberGenerator.Create();
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
var httpClientHandler = new HttpClientHandler();

if (!string.IsNullOrEmpty(proxy) && proxy.StartsWith("https", StringComparison.OrdinalIgnoreCase))
{
    httpClientHandler.Proxy = new WebProxy(proxy);
}
var client = new HttpClient(httpClientHandler);
client.Timeout = TimeSpan.FromMinutes(3);
client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
client.DefaultRequestHeaders.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));
client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Linux; Android 6.0; Nexus 5 Build/MRA58N) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/108.0.0.0 Mobile Safari/537.36");
client.DefaultRequestHeaders.Add("Cookie", $"oscid={oscid}");
var lstcheck = DateTime.MinValue;
do
{
    var dt = DateTime.Now;
    Console.WriteLine($"线程{Task.CurrentId}开始计算{dt:yyyy-MM-dd HH:mm.ss.ffff}");
    int _count = 0;
    int _scores = 0;
    var oscresults = new ConcurrentBag<PowDto>();
    Parallel.For(1, 31, _ =>
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

    Task.Run(async () =>
    {
        try
        {
            var dt1 = DateTime.Now;
            var rest = new HttpRequestMessage(HttpMethod.Post, "https://www.oschina.net/action/api/pow");
            var array = new JsonArray();
            foreach (var p in oscresults)
            {
                array.Add(new JsonObject
                {
                    { "user", p.user },
                    { "token", p.token },
                    { "project", p.project },
                    { "counter", p.counter }
                }.Root);
            }
            rest.Content = new StringContent(array.ToString());
            var result = await client.SendAsync(rest);
            var context = await result.Content?.ReadAsStringAsync();
            var jo = JsonNode.Parse(context ?? "{}");
            var _integral = jo["data"]?.AsArray().Sum(jn => jn["integral"].GetValue<long>());
            Console.WriteLine($"计算期望热度{_scores}实际获得热度：{_integral} 服务器返回:{(_integral > 0 ? result.StatusCode.ToString() : context ?? result.ReasonPhrase)}耗时{DateTime.Now.Subtract(dt1).TotalSeconds} ");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"提交计算结果时遇到错误:{ex.Message}");
        }
    });

    Task.Run(async () =>
    {
        if (DateTime.Now.Subtract(lstcheck).TotalMinutes > 5)
        {
            try
            {
                lstcheck = DateTime.Now;
                var dt1 = DateTime.Now;
                HttpRequestMessage rest = new(HttpMethod.Get, $"https://www.oschina.net/action/api/pow_integral?project={objId}");
                var rep = await client.SendAsync(rest);
                var context = await rep.Content?.ReadAsStringAsync();
                var jo = JsonNode.Parse(context ?? "{}");
                var _integral = jo["data"]?["integral"]?.AsValue().GetValue<long>();
                Console.WriteLine($"最新热度值:{_integral} 服务器返回:{(_integral > 0 ? rep.StatusCode.ToString() : context ?? rep.ReasonPhrase)} 耗时{DateTime.Now.Subtract(dt1).TotalSeconds}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"查询最新热度时遇到异常{ex.Message}");
            }
        }
    });
} while (!tokenSource.IsCancellationRequested);

string RandomString(int length)
{
    var buffer = new byte[length];
    lock (random)
    {
        random.GetNonZeroBytes(buffer);
    }
    return Convert.ToBase64String(buffer);
}

PowDto Pow(string oscid, string projectid, out string sha1, out int scores, CancellationToken cancellation)
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

public class PowDto
{
    public string user { get; set; }
    public string project { get; set; }
    public string token { get; set; }
    public int counter { get; set; }
}