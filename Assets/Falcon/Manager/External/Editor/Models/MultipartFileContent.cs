/*
     * Author: ngocdx
     * Email: ngocdx@falcongames.com
     * Company: Falcon Games
     * Date: 2025-08-04
     */


namespace Falcon.Manager.External
{
	using System.Net.Http;
	using Falcon.Helpers.Devkit;

	public class MultipartFileContent : IHttpBody
	{
		MultipartFormDataContent _content;
        
		public MultipartFileContent(MultipartFormDataContent content)
		{
			_content = content;
		}
        
		public HttpContent ToContent()
		{
			return _content;
		}
	}
}