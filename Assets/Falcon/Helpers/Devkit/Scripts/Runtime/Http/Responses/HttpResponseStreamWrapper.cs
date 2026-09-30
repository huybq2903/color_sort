/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-09-09
 */

using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Falcon.Helpers.Devkit
{
    public class HttpResponseStreamWrapper : Stream
    {
        private readonly Stream _innerStream;
        private readonly HttpResponseMessage _response;
        private bool _disposed;

        public HttpResponseStreamWrapper(HttpResponseMessage response, Stream innerStream)
        {
            _response = response ?? throw new ArgumentNullException(nameof(response));
            _innerStream = innerStream ?? throw new ArgumentNullException(nameof(innerStream));
        }

        // --- Forward all abstract members to the inner stream ---
        public override bool CanRead => _innerStream.CanRead;
        public override bool CanSeek => _innerStream.CanSeek;
        public override bool CanWrite => _innerStream.CanWrite;
        public override long Length => _innerStream.Length;

        public override long Position
        {
            get => _innerStream.Position;
            set => _innerStream.Position = value;
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing)
            {
                _innerStream.Dispose();
                _response.Dispose();
            }

            _disposed = true;
            base.Dispose(disposing);
        }

        public override void Close()
        {
            Dispose(true);
        }

        public override void Flush()
        {
            _innerStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _innerStream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _innerStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _innerStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _innerStream.Write(buffer, offset, count);
        }

        // Async overrides (nếu cần)
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count,
            CancellationToken cancellationToken)
        {
            return await _innerStream.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await _innerStream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override async Task FlushAsync(CancellationToken cancellationToken)
        {
            await _innerStream.FlushAsync(cancellationToken);
        }
    }
}