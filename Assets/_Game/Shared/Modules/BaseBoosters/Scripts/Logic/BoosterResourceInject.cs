/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2026-07-29
 */

using System;
using System.Linq;
using Falcon.Helpers.FReflection;
using Falcon.Modules.Core.GameData.Runtime;

namespace Falcon.Shared.BaseBooster
{
    /// <summary>Đưa mọi ABoosterResource vào GameDataCore để save/load và cộng lượt qua id.</summary>
    public class BoosterResourceInject : AResourceInject
    {
        public override void Inject(Injection injection)
        {
            var types = FReflection.Instance.GetTypes()
                .Where(t => typeof(ABoosterResource).IsAssignableFrom(t) && !t.IsAbstract);

            foreach (var t in types)
            {
                var resource = (ABoosterResource)Activator.CreateInstance(t);
                resource.ResetDefault();
                injection.Invoke(resource);
            }
        }
    }
}
