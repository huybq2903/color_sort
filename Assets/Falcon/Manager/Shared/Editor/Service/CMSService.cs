/*
     * Author: ngocdx
     * Email: ngocdx@falcongames.com
     * Company: Falcon Games
     * Date: 2025-08-04
     */


namespace Falcon.Manager.Shared
{
	using System;
	using System.Collections.Generic;
	using System.Threading;
	using System.Threading.Tasks;
	using Falcon.Helpers.Devkit;
	using Newtonsoft.Json;
	using Newtonsoft.Json.Linq;
	using UnityEngine;

	public class CMSService
	{
		private readonly GoogleLoginService _googleLoginService = new();

		private GoogleAuthKey _authKey;
		private string        _token;
		
		internal GoogleAuthKey AuthKey => _authKey;


		public CMSService(GoogleAuthKey authKey)
		{
			_authKey = authKey;
		}
		
		public async Task<bool> LoginAsync(CancellationToken ct = default)
		{
			if (!string.IsNullOrEmpty(_authKey.idToken))
			{
				Debug.Log("[Login] Auto Login");
				return await AuthenticateAsync(ct);
			}
			
			var credentials = _googleLoginService.LoadCredentials();
			if (credentials == null)
			{
				Debug.LogError($"[GoogleLogin] Failed to load credentials from {GoogleLoginService.CREDENTIALS_PATH}");
				return false;
			}

			var port        = 43688;
			var redirectUri = $"http://localhost:{port}/";
			var state       = Guid.NewGuid().ToString("N");

			var authCode = await _googleLoginService.GetAuthorizationCodeAsync(credentials, redirectUri, state, port, ct);
			if (string.IsNullOrEmpty(authCode))
			{
				Debug.LogError("[GoogleLogin] Failed to get authorization code");
				return false;
			}

			var tokenResponse = await _googleLoginService.ExchangeCodeForTokensAsync(credentials, authCode, redirectUri, ct);
			if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.IdToken))
			{
				Debug.LogError("[GoogleLogin] Failed to exchange code for tokens");
				return false;
			}

			_authKey = new GoogleAuthKey(tokenResponse.IdToken);
			
			Debug.Log("[GoogleLogin] Login successful");
			return await AuthenticateAsync(ct);
		}

		private async Task<bool> AuthenticateAsync(CancellationToken token)
		{
			string endpoint = "authenticate";
			var    url      = $"{Configuration.CMS_SEVER_URL}{endpoint}";
			
			var    post     = new PostRequest(url);
			post.SetJsonBody(_authKey);
			var res = await post.Execute();
			token.ThrowIfCancellationRequested();
			if (res.StatusCode == 200)
			{
				var     msg  = await res.SuccessStrBody();
				JObject json = JObject.Parse(msg);
				_token = json["data"].ToString();
				
				return string.IsNullOrEmpty(_token) == false;
			}
			
			return false;
		}

		public async Task<bool> Upload(IHttpBody body)
		{
			string endpoint = @"cdn/upload";
			var    url      = $"{Configuration.CMS_SEVER_URL}{endpoint}";
			var    post      = new PostRequest(url);
			post.AddAuthorization($"Bearer {_token}");
			
			post.SetBody(body);
                
			var res = await post.Execute();
			if (res.StatusCode == 200)
			{
				return true;
			}

			return false;
		}
		
		public async Task<bool> UploadConfig(string moduleName, IHttpBody body)
		{
			string endpoint = @"cdn/upload-module-config";
			var    url      = $"{Configuration.CMS_SEVER_URL}{endpoint}";
			var    post     = new PostRequest(url);
			post.AddAuthorization($"Bearer {_token}");
			
			post.AddParam("moduleName", moduleName);
			post.SetBody(body);
                
			var res = await post.Execute();
			if (res.StatusCode == 200)
			{
				return true;
			}

			return false;
		}

		public async Task<Dictionary<string, RegistryEntry>> GetUserModules()
		{
			string endpoint = @"user/get-list-module";
			var url =  $"{Configuration.CMS_SEVER_URL}{endpoint}";
			var get = new GetRequest(url);
			get.AddAuthorization($"Bearer {_token}");
			
			var res = await get.Execute();
			if (res.StatusCode == 200)
			{
				var     msg  = await res.SuccessStrBody();
				JObject json = JObject.Parse(msg);
				var jsonData = json["data"].ToString();
				
				var data = JsonConvert.DeserializeObject<Dictionary<string, RegistryEntry>>(jsonData);
				return data ?? new();
			}

			return new();
		}

		public async Task<Dictionary<string, RegistryEntry>> GetModules()
		{
			string endpoint = @"unity-module";
			var    url      =  $"{Configuration.CMS_SEVER_URL}{endpoint}";
			var    get      = new GetRequest(url);
			get.AddAuthorization($"Bearer {_token}");
			
			var res = await get.Execute();
			if (res.StatusCode == 200)
			{
				var     msg      = await res.SuccessStrBody();
				JObject json     = JObject.Parse(msg);
				var     jsonData = json["data"].ToString();
				
				var data = JsonConvert.DeserializeObject<Dictionary<string, RegistryEntry>>(jsonData);
				return data ?? new();
			}

			return new();
		}
	}
}
