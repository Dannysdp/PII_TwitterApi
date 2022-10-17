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
    public class TwitterImage : TwitterApi
    {
        /// <summary>
        /// Twitter endpoint for sending tweets
        /// </summary>
        readonly string _TwitterTextAPI;
        /// <summary>
        /// Twitter endpoint for uploading images
        /// </summary>
        readonly string _TwitterImageAPI;
        /// <summary>
        /// Current tweet limit
        /// </summary>
        //readonly int _limit;

        public TwitterImage() : base ()
        {
            _TwitterTextAPI = "https://api.twitter.com/1.1/statuses/update.json";
            _TwitterImageAPI = "https://upload.twitter.com/1.1/media/upload.json";
        }

        /// <summary>
        /// Publish a post with image
        /// </summary>
        /// <returns>result</returns>
        /// <param name="post">post to publish</param>
        /// <param name="pathToImage">image to attach</param>
        public string PublishToTwitter(string post, string pathToImage)
        {
            try
            {
                // first, upload the image
                string mediaID = string.Empty;
                var rezImage = Task.Run(async () =>
                {
                    var response = await TweetImage(pathToImage);
                    return response;
                });
                var rezImageJson = JObject.Parse(rezImage.Result.Item2);

                if (rezImage.Result.Item1 != 200)
                {
                    try // return error from JSON
                    {
                        return $"Error uploading image to Twitter. {rezImageJson["errors"][0]["message"].Value<string>()}";
                    }
                    catch (Exception) // return unknown error
                    {
                        // log exception somewhere
                        return "Unknown error uploading image to Twitter";
                    }
                }
                mediaID = rezImageJson["media_id_string"].Value<string>();

                // second, send the text with the uploaded image
                var rezText = Task.Run(async () =>
                {
                    var response = await TweetText(CutTweetToLimit(post), mediaID);
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
            catch (Exception ex)
            {
                // log exception somewhere
                return "Unknown error publishing to Twitter";
            }
        }

        /// <summary>
        /// Send a tweet with some image attached
        /// </summary>
        /// <returns>HTTP StatusCode and response</returns>
        /// <param name="text">Text</param>
        /// <param name="mediaID">Media ID for the uploaded image. Pass empty string, if you want to send just text</param>
        private Task<Tuple<int, string>> TweetText(string text, string mediaID)
        {
            var textData = new Dictionary<string, string> {
                    { "status", text },
                    { "trim_user", "1" },
                    { "media_ids", mediaID}
                };

            return SendText(_TwitterTextAPI, textData);
        }

        /// <summary>
        /// Upload some image to Twitter
        /// </summary>
        /// <returns>HTTP StatusCode and response</returns>
        /// <param name="pathToImage">Path to the image to send</param>
        private Task<Tuple<int, string>> TweetImage(string pathToImage)
        {
            byte[] imgdata = System.IO.File.ReadAllBytes(pathToImage);
            var imageContent = new ByteArrayContent(imgdata);
            imageContent.Headers.ContentType = new MediaTypeHeaderValue("multipart/form-data");

            var multipartContent = new MultipartFormDataContent();
            multipartContent.Add(imageContent, "media");

            return SendImage(_TwitterImageAPI, multipartContent);
        }

        async Task<Tuple<int, string>> SendText(string URL, Dictionary<string, string> textData)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", PrepareOAuth(URL, textData));

                var httpResponse = await httpClient.PostAsync(URL, new FormUrlEncodedContent(textData));
                var httpContent = await httpResponse.Content.ReadAsStringAsync();

                return new Tuple<int, string>(
                    (int)httpResponse.StatusCode,
                    httpContent
                    );
            }
        }

        async Task<Tuple<int, string>> SendImage(string URL, MultipartFormDataContent multipartContent)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", PrepareOAuth(URL, null));

                var httpResponse = await httpClient.PostAsync(URL, multipartContent);
                var httpContent = await httpResponse.Content.ReadAsStringAsync();

                return new Tuple<int, string>(
                    (int)httpResponse.StatusCode,
                    httpContent
                    );
            }
        }
    }
}