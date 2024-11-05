using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Security;

using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace MMDK.Util
{

    /// <summary>
    /// 网络请求处理模块
    /// </summary>
    class WebLinker
    {

        public static async void DownloadImageAsync(string url, string localPath)
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    // 获取图片的响应
                    using (HttpResponseMessage response = await client.GetAsync(url))
                    {
                        response.EnsureSuccessStatusCode(); // 确保请求成功

                        // 读取图片内容
                        using (var stream = await response.Content.ReadAsStreamAsync())
                        using (var fileStream = new FileStream(localPath, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            await stream.CopyToAsync(fileStream); // 将内容写入本地文件
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.Log(ex);
                }

            }
        }

        public static async Task<string> PostAsync(string url, string paramString)
        {
            using (HttpClient client = new HttpClient())
            {
                // 设置请求体为 JSON 格式
                var sp=System.Web.HttpUtility.UrlPathEncode(paramString);
                HttpContent content = new StringContent(sp, Encoding.UTF8, "application/x-www-form-urlencoded");
                //client(new UrlEncodedFormEntity(list, "UTF-8"));
                try
                {
                    // 发送 POST 请求
                    HttpResponseMessage response = await client.PostAsync(url, content);

                    // 确认响应状态
                    response.EnsureSuccessStatusCode();

                    // 读取响应内容
                    string responseBody = await response.Content.ReadAsStringAsync();
                    //Console.WriteLine("Received response: " + responseBody);

                    return responseBody;
                }
                catch (HttpRequestException e)
                {
                    Logger.Instance.Log(e);
                }
            }

            return "";
            
        }

        public static async Task<string> PostJsonAsync(string url, string jsonContent)
        {
            using (HttpClient client = new HttpClient())
            {
                // 设置请求体为 JSON 格式
                HttpContent content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                try
                {
                    // 发送 POST 请求
                    HttpResponseMessage response = await client.PostAsync(url, content);

                    // 确认响应状态
                    response.EnsureSuccessStatusCode();

                    // 读取响应内容
                    string responseBody = await response.Content.ReadAsStringAsync();
                    //Console.WriteLine("Received response: " + responseBody);

                    return responseBody;
                }
                catch (HttpRequestException e)
                {
                    Logger.Instance.Log(e);
                }
            }

            return "";
        }
    }
}
