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
    class SceneReader : BaseReader, IMeshReader {
        public struct SceneObject {
            public string Name;
            public List<FileEntry> Dependencies;
            public Transform Transform;
            public string Group;
            public SceneObjectType Type;
            public List<SceneObject> children = new List<SceneObject>();
        }

        public enum SceneObjectType {
            Group = 0x1,
            Model = 0x2,
            Camera = 0x4
        }

        public string Name;
        public List<SceneObject> Objects;

        public override bool Read(uint subType, FileStream stream) {
            base.Read(subType, stream);

            if (subType != 0x6) {
                MessageBox.Show("Subtype "+ToHex(subType)+" not supported!");
                return false;
            }

            ReadChunk(0x49);

            // Read the scene name
            Name = ReadString();

            uint u1 = ReadUint32();
            ReadChunk(0x4); // Zero
            uint u2 = ReadUint32();

            ReadChunk(0x9);
            float u3 = ReadFloat();
            ReadByte();
            float u4 = ReadFloat();
            byte[] unknownChunk = ReadChunk(0x8); // Zero, padding?
            Vector2 u5 = ReadVector2();
            ReadByte();
            float u6 = ReadFloat();
            ReadChunk(0x8); // Zero, padding?
            Vector2 u7 = ReadVector2();
            ReadChunk(0x5);
            Vector2 u8 = ReadVector2();
            uint u9 = ReadUint32();

            Objects = new List<SceneObject>();

            uint objectCount = ReadUint32();
            uint i = 0;

            for (i=0; i < objectCount; i++) {
                SceneObject sceneObject = ReadObject();
                Objects.Add(sceneObject);
            }

            return true;
        }

        public Mesh GetMesh() {
            Mesh m = null;
            string path = Common.GetPath(_stream.Name);
            Console.WriteLine(path);

            foreach (SceneObject obj in Objects) {
                for (int i = 0; i < obj.Dependencies.Count; i++) {
                    FileEntry file = obj.Dependencies[i];
                    string propPath = path+"\\"+file.Name;
                    //Console.WriteLine(obj.Name+": "+obj.Group);

                    // Only load it when the prop file actually exists
                    if(File.Exists(propPath)) {
                        // Only load prop files
                        if (Common.GetExtension(file.Name).Equals("prop")) {
                            JurassicReader r = new JurassicReader();
                            if (r.Read(propPath)) {
                                List<PropReader.DependencyEntry> dependencies = (r.reader as PropReader).Dependencies;
                                foreach (PropReader.DependencyEntry dependency in dependencies) {
                                    foreach (string entry in dependency.Files) {
                                        // Only load from d3dmeshes
                                        if (Common.GetExtension(entry).Equals("d3dmesh")) {
                                            string meshPath = path+"\\"+entry;
                                            if (File.Exists(meshPath)) {
                                                JurassicReader modelReader = new JurassicReader();
                                                if (modelReader.Read(meshPath)) {
                                                    Mesh propMesh = modelReader.mesh;
                                                    propMesh.Transform(obj.Transform);
                                                    if (m == null) {
                                                        m = propMesh;
                                                    } else {
                                                        m.Combine(propMesh);
                                                    }
                                                }
                                            } else {
                                                Console.WriteLine(entry+" does not exist - skipping");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    } else {
                        Console.WriteLine(file.Name+" does not exist - skipping");
                    }
                }
            }

            return m;
        }

        private SceneObject ReadObject() {
            string objectName = ReadString();

            SceneObject obj = new SceneObject() { Name = objectName };

            byte[] unknownChunk = ReadChunk(0x10);
            uint u10 = ReadUint32(unknownChunk, 0x0);
            uint u11 = ReadUint32(unknownChunk, 0x4);
            uint u12 = ReadUint32(unknownChunk, 0xC);

            obj.Dependencies = ReadFileNameBlock(0x0, false);

            uint dataSize = ReadUint32();

            byte[] dataChunk = ReadChunk(dataSize - 0x4);
            uint u13 = ReadUint32(dataChunk, 0x0);

            uint u14 = ReadUint32(dataChunk, 0x20);
            uint u15 = ReadUint32(dataChunk, 0x24);

            uint u16 = ReadUint32(dataChunk, 0x2C);
            uint u17 = ReadUint32(dataChunk, 0x30);

            uint u18 = ReadUint32(dataChunk, 0x38);

            uint offset = u18 * 0xC;

            if (u18 != 0x3) {
                offset += (uint)(u18 == 0x5 ? 2 : 1); }
            
            obj.Type = (SceneObjectType)ReadUint32(dataChunk, 0x4B + offset);

            // Read transform data
            Transform transform = new Transform();
            if (obj.Type == SceneObjectType.Group) {
                //Console.WriteLine(obj.Name+" "+ReadVector3(dataChunk,0+offset));
                
                Vector2 u19 = ReadVector2(dataChunk, 0x4F + offset); // Unknown
                Vector3 u20 = ReadVector3(dataChunk, 0x5B + offset); // Unknown (valid v3)
                Vector3 u21 = ReadVector3(dataChunk, 0x68 + offset); // Unknown
                uint u22 = ReadUint32(dataChunk, 0x73 + offset);
                uint cOffset = 0x77;
                uint currentOffset = (uint)(_stream.Position-dataChunk.Length)+(offset+cOffset);
                uint currentLength = (uint)(dataChunk.Length - (offset + cOffset));
                if (u22 == 0) {
                    Vector3 u23 = ReadVector3(dataChunk,0x87+offset); // Unknown
                    transform.Position = ReadVector3(dataChunk, 0xB7 + offset); // Position?
                    obj.Group = ReadString(dataChunk,0x10B+offset,false);
                } else {
                    obj.Group = ReadString(dataChunk, 0xAF + offset, false);
                    //Console.WriteLine(obj.Name + " " + ToHex(currentOffset) + " " + ToHex(currentLength));
                    //Console.WriteLine(u22 + " " + ToHex(dataChunk, offset + 0x77));
                }
            } else if (obj.Type == SceneObjectType.Model) {
                uint u20 = ReadUint32(dataChunk, 0x4F + offset); // Unknown
                uint u21 = ReadUint32(dataChunk, 0x53 + offset); // Unknown

                transform.Scale = ReadVector3(dataChunk, 0x5B + offset); // Scale ?
                transform.Position = ReadVector3(dataChunk, 0x73 + offset); // Position ?
                transform.Rotation = ReadQuaternion(dataChunk, 0x9B + offset); // Quaternion ?
                obj.Group = ReadString(dataChunk, 0xC7 + offset, false);
            }

            Console.WriteLine(obj.Name + ": "+transform.Position+" "+ obj.Group);
            obj.Transform = transform;

            return obj;
        }
    }
}
