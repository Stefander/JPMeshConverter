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

        public ObjectData(byte[] data, uint subType) {
            Dependencies = new List<DependencyList>();
            transform = new Transform();
            
            uint offset = 0x0;
            uint u1 = ReadUint32(data);
            
            if (u1 == 0) {
                return; }

            offset += 0x4;
            uint u2 = ReadByte(data, offset);
            
            if (u2 == 0x10) {
                offset += 0x1C;
                uint u3 = ReadUint32(data, offset);
                offset += 0x4;
                
                for (int i = 0; i < u3; i++) {
                    offset += 0x18; }

                if(u1 == 2) {
                    offset += 0xC;
                    Dependencies.Add(ReadDependencyBlock(data,offset,0x1C)); }
            } else if (u2 == 0x7D) {
                if (u1 < 4) {
                    return; }

                offset += 0x1C;
                uint u3 = ReadUint32(data, offset);
                offset += 0x38;
            } else if (u2 == 0xB0) {
                offset += 0x34;
            } else if (u2 == 0xC0) {

            } else if (u2 == 0x91) {
            
            } else {
                Console.WriteLine(u1 + " " + ToHex(u2));
                return;
            }

            if (offset == data.Length) {
                return;
            }

            uint metaType = ReadUint32(data,offset);
            offset+=0x4;
            
            if (metaType == 0x4) {
                Console.WriteLine(ToHex(metaType) + " : " + ToHex(data, offset, 0x30) + " " + ToHex(u2) + " " + ToHex((uint)data.Length - offset));
            }
                /*if (u1 == 1) {
                if (u2 == 0x91) {
                    return; }

                offset+=0x1C;
                uint u3 = ReadUint32(data,offset);
                offset += 0x4;
                for (int i = 0; i < u3; i++) {
                    offset += 0x18; }
            } else if (u1 == 2) {
                if (u2 == 0xC0) {
                    offset+=0xD;
                }

                Console.WriteLine(u1+": "+ToHex(u2)+" !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! ");
            }
            if (u1 == 5) {
                Console.WriteLine(u1 + ": " + ToHex(u2) + " !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! ");
            }*/
            
            /*if (subType == 0x3) {
                uint u2 = ReadUint32(data, 0x10); // 0x1
                if ((u1 == 0x3 || u1 == 0x4) && u2 == 0x1) {
                    uint u3 = ReadUint32(data, 0x2D); // 0x2

                    if (u3 == 0x2) {
                        Dependencies.Add(ReadDependencyBlock(data, 0x6D));
                    } else {
                        Console.WriteLine("Prop reader " + SubType + ": u6 is " + u3);
                    }
                } else {
                    Console.WriteLine("PROP READER: Not supported combination u4: " + ToHex(u1) + " u5: " + ToHex(u2));
                }
            } else if (subType == 0x5) {
                uint u5 = ReadUint32(data, 0x10);

                if (u1 == 0x7 && u5 == 0x2) {
                    uint u6 = ReadUint32(data, 0x3A);
                    if (u6 == 0x1) {
                        Dependencies.Add(ReadDependencyBlock(data,0xA6));
                    } else {
                        Console.WriteLine("PROPREADER " + SubType + ": " + u1 + " " + u5 + " " + u6);
                    }
                    // TODO: Get the amount somewhere? Is it a combination of u4/u5/etc?
                } else {
                    Console.WriteLine("PROPREADER: Not supported u4 and u5: " + u1 + " " + u5);
                }
            } else if (subType == 0x6) {
                uint u18 = ReadUint32(data, 0x38);
                uint offset = u18 * 0xC+0x4B;

                if (u18 != 0x3) {
                    offset += (uint)(u18 == 0x5 ? 2 : 1);
                }

                uint type = ReadUint32(data, offset);

                DependencyList dependencyList = new DependencyList();
                bool readTransform = (u1 == 7 && type == 0x4) || (u1 == 5 && type == 0x1) ? false : true;

                offset += (uint)(type == 0x1 ? 0x54 : type == 0x2 ? 0x10 : 0xB8);
                Console.WriteLine("offset: " + ToHex(offset));

                if (readTransform) {
                    transform.Scale = ReadVector3(data, offset);
                    transform.Position = ReadVector3(data, 0x18 + offset);
                    transform.Rotation = ReadQuaternion(data, 0x40 + offset);
                }

                dependencyList = ReadDependencyBlock(data, readTransform ? 0x5C + offset : offset);
                Dependencies.Add(dependencyList);
                
                Console.WriteLine(u1+" "+type+" "+readTransform);
            }*/
        }
    }
}
