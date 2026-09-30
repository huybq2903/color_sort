/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-10-06
 */

using System.Collections.Generic;
using CodeStage.AntiCheat.ObscuredTypes;
using Falcon.Modules.Core.AccountData;
using Falcon.Modules.Core.GameData.Runtime;
using UnityEngine;

namespace Game.Shared.Profile
{
    [FGameDataType("profile")]
    public class FGameDataProfile : FGameData<FGameDataProfile>
    {
        public int avatarId = 0;
        public int frameId = 0;
        public int badgeId = 0;
        public int styleNameId = 0;
        public string playerName = $"Player_{Random.Range(1000, 9999)}";
        public string avatarUrl = "";
        
        public string createDate;
        public string countryCode;
        public Dictionary<string, int> stats = new();

        public AvatarResource avatarResource = new();
        public FrameResource frameResource = new();
    }

    [ResourceInfo("avatar")]
    public class AvatarResource : ProfileResource { }
    
    [ResourceInfo("frame")]
    public class FrameResource : ProfileResource { }
    
    public abstract class ProfileResource : AResource
    {
        public List<ObscuredInt> unlocks = new();
        protected override bool AddInternal(int id, string data)
        {
            if (!unlocks.Contains(id)) unlocks.Add(id);
            return true;
        }

        protected override bool RemoveInternal(int id, string data)
        {
            if (unlocks.Contains(id)) unlocks.Remove(id);
            return true;
        }

        protected override int SetInternal(int value, string data)
        {
            if (!unlocks.Contains(value)) unlocks.Add(value);
            return 1;
        }

        protected override int ResetInternal()
        {
            int before = unlocks.Count;
            unlocks = new List<ObscuredInt>();
            return before;
        }

        public override object Get => unlocks;
    }
}