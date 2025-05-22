using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Gmail.v1;
using Google.Apis.Services;
using System.Text.RegularExpressions;
using WebScrappingTrades.Models;

namespace WebScrappingTrades
{
    internal partial class GmailApi
    {
        /// <summary>
        /// Retrieves a verification code from the user's unread Gmail messages.
        /// </summary>
        /// <remarks>This method uses the Gmail API to access the user's inbox and searches for unread
        /// messages.  If an unread message is found, it extracts a verification code using a regular expression  and
        /// marks the message as read by removing the "UNREAD" label. If no unread messages are  available, the method
        /// waits briefly before returning an empty string.</remarks>
        /// <param name="userCredentials">The user's credentials, including the client ID, client secret, and refresh token,  required to authenticate
        /// with the Gmail API.</param>
        /// <returns>A string containing the verification code extracted from the first unread email,  or an empty string if no
        /// unread messages are found or if the credentials are invalid.</returns>
        internal static async Task<string> GetCodeFromMail(UserCredentials userCredentials)
        {
            string code = "";
            try
            {
                if (userCredentials.clientId == null || userCredentials.clientSecret == null || userCredentials.refreshToken == null)
                {
                    return "";
                }
                else
                {

                    string accessToken = await GetAccessTokenAsync(userCredentials.clientId, userCredentials.clientSecret, userCredentials.refreshToken);
                    var credential = GoogleCredential.FromAccessToken(accessToken);
                    var service = new GmailService(new BaseClientService.Initializer()
                    {
                        HttpClientInitializer = credential,
                        ApplicationName = "Gmail API .NET Quickstart",
                    });
                    UsersResource.MessagesResource.ListRequest request = service.Users.Messages.List("me");
                    request.Q = "is:unread";
                    IList<Message> messages = request.Execute().Messages;
                    if (messages != null && messages.Count > 0)
                    {
                        var message = service.Users.Messages.Get("me", messages[0].Id).Execute();
                        string emailContent = message.Snippet;
                        code = MyRegex().Match(emailContent).Value;
                        ModifyMessageRequest modifyRequest = new()
                        {
                            RemoveLabelIds = ["UNREAD"]
                        };
                        service.Users.Messages.Modify(modifyRequest, "me", message.Id).Execute();
                    }
                    else
                    {
                        Console.WriteLine("No unread messages found.");
                        await Task.Delay(1000);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ex1: {ex.Message}{Environment.NewLine}{ex}");
            }
            return code;
        }

        /// <summary>
        /// Creates a <see cref="Regex"/> instance that matches a six-digit number as a whole word.
        /// </summary>
        /// <remarks>The regular expression pattern used is <c>\b\d{6}\b</c>, which matches exactly six
        /// consecutive digits that are delimited by word boundaries. This ensures that the match is not part of a
        /// larger sequence of digits or characters.</remarks>
        /// <returns>A <see cref="Regex"/> object configured to match a six-digit number surrounded by word boundaries.</returns>
        [GeneratedRegex(@"\b\d{6}\b")]
        private static partial Regex MyRegex();


        /// <summary>
        /// Asynchronously retrieves an access token using the provided client credentials and refresh token.
        /// </summary>
        /// <remarks>This method sends a POST request to the Google OAuth 2.0 token endpoint to exchange a
        /// refresh token for a new access token. Ensure that the provided client credentials and refresh token are
        /// valid.</remarks>
        /// <param name="clientId">The client ID associated with the application requesting the token.</param>
        /// <param name="clientSecret">The client secret associated with the application requesting the token.</param>
        /// <param name="refreshToken">The refresh token used to obtain a new access token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the access token as a string.</returns>
        private static async Task<string> GetAccessTokenAsync(string clientId, string clientSecret, string refreshToken)
        {
            using var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, "https://oauth2.googleapis.com/token")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "client_id", clientId },
                    { "client_secret", clientSecret },
                    { "refresh_token", refreshToken },
                    { "grant_type", "refresh_token" }
                })
            };
            var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            var tokenResponse = System.Text.Json.JsonSerializer.Deserialize<MyTokenResponse>(content);
            return tokenResponse?.access_token;
        }
    }
}