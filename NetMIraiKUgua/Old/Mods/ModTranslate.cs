using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GoogleTranslateFreeApi;

using MMDK.Util;
using System.Windows.Interop;
using System.Net.Http;
using System.Net;

namespace MMDK.Mods
{
    class ModTranslate : Mod
    {
        Dictionary<string, string> ctlist = new Dictionary<string, string>();


        public const string TranslateURL = "https://translate.google.cn/translate_a/single?client=gtx&dt=t&ie=UTF-8&oe=UTF-8&sl=auto&tl=zh-CN";

        private Regex regex = new Regex("(?<=\\[\\\").*?(?=\\\")");
        //替换掉翻译结果中的id
        private Regex rreplaceid = new Regex("\\[\\[\\[\\\"[0-9a-z]+\\\"\\,\\\"\\\"\\]");

        public bool Init(string[] args)
        {
            try
            {
                ctlist = new Dictionary<string, string>();
                var lines = FileManager.ReadResourceLines("Translate");
                foreach (var line in lines)
                {
                    string[] vitem = line.Split('\t');
                    if (vitem.Length >= 2) ctlist[vitem[0]] = vitem[1];
                }
            }
            catch (Exception e)
            {
                Logger.Instance.Log(e.Message + "\r\n" + e.StackTrace);
            }
            return true;
        }

        public void Exit()
        {
            throw new NotImplementedException();
        }

        public bool HandleText(long userId, long groupId, string message, List<string> results)
        {
            // z暂时不可用
            return false;
            try
            {
                // 翻译
                Regex transreg = new Regex(@"(\S+)译(\S+)\s+");
                var transmatch = transreg.Match(message);
                if (transmatch.Success)
                {
                    string msgyilist = transmatch.Groups[0].Value.Trim();
                    string msgtar = message.Substring(msgyilist.Length).Trim();
                    var lists = msgyilist.Split('译');
                    
                    if (lists.Length >= 2 && msgtar.Length > 0)
                    {
                        string tempResult = msgtar;
                        //Task<string> res;
                        for (int i = 0; i < lists.Length - 1; i++)
                        {
                            string lang1 = lists[i];
                            lang1 = GetLanguateISO(lang1);
                            if (string.IsNullOrWhiteSpace(lang1))
                            {
                                lang1 = "auto";
                            }



                            string lang2 = lists[i + 1];
                            lang2 = GetLanguateISO(lang2);
                            if (string.IsNullOrWhiteSpace(lang2))
                            {
                                lang2 = "zh-CN";
                            }

                            Language l1 = GoogleTranslator.GetLanguageByISO(lang1);
                            Language l2 = GoogleTranslator.GetLanguageByISO(lang2);
                            //var res =  Translate(tempResult, l1, l2);
                            //tempResult = res.Result;
                        }
                        results.Add(tempResult);
                        return true;
                    }
                }
            }
            catch(Exception ex)
            {

            }
            return false;
        }

        string GetLanguateISO(string name)
        {
            string fn = "";
            if (ctlist.ContainsKey(name)) fn = ctlist[name];
            else if (ctlist.ContainsKey(name + "文")) fn = ctlist[name + "文"];
            else if (ctlist.ContainsKey(name + "语")) fn = ctlist[name + "语"];
            return fn;
        }



        //static async Task<string> Translate(string message, Language from, Language to)
        //{
        //    try
        //    {
        //        var rr = WebLinker.getData("http://qq.com");

        //        // 设置本机 HTTP 代理的地址和端口
        //        var proxyAddress = "http://127.0.0.1:7897"; // 根据实际情况修改
        //        var proxyUri = new Uri(proxyAddress);

        //        // 创建 HttpClientHandler 并配置代理
        //        var httpClientHandler = new HttpClientHandler
        //        {
        //            //Proxy = new WebProxy(proxyUri),
        //            //UseProxy = true
        //        };

        //        // 创建 HttpClient 实例
        //        using (var httpClient = new HttpClient(httpClientHandler))
        //        {
        //            try
        //            {
        //                // 发起 GET 请求
        //                var response = await httpClient.GetAsync("http://qq.com"); // 更改为所需访问的网站
        //                //response.EnsureSuccessStatusCode(); // 确保请求成功

        //                // 读取响应内容
        //                var content = await response.Content.ReadAsStringAsync();
        //                Console.WriteLine(content); // 输出响应内容
        //            }
        //            catch (Exception ex)
        //            {
        //                Console.WriteLine($"请求失败: {ex.Message}");
        //            }
        //        }
















        //        var translator = new GoogleTranslator();
        //        Proxy proxy = new Proxy(new Uri("http://localhost:7897"));
        //        translator.Proxy = proxy;
        //        TranslationResult result = await translator.TranslateLiteAsync(message, from, to);
                
        //        //The result is separated by the suggestions and the '\n' symbols
        //        string[] resultSeparated = result.FragmentedTranslation;

        //        //You can get all text using MergedTranslation property
        //        string resultMerged = result.MergedTranslation;

        //        //There is also original text transcription
        //        string transcription = result.TranslatedTextTranscription; // Kon'nichiwa! Ogenkidesuka?

        //        return resultMerged;//}
        //    }
        //    catch(Exception ex)
        //    {

        //    }
        //    return null;
        
    }
}
