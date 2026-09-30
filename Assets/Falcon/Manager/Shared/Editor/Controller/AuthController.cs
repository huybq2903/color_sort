/*
     * Author: ngocdx
     * Email: ngocdx@falcongames.com
     * Company: Falcon Games
     * Date: 2025-07-30
     */


namespace Falcon.Manager.Shared
{
	using System;
	using UnityEditor;
	using UnityEngine;

	public class AuthController<T> : AViewController where T : ACMSValidatedController, new()
	{
		private CMSService _cmsService;
		
		public override void Edit(EditorWindow window)
		{
			if (AuthKeyRepository.TryGetKey(out var key))
			{
				try
				{
					_cmsService = new CMSService(key);
					return;
				}
				catch (Exception e)
				{
					Debug.LogException(e);
					AuthKeyRepository.DeleteKey();
					return;
				}
			}
			GUILayout.Space(20);
			GUIVertical(() =>
			{
				GUILayout.Space(10);
				
				GUILayout.Label("Authentication: ");
				GUILayout.Space(5);
				if (!GUILayout.Button("Google Login", GUILayout.Height(40))) return;
				var authKey = new GoogleAuthKey(string.Empty);
				_cmsService = new CMSService(authKey);
			});
		}
		
		public override bool TryMoveNextController(out IViewController viewController)
		{
			if (_cmsService != null)
			{
				viewController = new CMSValidateController<T>(_cmsService);
				return true;
			}
			
			viewController = null;
			return false;
		}
	}
}