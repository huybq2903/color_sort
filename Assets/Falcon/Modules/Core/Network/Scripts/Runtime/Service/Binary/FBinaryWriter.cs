/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 * Date: 2025-11-02
 */
using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;
using UnityEngine;

namespace Falcon.Modules.Core.Network
{
    public class FBinaryWriter
    {
        private byte[] _buffer;
        private int _offset;

        public FBinaryWriter(int capacity = 256)
        {
            _buffer = new byte[capacity];
            _offset = 0;
        }

        public FBinaryWriter(byte[] buffer)
        {
            _buffer = buffer;
            _offset = 0;
        }

        public int Position => _offset;
        public byte[] Buffer => _buffer;

        private void EnsureCapacity(int size)
        {
            if (_offset + size > _buffer.Length)
            {
                Array.Resize(ref _buffer, Math.Max(_buffer.Length * 2, _offset + size));
            }
        }

        public void WriteInt(int value)
        {
            EnsureCapacity(4);
            BinaryPrimitives.WriteInt32LittleEndian(_buffer.AsSpan(_offset, 4), value);
            _offset += 4;
        }

        public void WriteLong(long value)
        {
            EnsureCapacity(8);
            BinaryPrimitives.WriteInt64LittleEndian(_buffer.AsSpan(_offset, 8), value);
            _offset += 8;
        }

        public void WriteFloat(float value)
        {
            WriteInt(BitConverter.SingleToInt32Bits(value));
        }

        public void WriteBool(bool value)
        {
            EnsureCapacity(1);
            _buffer[_offset++] = (byte)(value ? 1 : 0);
        }

        public void WriteUShort(ushort value)
        {
            EnsureCapacity(2);
            BinaryPrimitives.WriteUInt16LittleEndian(_buffer.AsSpan(_offset, 2), value);
            _offset += 2;
        }

        public void WriteString(string s)
        {
            if (s == null)
            {
                WriteUShort(0);
                return;
            }

            var byteCount = Encoding.UTF8.GetByteCount(s);
            WriteUShort((ushort)byteCount);
            EnsureCapacity(byteCount);
            Encoding.UTF8.GetBytes(s, 0, s.Length, _buffer, _offset);
            _offset += byteCount;
        }

        public void WriteVector3(Vector3 v)
        {
            WriteFloat(v.x);
            WriteFloat(v.y);
            WriteFloat(v.z);
        }
        
        /// <summary>
        /// Ghi trực tiếp mảng byte vào buffer (có prefix độ dài nếu muốn).
        /// </summary>
        public void WriteBytes(byte[] data)
        {
            if (data == null)
            {
                WriteInt(0);
                return;
            }

            int len = data.Length;
            WriteInt(len);

            EnsureCapacity(len);
            Array.Copy(data, 0, _buffer, _offset, len);
            _offset += len;
        }

        public byte[] ToArray()
        {
            byte[] result = new byte[_offset];
            Array.Copy(_buffer, result, _offset);
            return result;
        }

        public void Reset()
        {
            _offset = 0;
        }
    }
}
