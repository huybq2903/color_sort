/*
     * Author: ngocdx
     * Email: ngocdx@falcongames.com
     * Company: Falcon Games
     * Date: 2025-06-03
     */

namespace Falcon.Helpers.Singleton
{
	using UnityEngine;

	/// <summary>
	/// Persistent singleton, will destroy any other newer components of the same type it finds on awake
	/// </summary>
	public class PersistentSingleton<T> : MonoBehaviour where T : Component
	{
		[Header("Persistent Singleton")]
		[Tooltip("if this is true, this singleton will auto detach if it finds itself parented on awake")]
		public bool AutomaticallyUnparentOnAwake = true;

		public static bool HasInstance => _instance != null;
		public static T    Current     => _instance;

		protected static T    _instance;
		protected        bool _enabled;

		/// <summary>
		/// Singleton design pattern
		/// </summary>
		/// <value>The instance.</value>
		public static T Instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = FindObjectOfType<T>();
					if (_instance == null)
					{
						GameObject obj = new GameObject();
						obj.name  = typeof(T).Name + "_AutoCreated";
						_instance = obj.AddComponent<T>();
					}
				}

				return _instance;
			}
		}

		/// <summary>
		/// On awake, we check if there's already a copy of the object in the scene. If there's one, we destroy it.
		/// </summary>
		protected virtual void Awake()
		{
			if (!Application.isPlaying)
			{
				return;
			}

			if (AutomaticallyUnparentOnAwake)
			{
				this.transform.SetParent(null);
			}

			if (_instance == null)
			{
				//If I am the first instance, make me the Singleton
				_instance = this as T;
				DontDestroyOnLoad(transform.gameObject);
				_enabled = true;
			}
			else
			{
				//If a Singleton already exists and you find another reference in scene, destroy it!
				if (this != _instance)
				{
					Destroy(this.gameObject);
				}
				else
				{
					DontDestroyOnLoad(transform.gameObject);
				}
			}
		}
	}
}

