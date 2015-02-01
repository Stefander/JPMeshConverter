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

using System.Collections.Generic;

namespace JPAssetReader {
    public class DependencyList {
        public List<string> Objects = new List<string>();

        public void Add(string fileName) {
            Objects.Add(fileName);
        }

        public uint Count {
            get {
                return (uint)Objects.Count;
            }
        }
    }

    public class ObjectMeta : BaseReader {
        public List<DependencyList> Dependencies;
        public Transform transform;
        private byte[] data;

        public ObjectMeta(byte[] inData) {
            data = inData;
            transform = new Transform();

            uint u1 = ReadUint32(data);

            DependencyList dependencyList = new DependencyList();

            ReadDependencies(dependencyList);
            ReadTransform();

            Dependencies = new List<DependencyList>() { dependencyList };
        }

        private void ReadDependencies(DependencyList list) {
            if (data.Length < 0x14) {
                return; }

            uint localOffset = (uint)data.Length;
            
            // Make sure to align the data
            localOffset-=0xC;
            while (ReadUint32(data, localOffset + 0x8) != 0x7ADB4E5A && localOffset > 0) {
                localOffset--;
            }

            if (localOffset > 0x8) {
                uint size;
                foreach (string entry in ReadDependencyBlock(data, localOffset, out size, 0x10, 0xC).Objects) {
                    list.Add(entry); }
            } else {
                localOffset = (uint)data.Length;
                uint u3 = ReadUint32(data, localOffset - 4);

                if (u3 >= 0x1 && u3 <= 0x7) {
                    while (u3 >= 0x1 && u3 <= 0x7) {
                        localOffset -= 0x10;
                        u3 = ReadUint32(data, localOffset - 4);
                    }
                }

                // check if there is a string to read
                uint textOffset = 0;
                while (ReadUint32(data, localOffset - textOffset - 4) != textOffset && localOffset - textOffset > 4) {
                    textOffset++;
                }

                bool hasText = localOffset - textOffset > 4;

                if (hasText) {
                    int stringCount = 0;
                    while (ReadUint32(data, localOffset - 0x4) != stringCount) {
                        uint stringLength = 0;

                        while (ReadUint32(data, localOffset - stringLength - 0x4) != stringLength) {
                            stringLength++;
                            uint character = ReadByte(data,localOffset-stringLength);
                            if (localOffset - stringLength <= 0x4) {
                                return; }
                        }

                        string dependencyName = ReadString(data, localOffset - stringLength - 0x4, false);

                        list.Add(dependencyName);
                        localOffset -= stringLength + 4;

                        uint prefix = ReadUint32(data, localOffset - 0x4);
                        if (prefix == 0) {
                            localOffset -= 0xC;
                        } else if (prefix == 1 || stringLength == prefix - 8) {
                            break;
                        }

                        stringCount++;
                    }
                }
            }
        }

        private void ReadTransform() {
            if (data.Length < 0x54) {
                return; }

            // Skip to the start of the block
            uint localOffset = (uint)data.Length-0x50;

            // Make sure to align the data properly
            while (ReadUint32(data, localOffset + 0x38) != 0x53F29BE3) {
                localOffset--;

                if (localOffset < 0x10) {
                    return; }
            }

            // Only read scale when needed
            if (ReadUint32(data, localOffset - 0x10) == 0x2) {
                transform.Scale = ReadVector3(data, localOffset);
            }

            // Read transform data
            transform.Position = ReadVector3(data, 0x18 + localOffset);
            transform.Rotation = ReadQuaternion(data, 0x40 + localOffset);

            //Console.WriteLine(ToHex(ReadUint32(data, localOffset - 0x10))+" "+transform.Scale+" "+transform.Position+" "+transform.Rotation);
        }
    }
}
