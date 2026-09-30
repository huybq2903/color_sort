/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-10-29
 */

using System.Collections.Generic;
using Falcon.Modules.UI.Menu.Runtime;
using UnityEngine;

namespace Falcon.OutGame.ShortcutHome
{
    [CreateAssetMenu(fileName = "SO/ShortcutConfig", menuName = "SOShortcutConfig")]
    public class SOShortcutConfig : UIHomeShortcutsConfig
    {
        public int quantityMaxOneSide;
        public List<string> priorities;
    }
}