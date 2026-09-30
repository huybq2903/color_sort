/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-12-19
 */

using Falcon.Shared.BaseInGame;
using Falcon.Shared.Common;
using Sirenix.OdinInspector;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Falcon.Shared.BaseLevelEditor
{
    public interface IEditorManager
    {
        public void Initialized() { }
    }

    public interface IPropertyDataHandler : IEditorManager {}
    
    public abstract class APropertyDataHandler<T> : MonoBehaviour, IPropertyDataHandler where T : PropertyData
    {
        [ShowInInspector, ReadOnly] protected T _propertyData;

        private void OnEnable()
        {
            Messenger<OnLoadLevel>.Register(OnLoadLevel);
        }

        private void OnDisable()
        {
            Messenger<OnLoadLevel>.Unregister(OnLoadLevel);
        }

        protected virtual void OnLoadLevel(OnLoadLevel data)
        {
            _propertyData = data.levelData.GetProperty<T>();
        }

        protected virtual void SavePropertyData()
        {
            Messenger<OnSavePropertyData>.Emit(new OnSavePropertyData
            {
                propertyData = _propertyData
            });
        }
    }
}