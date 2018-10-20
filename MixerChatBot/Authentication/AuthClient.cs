using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MixerChatBot.Authentication
{
    /// <summary>
    /// Class to simplest OAuth short code login for a console app.
    /// </summary>
    class AuthClient : HttpClient
    {
        private static readonly string shortCodeUri = "https://mixer.com/api/v1/oauth/shortcode";
        private static readonly string authCodeUri = "https://mixer.com/api/v1/oauth/shortcode/check/{0}";
        private static readonly string authTokenUri = "https://mixer.com/api/v1/oauth/token";
        private static readonly string chatAuthKey = "https://mixer.com/api/v1/chats/{0}";
        private static readonly string authCodeType = "authorization_code";
        private string clientId;
        private string clientSecret;
        private HttpClient webClient;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <see cref="https://mixer.com/lab/oauth"/>
        /// <param name="clientId">The OAuth client id.</param>
        /// <param name="clientSecret">The OAuth client secret, only required if the OAuth client in use has one.</param>
        public AuthClient(string clientId, string clientSecret)
            : base(CreateWebRequestHandler(), true)
        {
            this.clientId = clientId;
            this.clientSecret = clientSecret;
            this.webClient = new HttpClient();
        }

        /// <summary>
        /// Runs the full OAuth login flow for a console app.
        /// </summary>
        /// <param name="scopes">List of permissions separated by whitespace.</param>
        /// <returns>The access and refresh tokens.</returns>
        public async Task<Contracts.TokenResponse> RunOauthCodeFlowForConsoleAppAsync(string scopes)
        {
            Contracts.ShortCodeResponse shortCode = await this.RequestShortcodeAsync(scopes);

            Console.WriteLine("Go to http://mixer.com/go and enter code " + shortCode.code);

            Contracts.CodeResponse authCode = await this.RequestAuthCodeAsync(shortCode.handle);
            while (authCode == null)
            {
                await Task.Delay(500);
                authCode = await this.RequestAuthCodeAsync(shortCode.handle);
            }

            return await this.ExchangeCodeForTokenAsync(authCode.code);
        }

        /// <summary>
        /// Initializes the "shortcode" auth flow. The endpoint takes a client_id, client_secret, and scope similarly to the initial authorization request. It returns a short, six-digit code to be displayed to the user to link their account in addition to a longer "handle" that can be used for polling its status in POST /oauth/code/{handle}.
        /// </summary>
        /// <see cref="https://dev.mixer.com/rest/index.html#oauth_shortcode_post"/>
        /// <param name="scopes">List of permissions separated by whitespace.</param>
        /// <returns>Code for the user and handle for the app.</returns>
        public async Task<Contracts.ShortCodeResponse> RequestShortcodeAsync(string scopes)
        {
            var body = new Contracts.ShortCodeRequest
            {
                client_id = this.clientId,
                client_secret = this.clientSecret,
                scope = scopes,
            };

            using (var request = new HttpRequestMessage(HttpMethod.Post, shortCodeUri))
            {
                request.Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
                using (var response = await this.SendAsync(request))
                {
                    return await this.GetResponseAsync<Contracts.ShortCodeResponse>(response);
                }
            }
        }

        /// <summary>
        /// Checks the status of a previously issued code and, after it's been activated, returns an OAuth authorization code to be passed to GET /oauth/token to exchange it for an access and refresh token. Once again, the client ID and secret (if any) are required.
        /// </summary>
        /// <see cref="https://dev.mixer.com/rest/index.html#oauth_shortcode_check__handle__get"/>
        /// <param name="handle">The code handle as returned in POST /oauth/code</param>
        /// <returns>Null if the code is still valid but the user hasn't granted it yet, or OAuth code to exchange for access and refresh tokens.</returns>
        public async Task<Contracts.CodeResponse> RequestAuthCodeAsync(string handle)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, string.Format(CultureInfo.InvariantCulture, authCodeUri, handle)))
            {
                using (var response = await this.SendAsync(request))
                {
                    return await this.GetResponseAsync<Contracts.CodeResponse>(response);
                }
            }
        }

        /// <summary>
        /// Retrieves an OAuth token.
        /// </summary>
        /// <see cref="https://dev.mixer.com/rest/index.html#oauth_token_post"/>
        /// <param name="code">The authorization code received from the /authorize endpoint.</param>
        /// <returns>The access and refresh tokens.</returns>
        public async Task<Contracts.TokenResponse> ExchangeCodeForTokenAsync(string code)
        {
            var body = new Contracts.TokenRequest
            {
                code = code,
                client_id = this.clientId,
                client_secret = this.clientSecret,
                grant_type = authCodeType,
            };

            using (var request = new HttpRequestMessage(HttpMethod.Post, authTokenUri))
            {
                request.Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
                using (var response = await this.SendAsync(request))
                {
                    return await this.GetResponseAsync<Contracts.TokenResponse>(response);
                }
            }
        }

        /// <summary>
        /// Prepares the user to join a chat channel. It returns the channel's chatroom settings, available chat servers, and an authentication key that the user (if authenticated) should use to authenticate with the chat servers over websockets.
        /// </summary>
        /// <see cref="https://dev.mixer.com/rest/index.html#chats__channelId__get"/>
        /// <param name="authToken">OAuth token info.</param>
        /// <param name="channelId">The id of the channel to join chat for.</param>
        /// <returns>Chat connection info and auth key.</returns>
        public async Task<Contracts.ChatConnectionAuthentication> RequestChatAuthKey(Contracts.TokenResponse authToken, uint channelId)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, string.Format(CultureInfo.InvariantCulture, chatAuthKey, channelId)))
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(authToken.token_type, authToken.access_token);
                using (var response = await this.SendAsync(request))
                {
                    return await this.GetResponseAsync<Contracts.ChatConnectionAuthentication>(response);
                }
            }
        }

        /// <summary>
        /// Helper method to set the handler settings for this httpclient
        /// </summary>
        /// <returns>The HttpMessageHandler</returns>
        private static HttpMessageHandler CreateWebRequestHandler()
        {
            HttpClientHandler requestHandler = new HttpClientHandler
            {
                CookieContainer = new CookieContainer(),
                UseCookies = true,
            };

            return requestHandler;
        }

        /// <summary>
        /// Handles a web response and deserializes the response body.
        /// </summary>
        /// <typeparam name="T">The object type to deserialize into.</typeparam>
        /// <param name="response">The completed HTTP response.</param>
        /// <returns>A deserialized object from the response.</returns>
        protected async Task<T> GetResponseAsync<T>(HttpResponseMessage response)
            where T : class
        {
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new WebException(error);
            }

            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();

            var contract = JsonConvert.DeserializeObject<T>(content);

            return contract;
        }
    }
}
