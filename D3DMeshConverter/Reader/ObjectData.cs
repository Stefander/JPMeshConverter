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
            Dependencies = new List<DependencyList>();
            transform = new Transform();

            offset = 0;
            uint u1 = ReadUint32(data);

            if (u1 < 2) {
                return; }

            offset += 0x10;
            uint u2 = ReadUint32(data,offset);

            uint dependencyOffset = (uint)data.Length;
            uint u3 = ReadUint32(data, dependencyOffset-4);

            if (u3 >= 0x1 && u3 <= 0x7) {
                while (u3 >= 0x1 && u3 <= 0x7) {
                    dependencyOffset -= 0x10;
                    u3 = ReadUint32(data, dependencyOffset - 4); }
            }

            int stringCount = 0;

            while(ReadUint32(data,dependencyOffset-0x4) != stringCount) {
                uint stringLength = 2;

                while (ReadUint16(data, dependencyOffset - stringLength) != stringLength - 4) {
                    stringLength++;
                }

                //Console.WriteLine(stringCount+": "+ReadString(data, dependencyOffset - stringLength, false));
                dependencyOffset -= stringLength;

                uint prefix = ReadUint32(data, dependencyOffset - 0x4);
                if (prefix == 0) {
                    dependencyOffset -= 0xC; }
                else if(prefix == 1 || stringLength == prefix-0x4) {
                    break; }

                stringCount++;
            }

            dependencyOffset -= 0x60;

            // Make sure to align the data properly
            while (ReadUint32(data, dependencyOffset + 0x38) != 0x53F29BE3) {
                dependencyOffset--;

                if (dependencyOffset + 0x38 == 0) {
                    return; }
            }

            // Only read scale when needed
            if (ReadUint32(data, dependencyOffset - 0x10) == 0x2) {
                transform.Scale = ReadVector3(data, dependencyOffset); }

            // Read transform data
            transform.Position = ReadVector3(data, 0x18 + dependencyOffset);
            transform.Rotation = ReadQuaternion(data, 0x40 + dependencyOffset);

            Console.WriteLine(transform.Scale + " " + transform.Position + " " + transform.Rotation);
        }
    }
}
