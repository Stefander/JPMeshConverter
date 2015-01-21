using System;
using System.Collections.Generic;

namespace JPAssetReader {
    public struct DependencyEntry {
        public List<string> Objects;
    }

    public class ObjectData : BaseReader {
        public enum SceneObjectType {
            Group = 0x1,
            Model = 0x2,
            Camera = 0x4
        }

        public List<DependencyEntry> Dependencies;
        public Transform transform;

        public ObjectData(byte[] data, uint subType) {
            uint u1 = ReadUint32(data,0x0);
            Dependencies = new List<DependencyEntry>();

            if (subType == 0x3) {
                uint u2 = ReadUint32(data, 0x10); // 0x1
                if ((u1 == 0x3 || u1 == 0x4) && u2 == 0x1) {
                    uint u3 = ReadUint32(data, 0x2D); // 0x2

                    if (u3 == 0x2) {
                        // Read dependencies
                        uint dependencyCount = ReadUint32(data, 0x6D);
                        uint offset = 0x7D;
                        for (int i = 0; i < dependencyCount; i++) {
                            string fileName = ReadString(data, offset, false);
                            DependencyEntry entry = new DependencyEntry() { Objects = new List<string>() };
                            entry.Objects.Add(fileName);
                            Dependencies.Add(entry);
                            offset += (uint)fileName.Length + 0x4;
                        }
                    }
                    else {
                        Console.WriteLine("Prop reader " + SubType + ": u6 is " + u3);
                    }
                } else {
                    Console.WriteLine("PROP READER: Not supported combination u4: " + ToHex(u1) + " u5: " + ToHex(u2));
                }
            } else if (subType == 0x5) {
                uint u4 = ReadUint32(data, 0x0);
                uint u5 = ReadUint32(data, 0x10);

                if (u4 == 0x7 && u5 == 0x2) {
                    uint u6 = ReadUint32(data, 0x3A);
                    if (u6 == 0x1) {
                        uint dependencyCount = ReadUint32(data, 0xA6);
                        uint offset = 0xB6;
                        DependencyEntry entry = new DependencyEntry() { Objects = new List<string>() };
                        for (int j = 0; j < dependencyCount; j++) {
                            string fileName = ReadString(data, offset, false);
                            entry.Objects.Add(fileName);
                            offset += (uint)fileName.Length + 0x4;
                        }
                        Dependencies.Add(entry);
                    }
                    else {
                        Console.WriteLine("PROPREADER " + SubType + ": " + u4 + " " + u5 + " " + u6);
                    }
                    // TODO: Get the amount somewhere? Is it a combination of u4/u5/etc?
                } else {
                    Console.WriteLine("PROPREADER: Not supported u4 and u5: " + u4 + " " + u5);
                }
            } else if (subType == 0x6) {
                uint u14 = ReadUint32(data, 0x20);
                uint u15 = ReadUint32(data, 0x24);

                uint u16 = ReadUint32(data, 0x2C);
                uint u17 = ReadUint32(data, 0x30);

                uint u18 = ReadUint32(data, 0x38);

                uint offset = u18 * 0xC;

                if (u18 != 0x3) {
                    offset += (uint)(u18 == 0x5 ? 2 : 1);
                }

                SceneObjectType type = (SceneObjectType)ReadUint32(data, 0x4B + offset);

                // Read transform data
                string groupName = null;
                if (type == SceneObjectType.Group) {
                    Vector2 u19 = ReadVector2(data, 0x4F + offset); // Unknown
                    Vector3 u20 = ReadVector3(data, 0x5B + offset); // Unknown (valid v3)
                    Vector3 u21 = ReadVector3(data, 0x68 + offset); // Unknown
                    uint u22 = ReadUint32(data, 0x73 + offset);

                    if (u22 == 0) {
                        Vector3 u23 = ReadVector3(data, 0x87 + offset); // Unknown
                        transform.Position = ReadVector3(data, 0xB7 + offset); // Position?
                        groupName = ReadString(data, 0x10B + offset, false);
                    } else {
                        groupName = ReadString(data, 0xAF + offset, false);
                    }
                }
                else if (type == SceneObjectType.Model) {
                    uint u20 = ReadUint32(data, 0x4F + offset); // Unknown
                    uint u21 = ReadUint32(data, 0x53 + offset); // Unknown

                    transform.Scale = ReadVector3(data, 0x5B + offset); // Scale ?
                    transform.Position = ReadVector3(data, 0x73 + offset); // Position ?
                    transform.Rotation = ReadQuaternion(data, 0x9B + offset); // Quaternion ?
                    groupName = ReadString(data, 0xC7 + offset, false);
                }

                if (groupName != null && groupName.Length > 0) {
                    Dependencies.Add(new DependencyEntry() { Objects = new List<string>() { groupName } });
                }
            }
        }
    }
}
