/*
 * Author: ngocdx
 * Email: ngocdx@falcongames.com
 * Company: Falcon Games
 * Date: 2026-03-11
 */

namespace Falcon.Manager.Shared
{
	using System;
	using Newtonsoft.Json;

	[Serializable]
	public class GoogleTokenResponse
	{
		[JsonProperty("access_token")]
		public string AccessToken { get; set; }

		[JsonProperty("id_token")]
		public string IdToken { get; set; }

		[JsonProperty("refresh_token")]
		public string RefreshToken { get; set; }

		[JsonProperty("expires_in")]
		public int ExpiresIn { get; set; }

		[JsonProperty("token_type")]
		public string TokenType { get; set; }

		[JsonProperty("scope")]
		public string Scope { get; set; }
	}
}
