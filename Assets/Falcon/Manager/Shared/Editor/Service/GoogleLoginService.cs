/*
 * Author: ngocdx
 * Email: ngocdx@falcongames.com
 * Company: Falcon Games
 * Date: 2026-03-11
 */

namespace Falcon.Manager.Shared
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Net;
	using System.Net.Http;
	using System.Text;
	using System.Threading;
	using System.Threading.Tasks;
	using Newtonsoft.Json;
	using UnityEngine;

	internal class GoogleLoginService
	{
		internal const string CREDENTIALS_PATH = @"Assets/Falcon/Manager/Shared/Editor/Config/google-credentials.json";
		
		private const string GOOGLE_AUTH_URL  = @"https://accounts.google.com/o/oauth2/v2/auth";
		private const string GOOGLE_TOKEN_URL = @"https://oauth2.googleapis.com/token";
		private const string SCOPE            = @"openid email profile";

		private static readonly HttpClient kHttpClient = new();

		internal GoogleCredentials LoadCredentials()
		{
			try
			{
				var fullPath = Path.GetFullPath(CREDENTIALS_PATH);
				if (!File.Exists(fullPath))
				{
					Debug.LogError($"[GoogleLogin] Credentials file not found: {fullPath}");
					return null;
				}

				var json = File.ReadAllText(fullPath);
				return JsonConvert.DeserializeObject<GoogleCredentials>(json);
			}
			catch (Exception e)
			{
				Debug.LogError($"[GoogleLogin] Error loading credentials: {e.Message}");
				return null;
			}
		}

		internal int GetAvailablePort()
		{
			var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
			listener.Start();
			var port = ((IPEndPoint)listener.LocalEndpoint).Port;
			listener.Stop();
			return port;
		}

		internal async Task<string> GetAuthorizationCodeAsync(
			GoogleCredentials credentials,
			string redirectUri,
			string state,
			int port,
			CancellationToken ct)
		{
			var authUrl = BuildAuthUrl(credentials, redirectUri, state);

			using var listener = new HttpListener();
			listener.Prefixes.Add($"http://localhost:{port}/");
			listener.Start();

			Application.OpenURL(authUrl);
			Debug.Log("[GoogleLogin] Opened browser for authentication...");

			try
			{
				var contextTask   = listener.GetContextAsync();
				var completedTask = await Task.WhenAny(contextTask, Task.Delay(TimeSpan.FromMinutes(5), ct));

				if (completedTask != contextTask)
				{
					Debug.LogError("[GoogleLogin] Authentication timed out");
					return null;
				}

				var context = await contextTask;

				var query         = context.Request.QueryString;
				var code          = query["code"];
				var returnedState = query["state"];

				if (returnedState != state)
				{
					SendResponse(context.Response, "Authentication failed: State mismatch", false);
					return null;
				}

				if (string.IsNullOrEmpty(code))
				{
					var error = query["error"] ?? "Unknown error";
					SendResponse(context.Response, $"Authentication failed: {error}", false);
					return null;
				}

				SendResponse(context.Response, "Authentication successful! You can close this window.", true);
				return code;
			}
			finally
			{
				listener.Stop();
			}
		}

		private string BuildAuthUrl(GoogleCredentials credentials, string redirectUri, string state)
		{
			var parameters = new Dictionary<string, string>
			{
				{ "client_id", credentials.ClientId },
				{ "redirect_uri", redirectUri },
				{ "response_type", "code" },
				{ "scope", SCOPE },
				{ "state", state },
				{ "access_type", "offline" },
				{ "prompt", "consent" }
			};

			var queryString = string.Join("&", System.Linq.Enumerable.Select(parameters, kvp => 
					$"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));

			return $"{GOOGLE_AUTH_URL}?{queryString}";
		}

		private void SendResponse(HttpListenerResponse response, string message, bool success)
		{
			var html = $@"
<!DOCTYPE html>
<html>
<head>
    <title>Falcon - Google Login</title>
    <style>
        body {{ font-family: Arial, sans-serif; display: flex; justify-content: center; align-items: center; height: 100vh; margin: 0; background: {(success ? "#e8f5e9" : "#ffebee")}; }}
        .container {{ text-align: center; padding: 40px; background: white; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        h1 {{ color: {(success ? "#4caf50" : "#f44336")}; }}
    </style>
</head>
<body>
    <div class='container'>
        <h1>{(success ? "Succeeded" : "Failed")}</h1>
        <p>{message}</p>
    </div>
</body>
</html>";

			var buffer = Encoding.UTF8.GetBytes(html);
			response.ContentType     = "text/html";
			response.ContentLength64 = buffer.Length;
			response.StatusCode      = success ? 200 : 400;
			response.OutputStream.Write(buffer, 0, buffer.Length);
			response.OutputStream.Close();
		}

		internal async Task<GoogleTokenResponse> ExchangeCodeForTokensAsync(
			GoogleCredentials credentials,
			string code,
			string redirectUri,
			CancellationToken ct)
		{
			try
			{
				var content = new FormUrlEncodedContent(new Dictionary<string, string>
				{
					{ "code", code },
					{ "client_id", credentials.ClientId },
					{ "client_secret", credentials.ClientSecret },
					{ "redirect_uri", redirectUri },
					{ "grant_type", "authorization_code" }
				});

				var response     = await kHttpClient.PostAsync(GOOGLE_TOKEN_URL, content, ct);
				var responseBody = await response.Content.ReadAsStringAsync();

				if (!response.IsSuccessStatusCode)
				{
					Debug.LogError($"[GoogleLogin] Token exchange failed: {responseBody}");
					return null;
				}

				return JsonConvert.DeserializeObject<GoogleTokenResponse>(responseBody);
			}
			catch (Exception e)
			{
				Debug.LogError($"[GoogleLogin] Token exchange error: {e.Message}");
				return null;
			}
		}
	}

}
