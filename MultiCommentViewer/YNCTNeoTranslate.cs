using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
namespace MultiCommentViewer.YNCNeo
{

    public class Response
    {
        public bool IsError { get; set; }
        public string ErrorMessage { get; set; }
        public string Translated { get; set; }
    }
    public class YNCTNeoTranslate
    {        
        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
        private static readonly Random NonceRandom = new Random();

        private static int yncNeoPort = 8080;

        public YNCTNeoTranslate()
        {

            try
            {
                string USERREGIST_DATA = @"Software\YukarinetteConnectorNeo\TransServer\";

                using (RegistryKey regkey1 = Registry.CurrentUser.CreateSubKey(USERREGIST_DATA))
                {
                    yncNeoPort = (int)regkey1.GetValue(@"HTTP", 8080);
                }
            }
            catch
            {

            }
        }
        public async Task<Response> Traslate(string text)
        {

            //URLを作る
            String URL = $"http://127.0.0.1:{yncNeoPort}/";

            Dictionary<string,object> tranData = new Dictionary<string, object>();
            tranData["text"] = (String)text;
            tranData["lang"] = (String)"ja";
            string jsonText = Newtonsoft.Json.JsonConvert.SerializeObject(tranData);
            string decodeText = "";

            string m = "（翻訳サービスがメンテナンス中のようです）";
            Response parsed = new Response
            {
                IsError = true,
                ErrorMessage = m,
            };

            try
            {
                using (var request = new HttpRequestMessage())
                using (var client = new HttpClient())
                {
                    request.Properties["RequestTimeout"] = new TimeSpan(0, 0, 12);

                    request.Method = HttpMethod.Post;
                    request.RequestUri = new Uri(URL);
                    request.Content = new StringContent(jsonText, Encoding.UTF8, "application/json");

                    var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                    var jsonResponse = await response.Content.ReadAsStringAsync();

                    try
                    {
                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            JObject json0 = Newtonsoft.Json.JsonConvert.DeserializeObject<JObject>(jsonResponse);

                            string stat = (string)json0["status"];

                            if (stat == "success")
                            {
                                parsed = new Response
                                {
                                    IsError = false,
                                    Translated = json0["text"].ToString(),
                                };
                            }
                        }

                    }
                    catch (Exception ex)
                    {

                    }
                    finally
                    {
                        response.Dispose();
                    }

                };

            }
            catch (JsonReaderException ex)
            {
                parsed = new Response
                {
                    IsError = true,
                    ErrorMessage = ex.Message,
                };
            }
            catch (Exception ex)
            {
                parsed = new Response
                {
                    IsError = true,
                    ErrorMessage = ex.Message,
                };
            }
            return parsed;
        }                
    }
}
