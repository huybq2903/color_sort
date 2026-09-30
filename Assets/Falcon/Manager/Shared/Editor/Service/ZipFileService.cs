/*
     * Author: ngocdx
     * Email: ngocdx@falcongames.com
     * Company: Falcon Games
     * Date: 2025-07-30
     */


namespace Falcon.Manager.Shared
{
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.IO;
	using System.IO.Compression;
	using JetBrains.Annotations;
	using UnityEngine;
	using CompressionLevel = System.IO.Compression.CompressionLevel;

	public static class ZipFileService
	{
		/// <summary>
		/// Zip multiple files
		/// </summary>
		/// <param name="fileName">Full file name, e.g. D:\Files\abc.zip</param>
		/// <param name="files">Enumerable of file paths</param>
		public static void Zip(string fileName, IEnumerable<string> files)
		{
			var path = Path.GetDirectoryName(fileName);
			if (!string.IsNullOrEmpty(path) && !Directory.Exists(path))
			{
				Directory.CreateDirectory(path);
			}
			
			using (var zip = ZipFile.Open(fileName, ZipArchiveMode.Create))
			{
				foreach (var file in files)
				{
					if (File.Exists(file))
					{
						zip.CreateEntryFromFile(file, Path.GetFileName(file), CompressionLevel.Optimal);
					}
					else
					{
						Debug.LogError($"File {file} does not exist.");
					}
				}
			}
		}

		/// <summary>
		/// Open read-only, no extraction
		/// </summary>
		/// <param name="fileName">Full file name, e.g. D:\Files\abc.zip</param>
		/// <returns></returns>
		[CanBeNull]
		public static ReadOnlyCollection<ZipArchiveEntry> OpenRead(string fileName)
		{
			ReadOnlyCollection<ZipArchiveEntry> result = null;
			if (File.Exists(fileName))
			{
				using (var zip = ZipFile.OpenRead(fileName))
				{
					result = zip.Entries;
				}
			}
			
			return result;
		}

		/// <summary>
		/// Unzip file to a destination
		/// </summary>
		/// <param name="fileName">Full file name, e.g. D:\Files\abc.zip</param>
		/// <param name="destination">Destination of extraction</param>
		/// <param name="overwrite">Override files?</param>
		public static void Unzip(string fileName, string destination, bool overwrite)
		{
			if (File.Exists(fileName))
			{
				var path = Path.GetDirectoryName(destination);
				if (!string.IsNullOrEmpty(path) && !Directory.Exists(path))
				{
					Directory.CreateDirectory(path);
				}
				
				ZipFile.ExtractToDirectory(fileName, destination, overwrite);
			}
			else
			{
				Debug.LogError("ZIP file does not exist.");
			}
		}
	}
}