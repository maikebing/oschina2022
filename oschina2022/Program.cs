using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
 

char[] constant =
{
        '0','1','2','3','4','5','6','7','8','9',
        'a','b','c','d','e','f','g','h','i','j','k','l','m','n','o','p','q','r','s','t','u','v','w','x','y','z',
        'A','B','C','D','E','F','G','H','I','J','K','L','M','N','O','P','Q','R','S','T','U','V','W','X','Y','Z'
      };
var jd = JsonNode.Parse(System.IO.File.ReadAllText("config.json"));
var objId = jd["project"]?.GetValue<string>();
var proxy = jd["proxy"]?.GetValue<string>();
List<(string g_user_id, string oscid)> _user = new List<(string g_user_id, string oscid)>();
jd["users"].AsArray().ToList().ForEach(n =>
{
    var u = n["g_user_id"]?.GetValue<string>();
    var osid = n["oscid"]?.GetValue<string>();
    _user.Add((u, osid));
});

Console.WriteLine($"项目ID:{objId}{Environment.NewLine}用户信息共计{_user.Count}个{Environment.NewLine}");
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

var lstcheck = DateTime.MinValue;
bool _is_full20w = false;
var oscresults = new Queue<PowDto>();
ReloadInfo(objId, client);
do
{
    var user = _user[RandomNumberGenerator.GetInt32(0, _user.Count)];
    var dt = DateTime.Now;
    Console.WriteLine($"线程{Task.CurrentId}开始计算{dt:yyyy-MM-dd HH:mm.ss.ffff}");
    int _count = 0;
    int _scores = 0;
    Parallel.For(0, 30, _ =>
    {
        var dt2 = DateTime.Now;
        var osc = Pow(user.g_user_id, objId, out string sha1,  out int scores, tokenSource.Token);
        if (osc != null)
        {
            oscresults.Enqueue(osc);
            _count++;
            _scores += scores;
            Console.WriteLine($"线程{Environment.CurrentManagedThreadId} {sha1}  获得{scores}热度值 耗时{DateTime.Now.Subtract(dt2).TotalSeconds}");
        }
    });
    Console.WriteLine($"线程{Task.CurrentId}计算完成耗时{DateTime.Now.Subtract(dt).TotalSeconds}{Environment.NewLine}开始提交{DateTime.Now:yyyy-MM-dd HH:mm.ss.ffff}{Environment.NewLine}");
    if (oscresults.Count > 25)
    {
        Task.Run(async () =>
        {
            try
            {
                var dt1 = DateTime.Now;
                var rest = new HttpRequestMessage(HttpMethod.Post, "https://www.oschina.net/action/api/pow");
                rest.Headers.Add("Cookie", $"oscid={user.oscid}");
                var array = new JsonArray();
                for (int i = 0; i < 30; i++)
                {
                    if (oscresults.TryDequeue(out var p))
                    {
                        array.Add(new JsonObject
                    {
                    { "user", p.user },
                    { "token", p.token },
                    { "project", p.project },
                    { "counter", p.counter }
                    }.Root);
                    }
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
    }
    else
    {
        Console.WriteLine($"计算的结果数大于25个时在提交");
    }
    ReloadInfo(objId, client);
} while (!tokenSource.IsCancellationRequested);

byte[] RandomString(int length)
{

    byte[] buffer = new byte[length];
    for (int i = 0; i < length; i++)
    {
        buffer[i]= (byte)constant[RandomNumberGenerator.GetInt32(constant.Length)];
    }
    return buffer;
}

PowDto Pow(string oscid, string projectid, out string sha1  ,out int scores, CancellationToken cancellation)
{
    var counter = 0;
    scores = 0;
    var _tokenlen = 16;
    sha1 = string.Empty;
    var _head = Encoding.ASCII.GetBytes(projectid + ":" + oscid + ":").AsSpan();
    while (true)
    {
        var token = RandomString(_tokenlen);
        for (int i = 0; i < 999999; i++)
        {
            if (cancellation.IsCancellationRequested)
            {
                return null;
            }
            var _c = Encoding.ASCII.GetBytes(counter.ToString()).AsSpan();
            Span<byte> bytes = new byte[_head.Length + _tokenlen + 1 + _c.Length];
            _head.CopyTo(bytes);
            _c.CopyTo(bytes.Slice(_head.Length));
            bytes[_head.Length + _c.Length] = (byte)':';
            token.CopyTo(bytes.Slice( _head.Length + _c.Length + 1));
            var _bin = SHA1.HashData(bytes);
            if (_bin[0]==0 && _bin[1]==0 && _bin[2]==0)
            {
                scores = 10;
            }
            else if (!_is_full20w && _bin[0] == 0 && _bin[1] == 0 && (byte)((_bin[2] >> 4)) == 0)
            {
                scores = 1;
            }
            //else if (testres.Contains("oschina", StringComparison.OrdinalIgnoreCase))
            //{
            //    scores = 10000;
            //}
            if (scores > 0)
            {
                sha1 =Convert.ToHexString(_bin);
                return new PowDto() { user = oscid, project = projectid, token = Encoding.ASCII.GetString( token), counter = counter };
            }
            counter++;
        }
    }
}



 void ReloadInfo(string objId, HttpClient client)
{
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
                if (_integral > 200000)
                {
                    _is_full20w = true;
                }
                Console.WriteLine($"最新热度值:{_integral} 服务器返回:{(_integral > 0 ? rep.StatusCode.ToString() : context ?? rep.ReasonPhrase)} 耗时{DateTime.Now.Subtract(dt1).TotalSeconds}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"查询最新热度时遇到异常{ex.Message}");
            }
        }
    });
}

public class PowDto
{
    public string user { get; set; }
    public string project { get; set; }
    public string token { get; set; }
    public int counter { get; set; }
}