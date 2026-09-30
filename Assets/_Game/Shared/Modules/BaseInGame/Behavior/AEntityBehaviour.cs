/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-12-17
 */

using Sirenix.OdinInspector;
using UnityEngine;

namespace Falcon.Shared.BaseInGame
{
    public abstract class AEntityBehaviour<D, R> : MonoBehaviour, IContainerEntityRuntime<R> , IContainerEntityData<D> 
        where R : EntityRuntime, new()
        where D : EntityData
    {
        public EntityRuntime BaseRuntime => Runtime;
        public EntityData BaseData => Data;
        [ShowInInspector, ReadOnly] public R Runtime { get; set; }
        [ShowInInspector, ReadOnly] public D Data { get; protected set; }
        
        public virtual void Setup(D data)
        {
            Data = data;
            Runtime = new R {id = data != null ? data.id : ""};
        }
        
        public virtual void Setup(R runtime)
        {
            Runtime = runtime;
        }
        
        public virtual void Setup(D data, R runtime)
        {
            Data = data;
            Runtime = runtime ?? new R {id = data != null ? data.id : ""};
        }
    }
    
    public interface IContainerEntityData<out E> : IEntityDataContainer where E : EntityData
    {
        E Data { get; }
    }

    public interface IEntityDataContainer
    {
        EntityData BaseData { get; }
    }

    public interface IContainerEntityRuntime<out ER> : IEntityRuntimeContainer where ER : EntityRuntime
    {
        ER Runtime { get; }
    }

    public interface IEntityRuntimeContainer
    {
        EntityRuntime BaseRuntime { get; }
    }
}