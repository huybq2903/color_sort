/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 */
// 2025-06-08

using System.Collections;
#if UNITY_EDITOR
using NUnit.Framework;
#endif
using UnityEngine;
using UnityEngine.TestTools;

namespace Falcon.Modules.Core.AccountData
{

    public class MainTest
    {

        [UnityTest]
        public IEnumerator Init()
        {
            AccountManager.Instance.Init();
            yield return new WaitForSeconds(300);
        }

        
        public CSLogin CreateCSLogin()
        {
            return new CSLogin(1000001, "cc5cf5ec-312f-4547-a3cd-52a9f94b5483", "49A1D962-F711-51BF-9AED-2ED5C34C1252-editor", 1, 1, "editor", "", "", "");
        }

        
    }
}