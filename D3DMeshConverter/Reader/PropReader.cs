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
using System.Windows.Forms;

namespace JPAssetReader {
    public class PropReader : BaseReader {
        public struct DependencyEntry {
            public List<string> Files;
        }

        public List<FileEntry> Modules { get; private set; }
        public List<DependencyEntry> Dependencies { get; private set; }
        public string RuleFile { get; private set; }
        
        public override bool Read(uint subType, FileStream stream) {
            base.Read(subType, stream);
            if (subType == 0x2) {
                byte[] header = ReadChunk(0x18);
                uint u1 = ReadUint32();
                uint u2 = ReadUint32(); // Zero
                uint u3 = ReadUint16();
                Modules = ReadFileNameBlock(0x0, false);
                Dependencies = new List<DependencyEntry>();
            } else if (subType == 0x3) {
                byte[] header = ReadChunk(0x24);
                uint u1 = ReadUint32();
                uint u2 = ReadUint32();
                uint u3 = ReadUint32();
                Modules = ReadFileNameBlock(0x0, false);
                uint dataSize = ReadUint32();
                byte[] data = ReadChunk(dataSize-0x4);
                uint u4 = ReadUint32(data,0x0); // 0x3
                uint u5 = ReadUint32(data,0x10); // 0x1
                uint u6 = ReadUint32(data,0x2D); // 0x2
                Vector3 u7 = ReadVector3(data,0x3D); // Position?
                Vector3 u8 = ReadVector3(data,0x55); // Position?

                // Read dependencies
                Dependencies = new List<DependencyEntry>();
                uint dependencyCount = ReadUint32(data,0x6D);
                uint offset = 0x7D;
                for (int i = 0; i < dependencyCount; i++) {
                    string fileName = ReadString(data,offset,false);
                    DependencyEntry entry = new DependencyEntry() { Files = new List<string>() };
                    entry.Files.Add(fileName);
                    Dependencies.Add(entry);
                    offset += (uint)fileName.Length + 0x4;
                }
            } else if (subType == 0x5) {
                byte[] header = ReadChunk(0x30);
                byte[] identifier = ReadChunk(0x8);
                byte[] unknownChunk = ReadChunk(0x10);
                uint u1 = ReadUint32(unknownChunk, 0xC);

                int i = 0;
                Modules = ReadFileNameBlock(0x0, false);

                if (u1 != 0xBA) {
                    MessageBox.Show(ToHex(u1) + " not implemented!", "Fail");
                    return false;
                }

                uint u = ReadUint32();
                uint u2 = ReadUint32();
                unknownChunk = ReadChunk(0xC);
                uint u3 = ReadUint32();
                unknownChunk = ReadChunk(0xC);

                for (i = 0; i < u3; i++) {
                    ReadChunk(0x10);
                }

                uint u4 = ReadUint32();

                if (u2 > 6) {
                    for (i = 0; i < u4; i++) {
                        ReadChunk(0x10);
                    }

                    ReadChunk(0x3);
                    u4 = ReadUint32();
                }

                unknownChunk = ReadChunk(0x28);
                Dependencies = new List<DependencyEntry>();

                // Loop through all the dependencies
                while (true) {
                    uint count = ReadUint32();
                    DependencyEntry entry = new DependencyEntry() { Files = new List<string>() };
                    for (int j = 0; j < count; j++) {
                        unknownChunk = ReadChunk(0xC);
                        entry.Files.Add(ReadString(false));
                    }
                    Dependencies.Add(entry);

                    // Break when the current chunk equals the identifier
                    if (ToHex(ReadChunk(0x8)).Equals(ToHex(identifier))) {
                        break;
                    }

                    unknownChunk = ReadChunk(0x4);
                }

                unknownChunk = ReadChunk(0x34);

                RuleFile = ReadString(false);
            } else {
                Console.WriteLine(_stream.Name+": Subtype "+subType+" not supported!");
            }

            return true;
        }
    }
}
