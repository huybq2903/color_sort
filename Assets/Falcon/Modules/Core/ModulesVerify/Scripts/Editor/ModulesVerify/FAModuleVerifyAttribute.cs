/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 */
// 2025-06-24


using System;

namespace Falcon.Modules.Core.ModulesVerify.Editor
{
    public class FAModuleVerifyAttribute : Attribute
    {
        public string name;
        public FAModuleVerifyAttribute(string name) 
        {
            this.name = name;
        }
    }
}