/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System;
// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    public class FTempFile : FFileWrapper, IDisposable
    {
        public FTempFile(IFFile baseFile)
        {
            BaseFile = baseFile;
        }

        protected override IFFile BaseFile { get; }

        public void Dispose()
        {
            if (Exists()) Delete();
        }
    }
    
    public static class FTempFileExtensions
    {
        public static FTempFile AsTempFile(this IFFile file)
        {
            return new FTempFile(file);
        }
    }
}