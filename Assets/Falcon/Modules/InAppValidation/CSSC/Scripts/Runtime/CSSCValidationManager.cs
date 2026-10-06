/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-18
 */


using Falcon.Modules.Core.AccountData;
using Falcon.Modules.Core.InAppPurchase.Runtime;
using UnityEngine;

namespace Falcon.Modules.InAppValidation.CSSC.Runtime
{
    public class CSSCValidationManager : IPurchaseValidation, IPurchaseLog
    {
        private IStoreCSSCValidationHandler _validationHandler;
        public void Initialized()
        {
#if UNITY_EDITOR
            _validationHandler = new FakeStoreCSSCValidationHandler();
#elif UNITY_ANDROID
            _validationHandler = new GoogleCSSCValidationHandler();
#elif UNITY_IOS
            _validationHandler = new AppleCSSCValidationHadler();
#else
            _validationHandler = new FakeStoreCSSCValidationHandler();
#endif
        }

        public void SendValidate(APurchaseProcess purchaseProcess)
        {
            _validationHandler.SendValidate(purchaseProcess);
        }

        public void Log(APurchaseProcess purchaseProcess)
        {
            if (_validationHandler == null)
                Initialized();
            _validationHandler?.Log(purchaseProcess);
        }

        [RuntimeInitializeOnLoadMethod]
        private static void OnLogin()
        {
            AccountManager.Instance.OnLoginEvent += success =>
            {
                if (success) new CSGetLtvIap().Send();
            };
        }
    }
}