/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 */
// 2025-06-13

namespace Falcon.Modules.Core.AccountData 
{
    using global::Falcon.Modules.Core.SystemInformation.Runtime;
    [FGameDataType("apk_info")]
    public class ApkInfo : FGameData<ApkInfo>
    {
        public string app_version = SystemInformation.App.Version;
        public int app_version_int = SystemInformation.App.VersionInt;
        public string install_vendor = SystemInformation.App.InstallVendor;
        public int number_lib_files = SystemInformation.App.NumberLibFiles;
        public long total_lib_file_size = SystemInformation.App.TotalLibFileSize;
        public string lib_file_name_list = SystemInformation.App.LibFileNameList;
        public string lib_folder = SystemInformation.App.LibFolder;
        public string lib_md5 = SystemInformation.App.LibMD5;
        public string package_name = SystemInformation.App.PackageName;

        public override void PostConstructor()
        {
            app_version = SystemInformation.App.Version;
            app_version_int = SystemInformation.App.VersionInt;
            install_vendor = SystemInformation.App.InstallVendor;
            number_lib_files = SystemInformation.App.NumberLibFiles;
            total_lib_file_size = SystemInformation.App.TotalLibFileSize;
            lib_file_name_list = SystemInformation.App.LibFileNameList;
            lib_folder = SystemInformation.App.LibFolder;
            lib_md5 = SystemInformation.App.LibMD5;
            package_name = SystemInformation.App.PackageName;
        }
    }
}