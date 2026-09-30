/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-11-28
 */

using System;
using Falcon.Helpers.Devkit;
using UnityEngine.Scripting;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    [Serializable]
    public class ExtendAdLog : FAdLog
    {
        [Preserve]
        public ExtendAdLog()
        {
        }

        public ExtendAdLog(AdParam param) : base(param)
        {
        }

        public ExtendAdLog(AdType type, string adWhere, string adPrecision, string adCountry, double adRev,
            string adNetwork, string adMediation, double timeShow, bool hasClick, int? currentLevel = null, FParam param = null)
        {
            LogParams(type, adWhere, adPrecision, adCountry, adRev, adNetwork, adMediation, timeShow, hasClick, currentLevel, param);
            if (param is AdParam adParam)
            {
                adParam.type = type;
                adParam.adWhere = adWhere;
                adParam.adPrecision = adPrecision;
                adParam.adCountry = adCountry;
                adParam.adRev = adRev;
                adParam.adNetwork = adNetwork;
                adParam.adMediation = adMediation;
                adParam.currentLevel = currentLevel;
                adParam.timeShow = timeShow;
                adParam.hasClick = hasClick;
                this.param = adParam;
            }
            else
            {
                this.param = new OldCodeSupportAdParam
                {
                    type = type,
                    adWhere = adWhere,
                    adPrecision = adPrecision,
                    adCountry = adCountry,
                    adRev = adRev,
                    adNetwork = adNetwork,
                    adMediation = adMediation,
                    currentLevel = currentLevel,
                    timeShow = timeShow,
                    hasClick = hasClick,
                };
            }

            this.param.CorrectValues();
        }
    }
}