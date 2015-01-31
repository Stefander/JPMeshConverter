using System;
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

    public class ObjectData : BaseReader {
        public List<DependencyList> Dependencies;
        public Transform transform;
        private byte[] data;
        private uint offset;

        public ObjectData(byte[] inData, uint subType) {
            data = inData;
            transform = new Transform();

            offset = 0;
            uint u1 = ReadUint32(data);

            DependencyList dependencyList = new DependencyList();
            Dependencies = new List<DependencyList>() { dependencyList };

            if (u1 < 2) {
                return; }

            offset += 0x10;
            uint u2 = ReadUint32(data,offset);

            uint dependencyOffset = (uint)data.Length;
            uint u3 = ReadUint32(data, dependencyOffset-4);

            if (u3 >= 0x1 && u3 <= 0x7) {
                while (u3 >= 0x1 && u3 <= 0x7) {
                    dependencyOffset -= 0x10;
                    u3 = ReadUint32(data, dependencyOffset - 4);
                }
            }

            uint oldOffset = dependencyOffset;
            while (ReadUint32(data, dependencyOffset - 0x8) != 0x73E09E0F && dependencyOffset > 0x8) {
                dependencyOffset--;
            }

            if (dependencyOffset > 0x8) {
                //Console.WriteLine("Found alignment");
                uint count = 0;
                
                // Check for false positive
                if (ReadUint32(data, dependencyOffset+0x10) == 0) {
                    return;
                }

                while (dependencyOffset < data.Length && ReadUint32(data,dependencyOffset-0x8) != 0xA5B4E052) {
                    uint size;
                    uint dependencyCount = ReadUint32(data, dependencyOffset);
                    
                    /*if () {//ReadUint32(data,dependencyOffset-0x8) != ) {
                        
                        /*uint strCount = ReadUint32(data,dependencyOffset);
                        dependencyOffset += 0x14;
                        for (int s = 0; s < 4; s++) {
                            string str = ReadString(data, dependencyOffset, false);
                            dependencyList.Add(str);
                            uint sOffset = (uint)(s == 0 ? 0x1C : s == 1 ? 0x24 : s == 2 ? 0x20 : 0x4);
                            dependencyOffset+=sOffset+(uint)str.Length;
                        }
                    } else {*/
                        foreach (string entry in ReadDependencyBlock(data, dependencyOffset, out size,0x10,0xC).Objects) {
                            dependencyList.Add(entry); }
                        dependencyOffset += size;
                    //}
                    count++;
                }
            }
            else {
                dependencyOffset = oldOffset;

                // check if there is a string to read
                uint textOffset = 0;
                while(ReadUint32(data,dependencyOffset-textOffset-4) != textOffset && dependencyOffset-textOffset > 4) {
                    textOffset++;
                }

                bool hasText = dependencyOffset - textOffset > 4;

                if (hasText) {
                    int stringCount = 0;
                    while (ReadUint32(data, dependencyOffset - 0x4) != stringCount) {
                        uint stringLength = 2;

                        while (ReadUint16(data, dependencyOffset - stringLength) != stringLength - 4) {
                            stringLength++;
                            if (dependencyOffset - stringLength <= 0x4) {
                                return;
                            }
                        }

                        string dependencyName = ReadString(data, dependencyOffset - stringLength, false);
                        dependencyList.Add(dependencyName);
                        dependencyOffset -= stringLength;

                        uint prefix = ReadUint32(data, dependencyOffset - 0x4);
                        if (prefix == 0) {
                            dependencyOffset -= 0xC;
                        }
                        else if (prefix == 1 || stringLength == prefix - 0x4) {
                            break;
                        }

                        stringCount++;
                    }
                }
            }

            dependencyOffset -= 0x60;

            // Make sure to align the data properly
            while (ReadUint32(data, dependencyOffset + 0x38) != 0x53F29BE3) {
                dependencyOffset--;

                if (dependencyOffset + 0x38 == 0) {
                    return; }
            }

            // Only read scale when needed
            if (ReadUint32(data, dependencyOffset - 0x10) <= 0x2) {
                transform.Scale = ReadVector3(data, dependencyOffset); }

            // Read transform data
            transform.Position = ReadVector3(data, 0x18 + dependencyOffset);
            transform.Rotation = ReadQuaternion(data, 0x40 + dependencyOffset);
        }
    }
}
