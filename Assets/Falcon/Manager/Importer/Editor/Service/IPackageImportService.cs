/*
     * Author: ngocdx
     * Email: ngocdx@falcongames.com
     * Company: Falcon Games
     * Date: 2026-03-03
     */


namespace Falcon.Manager.Importer
{
	using UnityEditor;

	public interface IPackageImportService
	{
		void ImportPackage(string path, bool userInteractive);
	}
	
	public class DefaultUnityImporter : IPackageImportService
	{
		public void ImportPackage(string path, bool userInteractive)
		{
			AssetDatabase.ImportPackage(path, userInteractive);
		}
	}

	public class OnlyNewImporter : IPackageImportService
	{
		public void ImportPackage(string path, bool userInteractive)
		{
			UnityPackageCustomImporter.ShowPreviewWindow(path);
		}
	}
}