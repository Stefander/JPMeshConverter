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
        public DependencyList Modules { get; private set; }
        public List<DependencyList> Dependencies { get; private set; }
        
        public override bool Read(uint subType, FileStream stream) {
            base.Read(subType, stream);
            uint headerSize = (uint)(subType == 0x5 || subType == 0x4 ? 0x30 : subType == 0x3 ? 0x24 : 0x18);
            byte[] header = ReadChunk(headerSize);

            if (subType == 0x2 || subType == 0x3 || subType == 0x4) {
                uint u1 = ReadUint32();
                uint u2 = ReadUint32(); // Zero
                uint u3 = ReadUint32();
            } else if (subType == 0x5) {
                byte[] identifier = ReadChunk(0x8);
                byte[] unknownChunk = ReadChunk(0x10);
                uint u1 = ReadUint32(unknownChunk, 0xC);
            } else {
                Console.WriteLine(_stream.Name+": Subtype "+ToHex(subType)+" not supported!");
                return false;
            }

            Modules = ReadDependencyBlock(0x0, false);
            uint dataSize = ReadUint32();
            ObjectData objectData = new ObjectData(ReadChunk(dataSize - 0x4), subType);
            Dependencies = objectData.Dependencies;

            return true;
        }
    }
}
