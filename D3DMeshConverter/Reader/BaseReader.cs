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
using System.Collections.Generic;
using System.IO;

namespace JPAssetReader {
    public interface IMeshReader {
        Mesh GetMesh();
    }

    public abstract class BaseReader : ByteReader {
        public struct FileEntry {
            public string Name;
            public byte[] Data;
        }

        public uint SubType;

        public virtual bool Read(uint subType, FileStream stream) {
            SubType = subType;
            _stream = stream;
            return true;
        }

        /// <summary>
        /// Reads file name blocks
        /// </summary>
        protected List<FileEntry> ReadFileNameBlock(uint dataSize, bool hasChecksum=true) {
            // Read the texture count
            uint textureCount = ReadUint32();

            List<FileEntry> outList = new List<FileEntry>();
            
            // Read all texture entries
            for (int i = 0; i < textureCount; i++) {
                String fileName = ReadString(hasChecksum);
                byte[] fileData = ReadChunk(dataSize); // Unknown
                //Console.WriteLine(i+": "+fileName);
                FileEntry entry = new FileEntry() { Name = fileName, Data = fileData };
                outList.Add(entry);
            }

            return outList;
        }

        /// <summary>
        /// Shows the current stream position, in hexadecimal format
        /// </summary>
        protected void CurrentPos() {
            string hexPosition = ToHex((uint)_stream.Position);
            Console.WriteLine("0x" + hexPosition);
        }

        /// <summary>
        /// Helper function for reading padding
        /// </summary>
        /// <param name="length">Padding length (in uint32)</param>
        protected void ReadPadding(int length) {
            for (int i = 0; i < length; i++) {
                ReadUint32();
            }
        }

        protected String ReadString(byte[] data, uint offset, bool hasChecksum) {
            uint fileNameOffset = 0;

            if (hasChecksum) {
                fileNameOffset = ReadUint32(data,offset); // Filename length+8
            }

            uint fileNameLength = ReadUint32(data,hasChecksum ? offset+4 : offset); // Filename length

            // If this isn't an appropriate string header, return null
            if (hasChecksum && fileNameOffset != fileNameLength + 8) {
                return null;
            }

            // Read the string and return it
            return ReadString(data,(uint)(hasChecksum ? offset+0x8 : offset+0x4),fileNameLength);
        }

        /// <summary>
        /// Helper function for reading strings
        /// </summary>
        /// <returns>The string, null if invalid</returns>
        protected String ReadString(bool hasChecksum=true) {
            uint fileNameOffset = 0;
            
            if (hasChecksum) {
                fileNameOffset = ReadUint32(); // Filename length+8
            }

            uint fileNameLength = ReadUint32(); // Filename length

            // If this isn't an appropriate string header, return null
            if (hasChecksum && fileNameOffset != fileNameLength + 8) {
                return null;
            }

            // Read the string and return it
            return ReadString(fileNameLength);
        }
    }
}
