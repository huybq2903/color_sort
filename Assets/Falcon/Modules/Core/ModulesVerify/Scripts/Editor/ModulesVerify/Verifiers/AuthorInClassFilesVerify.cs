/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 */
// 2025-07-01


namespace Falcon.Modules.Core.ModulesVerify.Editor
{
    [FAModuleVerify("Điền tác giả trong các file .cs")]
    public class AuthorInClassFilesVerify : IModuleVerify
    {
        public (bool, string) VerifyModules(FModule module)
        {
            if (module.HasTextInAllFiles("falcongames.com"))
            {
                return (true, "");
            }
            else
            {
                return (false, "Chưa điền hết tên, email vào các file .cs");
            }
        }
    }
}