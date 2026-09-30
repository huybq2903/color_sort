/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-04
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Falcon.Modules.Core.AccountData 
{
    public abstract class FSingleton<T> where T : class, new()
    {
        private static T _instance;
        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new T();
                }
                return _instance;
            }
        }
    
        protected FSingleton()
        {
        }
    }
}

