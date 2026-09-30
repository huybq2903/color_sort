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

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    /// <summary>
    ///     A read-only stream that presents other streams sequentially.
    /// </summary>
    public class ConcatenatedStream : Stream
    {
        private readonly Stream[] _streams;
        private int _currentIndex;
        private long _position;

        public ConcatenatedStream(params Stream[] streams)
        {
            _streams = streams ?? throw new ArgumentNullException(nameof(streams));
            foreach (var s in streams)
            {
                if (s == null) throw new ArgumentNullException(nameof(streams), "One of the streams is null");
                if (!s.CanRead) throw new ArgumentException("All streams must be readable", nameof(streams));
            }
        }

        public override bool CanRead => true;
        public override bool CanSeek => false; // Seeking not supported
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => _position;
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
            /* nothing to do */
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (offset < 0 || count < 0 || buffer.Length - offset < count) throw new ArgumentOutOfRangeException();

            if (_currentIndex >= _streams.Length)
                return 0;

            var totalBytesRead = 0;

            while (count > 0 && _currentIndex < _streams.Length)
            {
                var bytesRead = _streams[_currentIndex].Read(buffer, offset, count);
                if (bytesRead > 0)
                {
                    totalBytesRead += bytesRead;
                    offset += bytesRead;
                    count -= bytesRead;
                    _position += bytesRead;
                }
                else
                {
                    _currentIndex++;
                }
            }

            return totalBytesRead;
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            if (_currentIndex >= _streams.Length)
                return 0;

            var totalBytesRead = 0;

            while (buffer.Length > 0 && _currentIndex < _streams.Length)
            {
                var bytesRead = await _streams[_currentIndex]
                    .ReadAsync(buffer, cancellationToken)
                    .ConfigureAwait(false);

                if (bytesRead > 0)
                {
                    totalBytesRead += bytesRead;
                    _position += bytesRead;
                    buffer = buffer.Slice(bytesRead);
                }
                else
                {
                    _currentIndex++;
                }
            }

            return totalBytesRead;
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            // Use the Memory<byte> version for efficiency
            return ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
        }

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            if (bufferSize <= 0) throw new ArgumentOutOfRangeException(nameof(bufferSize));

            return CopyInternalAsync(destination, bufferSize, cancellationToken);
        }

        private async Task CopyInternalAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            var buffer = new byte[bufferSize];
            int bytesRead;
            while ((bytesRead = await ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) > 0)
                await destination.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
            
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                foreach (var s in _streams)
                    s?.Dispose();

            base.Dispose(disposing);
        }
    }
}