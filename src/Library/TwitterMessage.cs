using System.Security.Cryptography;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http;
using System.Linq;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;

namespace TwitterUCU
{
    public class TwitterMessage : TwitterApi
    {
        /// <summary>
        /// Twitter endpoint for sending tweets
        /// </summary>
        readonly string _TwitterMessageAPI;

        public TwitterMessage(int limit = 280)
        : base ()
        {
            this._TwitterMessageAPI = "https://api.twitter.com/1.1/direct_messages/events/new.json";
        }

        /// <summary>
        /// Publish a post with image
        /// </summary>
        /// <returns>result</returns>
        /// <param name="post">post to publish</param>
        /// <param name="pathToImage">image to attach</param>
        public string SendMessage(string message, string sendToUser)
        {
            try
            {
                var rezText = Task.Run(async () =>
                {
                    var response = await SendMessageAsync(CutTweetToLimit(message), sendToUser);
                    return response;
                });
                var rezTextJson = JObject.Parse(rezText.Result.Item2);

                if (rezText.Result.Item1 != 200)
                {
                    try // return error from JSON
                    {
                        return $"Error sending post to Twitter. {rezTextJson["errors"][0]["message"].Value<string>()}";
                    }
                    catch (Exception) // return unknown error
                    {
                        // log exception somewhere
                        return "Unknown error sending post to Twitter";
                    }
                }

                return "OK";
            }
            catch (Exception)
            {
                // log exception somewhere
                return "Unknown error publishing to Twitter";
            }
        }
        private async Task<Tuple<int, string>> SendMessageAsync(string text, string sendToUser)
        {
            var textData = new Dictionary<string, string> {
                    { "text", text},
                    { "recipient_id", sendToUser }
                };
            using (var httpClient = new HttpClient())
            {
                try
                {
                    string contentString = "{\"event\": {\"type\": \"message_create\", \"message_create\": {\"target\": {\"recipient_id\": \""+sendToUser+
                    "\"}, \"message_data\": {\"text\": \""+text+"\"}}}}";

                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    httpClient.DefaultRequestHeaders.Add("Authorization", base.PrepareOAuth(_TwitterMessageAPI, null));
                    httpClient.BaseAddress = new Uri(_TwitterMessageAPI);
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "");
                    request.Content = new StringContent(contentString,
                                    Encoding.UTF8,
                                    "application/json");
                    var httpResponse = await httpClient.SendAsync(request);
                    var httpContent = await httpResponse.Content.ReadAsStringAsync();
                    return new Tuple<int, string>(
                        (int)httpResponse.StatusCode,
                        httpContent
                        );
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    return new Tuple<int, string>(
                        -1,
                        ex.Message
                        );
                }
            }
        }
    }
}