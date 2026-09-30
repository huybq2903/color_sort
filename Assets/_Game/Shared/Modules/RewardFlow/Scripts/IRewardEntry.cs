/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2026-02-06
 */

using System;
using UnityEngine;

namespace Falcon.Shared.RewardFlow
{
    public interface IRewardEntry
    {
        void Init();
        void Open(); 
        GameObject GameObject { get; }
        Action OnDispose { get; set; } //cái này gọi khi muốn hủy entry đi khi xong
        Action OnNext { get; set; } //cái này gọi khi muốn chuyển sang entry tiếp theo
    }
    public abstract class RewardEntry<D> : MonoBehaviour, IRewardEntry where D : IRewardEntryData
    {
        public D Data { get; set; }
        public virtual void Init() {}
        public virtual void Open() {}
        public GameObject GameObject => gameObject;
        public Action OnDispose { get; set; }
        public Action OnNext { get; set; }
    }
    
    public interface IRewardEntryData {}
}