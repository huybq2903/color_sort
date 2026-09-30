/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 */
// 2025-06-22


using Falcon.Helpers.FReflection;

namespace Falcon.Modules.Core.ModulesVerify.Editor
{
    public interface IModuleVerify : IFReflection
    {
        public const string OK_STATUS = "OK";
        public const string ERROR_STATUS = "ERROR";
        public (bool, string) VerifyModules(FModule module);
    }
}