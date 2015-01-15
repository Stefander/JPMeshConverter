/*
 * Copyright (c) 2015 Stefan Wijnker
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), 
 * to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, 
 * and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS 
 * IN THE SOFTWARE.
*/

using System;
using System.IO;
using System.Text;

namespace JPMeshConverter {
    public enum EndianType {
        Little = 0,
        Big
    }

    public class ByteReader {
        protected EndianType type = EndianType.Little;
        protected FileStream _stream;

        protected virtual void Open(string fileName) {
            // Open the file as a new stream
            _stream = new FileStream(fileName, FileMode.Open);
        }

        protected virtual void Close() {
            // Close the stream
            _stream.Close();
        }

        protected byte[] ReadChunk(uint size) {
            byte[] dataChunk = new byte[size];
            
            _stream.Read(dataChunk, 0, (int)size);
            return dataChunk;
        }

        protected byte[] ReadChunk(uint offset, uint size) {
            _stream.Seek(offset, SeekOrigin.Begin);
            return ReadChunk(size);
        }

        protected uint ReadByte(byte[] data) {
            uint value = (uint)data[0];
            return value;
        }

        protected float ReadFloat16() {
            return ReadFloat16(ReadChunk(2), 0);
        }

        protected float ReadFloat16(byte[] data, uint offset) {
            return (ReadUint16(data, offset) / (float)UInt16.MaxValue) * 2;
        }

        protected uint ReadUint32(byte[] data, uint offset) {
            uint value = BitConverter.ToUInt32(data, (int)offset);
            if (type == EndianType.Little) {
                return value;
            }

            var b1 = (value >> 0) & 0xff;
            var b2 = (value >> 8) & 0xff;
            var b3 = (value >> 16) & 0xff;
            var b4 = (value >> 24) & 0xff;

            return b1 << 24 | b2 << 16 | b3 << 8 | b4 << 0;
        }

        protected uint ReadUint16(byte[] data, uint offset) {
            uint value = BitConverter.ToUInt16(data, (int)offset);
            if (type == EndianType.Little) {
                return value;
            }

            var b1 = (value >> 0) & 0xff;
            var b2 = (value >> 8) & 0xff;

            return b1 << 8 | b2 << 0;
        }

        protected String ReadString(uint length) {
            return ReadString(ReadChunk(length),0,length);
        }

        protected float ReadFloat(byte[] data, uint offset) {
            return BitConverter.ToSingle(data, (int)offset);
        }

        protected float ReadFloat() {
            return ReadFloat(ReadChunk(4),0);
        }

        protected Vector2 ReadVector2() {
            return new Vector2(ReadFloat(),ReadFloat());
        }

        protected Vector3 ReadVector3() {
            return new Vector3(ReadFloat(),ReadFloat(),ReadFloat());
        }

        protected uint ReadUint32() {
            return ReadUint32(ReadChunk(4),0);
        }

        protected uint ReadByte() {
            return ReadByte(ReadChunk(1));
        }

        protected uint ReadUint16() {
            return ReadUint16(ReadChunk(2),0);
        }

        protected String ReadString(byte[] data, uint offset, uint length) {
            char[] removeChars = { (char)0, '\n' };
            return Encoding.UTF8.GetString(data, (int)offset, (int)length).Trim(removeChars);
        }

        public String ToHex(byte[] array) {
            return BitConverter.ToString(array);
        }

        public String ToHex(uint data) {
            return data.ToString("X");
        }
    }
}
