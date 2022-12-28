using RestSharp;
using System;
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
            var oscid = Environment.ExpandEnvironmentVariables("%oscid%");
            var pow_batch = 5;
            if (int.TryParse(Environment.ExpandEnvironmentVariables("%pow_batch%"), out   pow_batch))
            {
                pow_batch = pow_batch < 5 ? 5 : pow_batch;
            }
            Console.WriteLine($"{objId} {g_user_id}");
            List<Oscresult> oscresults = new List<Oscresult>();
            for (int i = 0; i < pow_batch; i++)
            {
                oscresults.Add(Pow(g_user_id, objId));
            }
            var client = new RestSharp.RestClient();
            RestRequest rest = new RestRequest("https://www.oschina.net/action/api/pow",    Method.Post);
            rest.AddHeader("Content-Type", "application/json;charset=utf-8");
            rest.AddHeader("User-Agent", "Mozilla/5.0 (Linux; Android 6.0; Nexus 5 Build/MRA58N) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/108.0.0.0 Mobile Safari/537.36");
            rest.AddHeader("Cookie", $"oscid={oscid}");
            rest.AddJsonBody(oscresults);
            var result= client.Execute(rest);
            Console.WriteLine(result.Content);
            Console.ReadKey();
        }
       static string  RandomString(int length)
        {
            const string characters = "abcdefghijklmnopqrstuvwxyz0123456789";
            return new string(Enumerable.Repeat(characters, length)
              .Select(s => s[Random.Shared.Next(s.Length)]).ToArray());
        }

        private static Oscresult? Pow(string oscid, string projectid)
        {
            var IsOk = true;
            var counter = 0;
            while (IsOk)
            {
                var token = RandomString(36 - 8);
                for (int i = 0; i < 999999; i++)
                {
                    var genkey = projectid + ":" + oscid + ":" + counter + ":" + token;
                    var testres =string.Join("", SHA1.HashData(Encoding.Default.GetBytes(genkey)).Select(b=>b.ToString("x2")));
                    if (testres.StartsWith("00000") || testres.ToLower().Contains("oschina", StringComparison.CurrentCulture))
                    {
                        return new Oscresult(oscid, projectid, token, counter);
                    }
                    counter++;
                }
            }
            return null;
        }
    }
}