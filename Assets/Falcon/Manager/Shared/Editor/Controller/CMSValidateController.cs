/*
 * Author: ngocdx
 * Email: ngocdx@falcongames.com
 * Company: Falcon Games
 * Date: 2025-08-04
 */

namespace Falcon.Manager.Shared
{
	using System.Threading;
	using System.Threading.Tasks;
	using UnityEditor;
	using UnityEngine;

	public class CMSValidateController<T> : AViewController where T : ACMSValidatedController, new()
     {
	     private readonly CMSService              _cmsService;
	     private readonly Task<bool>              _task;
	     private readonly CancellationTokenSource _tokenSource;
	     
	     public CMSValidateController(CMSService cmsService)
	     {
		     _cmsService = cmsService;
		     _tokenSource = new CancellationTokenSource();
		     _task       = _cmsService.LoginAsync(_tokenSource.Token);
	     }
	     
	     public override void Edit(EditorWindow window)
	     {
		     GUILayout.Label("Validating...");

		     GUILayout.Space(5);
		     if (GUILayout.Button("Cancel", GUILayout.Height(20), GUILayout.Width(100)))
		     {
			     _tokenSource.Cancel();
		     }
	     }
	     
	     public override bool TryMoveNextController(out IViewController viewController)
	     {
		     viewController = null;
		     var next = Next (out var faulted);
		     if (!next)
		     {
			     return false;
		     }

		     if (faulted)
		     {
			     Debug.LogError("Exception on validating...");
			     Debug.LogError(_task.Exception);
			     AuthKeyRepository.DeleteKey();
			     viewController = new AuthController<T>();
			     return true;
		     }
		     
		     viewController = new T();
		     var controller = viewController as T;
		     controller.SetCMSService(_cmsService);
		     
		     AuthKeyRepository.Save(_cmsService.AuthKey);
		     
		     return true;
	     }
	     
	     private bool Next(out bool isFaulted)
	     {
		     isFaulted = false;
		     if (!_task.IsCompleted) return false;

		     if (_task.IsFaulted || _task.Exception != null || _task.Status != TaskStatus.RanToCompletion)
		     {
			     isFaulted = true;
		     }

		     if (_task.Result == false)
		     {
			     isFaulted = true;
		     }

		     return true;
	     }
     }
}