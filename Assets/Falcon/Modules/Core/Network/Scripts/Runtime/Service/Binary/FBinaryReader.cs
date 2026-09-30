/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 * Date: 2025-11-02
 */
using System;
using System.Buffers.Binary;
using System.Text;
using UnityEngine;

namespace Falcon.Modules.Core.Network
{
    public class FBinaryReader
    {
        private readonly byte[] _buffer;
        private int _offset;

        public FBinaryReader(byte[] buffer)
        {
            _buffer = buffer;
            _offset = 0;
        }

        public int Position => _offset;

        public int ReadInt()
        {
            int value = BinaryPrimitives.ReadInt32LittleEndian(_buffer.AsSpan(_offset, 4));
            _offset += 4;
            return value;
        }

        public long ReadLong()
        {
            long value = BinaryPrimitives.ReadInt64LittleEndian(_buffer.AsSpan(_offset, 8));
            _offset += 8;
            return value;
        }

        public float ReadFloat()
        {
            int bits = ReadInt();
            return BitConverter.Int32BitsToSingle(bits);
        }

        public bool ReadBool()
        {
            return _buffer[_offset++] != 0;
        }

        public ushort ReadUShort()
        {
            ushort value = BinaryPrimitives.ReadUInt16LittleEndian(_buffer.AsSpan(_offset, 2));
            _offset += 2;
            return value;
        }

        public string ReadString()
        {
            ushort len = ReadUShort();
            if (len == 0)
                return string.Empty;

            string s = Encoding.UTF8.GetString(_buffer, _offset, len);
            _offset += len;
            return s;
        }

        public Vector3 ReadVector3()
        {
            float x = ReadFloat();
            float y = ReadFloat();
            float z = ReadFloat();
            return new Vector3(x, y, z);
        }
        
        public byte[] ReadBytes()
        {
            int length = ReadInt();
            if (length <= 0)
                return Array.Empty<byte>();

            byte[] data = new byte[length];
            Array.Copy(_buffer, _offset, data, 0, length);
            _offset += length;
            return data;
        }

        public void Reset()
        {
            _offset = 0;
        }
    }
}