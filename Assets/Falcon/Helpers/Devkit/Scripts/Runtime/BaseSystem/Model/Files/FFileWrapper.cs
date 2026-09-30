/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-08-28
 */

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Falcon.Helpers.Devkit
{
    public abstract class FFileWrapper : IFFile
    {
        protected abstract IFFile BaseFile { get; }

        public virtual bool Exists()
        {
            return BaseFile.Exists();
        }

        public virtual Task SaveAsync(Stream data, int bufferSize = 4096, CancellationToken token = default)
        {
            return BaseFile.SaveAsync(data, bufferSize, token);
        }

        public virtual Task AppendAsync(Stream data, int bufferSize = 4096, CancellationToken token = default)
        {
            return BaseFile.AppendAsync(data, bufferSize, token);
        }

        public virtual Task<Stream> LoadAsync(CancellationToken token = default)
        {
            return BaseFile.LoadAsync(token);
        }
        
        public virtual void Delete()
        {
            BaseFile.Delete();
        }

        public virtual DateTime CreatedTimeUtc()
        {
            return BaseFile.CreatedTimeUtc();
        }

        public virtual DateTime LastWriteTimeUtc()
        {
            return BaseFile.LastWriteTimeUtc();
        }
    }
}