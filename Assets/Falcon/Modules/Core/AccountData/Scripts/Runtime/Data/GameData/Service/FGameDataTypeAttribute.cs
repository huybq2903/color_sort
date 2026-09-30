/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 */
// 2025-06-09


using System;

namespace Falcon.Modules.Core.AccountData 
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class FGameDataTypeAttribute : Attribute
    {
        public string TypeName { get; }
        public bool Skip { get; }
        public FGameDataTypeAttribute(string typeName, bool skip = false)
        {
            TypeName = typeName;
            Skip = skip;
        }
    }
}