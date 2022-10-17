using System.Security.Cryptography;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http;
using System.Linq;
using System.Text.Json;
using Nito.AsyncEx;

namespace TwitterUCU
{
	/// <summary>
	/// Credit to Danny Tuppeny: https://blog.dantup.com/2016/07/simplest-csharp-code-to-post-a-tweet-using-oauth/
	/// Simple class for sending tweets to Twitter using Single-user OAuth
	/// https://dev.twitter.com/oauth/overview/single-user
	/// </summary>
	public abstract class TwitterApi
	{
		private static bool initialized = false;
		const string TwitterApiBaseUrl = "https://api.twitter.com/1.1/";
		private static string consumerKey, consumerKeySecret, accessToken, accessTokenSecret;
		private static HMACSHA1 sigHasher;
		readonly DateTime epochUtc = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		readonly int limit;

		/// <summary>
		/// Inicializa una nueva instancia de TwitterApi.
		/// </summary>
		/// <param name="limit">El tamaño máximo de los mensajes a publicar.</param>
		public TwitterApi(int limit = 280)
		{
			this.limit = limit;
			if (!initialized)
			{
				AsyncContext.Run(InitializeAsync);
				initialized = true;
			}
		}

		private static async Task InitializeAsync()
		{
			HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync(@"https://pii-secretsapiwebapp.azurewebsites.net/TwitterApiSecrets");
            string result = await response.Content.ReadAsStringAsync();
            TwitterApiSecrets secrets = JsonSerializer.Deserialize<TwitterApiSecrets>(result,
                new JsonSerializerOptions { PropertyNamingPolicy  = JsonNamingPolicy.CamelCase });

			consumerKey = secrets.ConsumerKey;
			consumerKeySecret = secrets.ConsumerKeySecret;
			accessToken = secrets.AccessToken;
			accessTokenSecret = secrets.AccessTokenSecret;

			sigHasher = new HMACSHA1(new ASCIIEncoding().GetBytes(string.Format("{0}&{1}", consumerKeySecret, accessTokenSecret)));
		}

		/// <summary>
		/// Sends a tweet with the supplied text and returns the response from the Twitter API.
		/// </summary>
		public Task<string> Tweet(string text)
		{
			var data = new Dictionary<string, string> {
				{ "status", text },
				{ "trim_user", "1" }
			};

			return SendRequest("statuses/update.json", data);
		}

		async Task<string> SendRequest(string url, Dictionary<string, string> data)
		{
			var fullUrl = TwitterApiBaseUrl + url;

			// Timestamps are in seconds since 1/1/1970.
			var timestamp = (int)((DateTime.UtcNow - epochUtc).TotalSeconds);

			// Add all the OAuth headers we'll need to use when constructing the hash.
			data.Add("oauth_consumer_key", consumerKey);
			data.Add("oauth_signature_method", "HMAC-SHA1");
			data.Add("oauth_timestamp", timestamp.ToString());
			data.Add("oauth_nonce", "a"); // Required, but Twitter doesn't appear to use it, so "a" will do.
			data.Add("oauth_token", accessToken);
			data.Add("oauth_version", "1.0");

			// Generate the OAuth signature and add it to our payload.
			data.Add("oauth_signature", GenerateSignature(fullUrl, data));

			// Build the OAuth HTTP Header from the data.
			string oAuthHeader = GenerateOAuthHeader(data);

			// Build the form data (exclude OAuth stuff that's already in the header).
			var formData = new FormUrlEncodedContent(data.Where(kvp => !kvp.Key.StartsWith("oauth_")));

			return await SendRequest(fullUrl, oAuthHeader, formData);
		}

		/// <summary>
		/// Send HTTP Request and return the response.
		/// </summary>
		async Task<string> SendRequest(string fullUrl, string oAuthHeader, FormUrlEncodedContent formData)
		{
			using (var http = new HttpClient())
			{
				http.DefaultRequestHeaders.Add("Authorization", oAuthHeader);

				var httpResp = await http.PostAsync(fullUrl, formData);
				var respBody = await httpResp.Content.ReadAsStringAsync();

				return respBody;
			}
		}

		internal string PrepareOAuth(string URL, Dictionary<string, string> data)
    	{
			// seconds passed since 1/1/1970
			var timestamp = (int)((DateTime.UtcNow - epochUtc).TotalSeconds);

			// Add all the OAuth headers we'll need to use when constructing the hash
			Dictionary<string, string> oAuthData = new Dictionary<string, string>();
			oAuthData.Add("oauth_consumer_key", consumerKey);
			oAuthData.Add("oauth_signature_method", "HMAC-SHA1");
			oAuthData.Add("oauth_timestamp", timestamp.ToString());
			oAuthData.Add("oauth_nonce", Guid.NewGuid().ToString());
			oAuthData.Add("oauth_token", accessToken);
			oAuthData.Add("oauth_version", "1.0");

			if (data != null) // add text data too, because it is a part of the signature
			{
				foreach (var item in data)
				{
					oAuthData.Add(item.Key, item.Value);
				}
			}

			// Generate the OAuth signature and add it to our payload
			oAuthData.Add("oauth_signature", GenerateSignature(URL, oAuthData));

			// Build the OAuth HTTP Header from the data
			return GenerateOAuthHeader(oAuthData);
    	}

		/// <summary>
		/// Generate an OAuth signature from OAuth header values
		/// </summary>
		internal string GenerateSignature(string url, Dictionary<string, string> data)
		{
			var sigString = string.Join(
				"&",
				data
					.Union(data)
					.Select(kvp => string.Format("{0}={1}", Uri.EscapeDataString(kvp.Key), Uri.EscapeDataString(kvp.Value)))
					.OrderBy(s => s)
			);

			var fullSigData = string.Format("{0}&{1}&{2}",
				"POST",
				Uri.EscapeDataString(url),
				Uri.EscapeDataString(sigString.ToString()
				)
			);

			return Convert.ToBase64String(
				sigHasher.ComputeHash(
					new ASCIIEncoding().GetBytes(fullSigData.ToString())
				)
			);
		}

		/// <summary>
		/// Generate the raw OAuth HTML header from the values (including signature)
		/// </summary>
		internal string GenerateOAuthHeader(Dictionary<string, string> data)
		{
			return string.Format(
				"OAuth {0}",
				string.Join(
					", ",
					data
						.Where(kvp => kvp.Key.StartsWith("oauth_"))
						.Select(
							kvp => string.Format("{0}=\"{1}\"",
							Uri.EscapeDataString(kvp.Key),
							Uri.EscapeDataString(kvp.Value)
							)
						).OrderBy(s => s)
					)
				);
		}

		/// <summary>
		/// Cuts the tweet text to fit the limit.
		/// </summary>
		/// <returns>Cutted tweet text</returns>
		/// <param name="tweet">Uncutted tweet text</param>
		internal string CutTweetToLimit(string tweet)
		{
			while (tweet.Length >= limit)
			{
				tweet = tweet.Substring(0, tweet.LastIndexOf(" ", StringComparison.Ordinal));
			}
			return tweet;
		}
	}
}