using UnityEngine;

using Falcon.Shared.Common;
namespace Game.Shared.Clan
{
    public class ClanEditingData
    {
        public int avatar_id;
        public string name;
        public string description;
        public int required_level;
        public bool open;

        public ClanEditingData()
        {
            avatar_id = 0;
            name = "";
            description = "";
            required_level = Center.GetOrCreate<ClanService>().LevelUnlock;
            open = true;
        }

        public ClanEditingData(ClanData data)
        {
            avatar_id = data.avatar_id;
            name = data.name;
            description = data.description;
            required_level = data.required_level;
            open = data.open;
        }
    }
}
