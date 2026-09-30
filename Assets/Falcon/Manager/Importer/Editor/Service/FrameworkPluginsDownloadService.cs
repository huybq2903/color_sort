/*
     * Author: ngocdx
     * Email: ngocdx@falcongames.com
     * Company: Falcon Games
     * Date: 2025-06-23
     */

namespace Falcon.Manager.Importer
{
    using System.Threading.Tasks;
    using Shared;

    public class FrameworkPluginsDownloadService
    {
        public async Task DownloadAndImport()
        {
            var url = GetUrl();
            await SimplePackageImporter.DownloadAndImport("Framework Plugins", url);
        }

        private string GetUrl()
        {
            var prefix = $"{Configuration.URL_MAIN}/{Configuration.BUCKET}";
            return $"{prefix}/plugin-lib/FPlugins.unitypackage";
        }

        public string[] GetPluginsInfo()
        {
            return new[]
            {
                "DOTween Pro",
                "Odin Inspector"
            };
        }
    }
}