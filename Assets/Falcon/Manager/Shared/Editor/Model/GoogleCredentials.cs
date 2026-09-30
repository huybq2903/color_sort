/*
 * Author: ngocdx
 * Email: ngocdx@falcongames.com
 * Company: Falcon Games
 * Date: 2026-03-11
 */

namespace Falcon.Manager.Shared
{
	using Newtonsoft.Json;

	public class GoogleCredentials
	{
		[JsonProperty("client_id")]
		public string ClientId { get; set; }

		[JsonProperty("client_secret")]
		public string ClientSecret { get; set; }

		[JsonProperty("redirect_uri")]
		public string RedirectUri { get; set; } = @"http://localhost";
	}
}
