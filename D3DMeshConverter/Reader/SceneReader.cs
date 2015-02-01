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
using System.IO;
using System.Windows.Forms;

namespace JPAssetReader {
    class SceneReader : BaseReader, IMeshReader {
        public class SceneObject {
            public string Name;
            public DependencyList Modules;
            public List<DependencyList> Dependencies;
            public Transform Transform;
            public List<SceneObject> children = new List<SceneObject>();
        }

        public string Name;
        public List<SceneObject> Objects;

        public override bool Read(FileStream stream) {
            base.Read(stream);
            ReadChunk(0x1);

            // Read the scene name
            Name = ReadString();

            uint u1 = ReadUint32();
            ReadDependencyBlock(0x0, false);
            ReadChunk(0x4C);

            Objects = new List<SceneObject>();

            uint objectCount = ReadUint32();
            uint i = 0;

            List<SceneObject> objectList = new List<SceneObject>();
            for (i=0; i < objectCount; i++) {
                SceneObject sceneObject = ReadObject();
                objectList.Add(sceneObject);
            }

            // Construct transform hierarchy
            foreach (SceneObject sceneObject in objectList) {
                SceneObject parent = null;
                if (sceneObject.Dependencies.Count == 1 && sceneObject.Dependencies[0].Objects.Count == 1) {
                    string groupName = sceneObject.Dependencies[0].Objects[0];
                    parent = objectList.Find(obj => obj.Name.Equals(groupName));

                    if (parent != null) {
                        parent.children.Add(sceneObject);
                    }
                    else {
                        Objects.Add(sceneObject);
                    }
                }
                else {
                    Objects.Add(sceneObject);
                }
            }

            return true;
        }

        private SceneObject ReadObject() {
            string objectName = ReadString();

            SceneObject obj = new SceneObject() { Name = objectName };

            //Console.WriteLine(objectName);

            ReadChunk(0x10);
            
            obj.Modules = ReadDependencyBlock(0x0, false);

            //CurrentPos();
            //Console.WriteLine("Size: " + ToHex(dataSize));
            ObjectMeta meta = ReadObjectMeta();
            obj.Dependencies = meta.Dependencies;
            obj.Transform = meta.transform;

            return obj;
        }

        private Mesh StripMesh(string propName, string objectName) {
            Mesh mesh = null;
            JurassicReader r = new JurassicReader();
            if (r.Read(resourcePath + "\\" + propName)) {
                List<DependencyList> dependencies = (r.reader as PropReader).Dependencies;
                foreach (DependencyList dependencyList in dependencies) {
                    foreach (string entry in dependencyList.Objects) {
                        // Only load d3dmeshes, for now :)
                        if (Common.GetExtension(entry).Equals("d3dmesh")) {
                            string meshPath = resourcePath + "\\" + entry;
                            if (!File.Exists(meshPath)) {
                                MessageBox.Show("Couldn't find "+entry+"! Please navigate to the JP resource directory.","ERROR!");
                                AdjustResourcePath();
                                //Console.WriteLine(entry + " does not exist - skipping");
                                continue; }

                            JurassicReader modelReader = new JurassicReader();
                            if (modelReader.Read(meshPath)) {
                                Mesh propMesh = modelReader.mesh;
                                if (propMesh == null) {
                                    continue; }

                                if (mesh == null) {
                                    mesh = propMesh;
                                } else {
                                    mesh.Combine(propMesh); }
                            }
                        }
                    }
                }
            }

            return mesh;
        }

        private Mesh ConstructMesh(SceneObject obj, Matrix matrix) {
            Mesh mesh = null;

            // Make sure to apply the transformations in the right order (TRS)
            matrix.Translate(obj.Transform.Position);
            matrix.Rotate(obj.Transform.Rotation);
            matrix.Scale(obj.Transform.Scale);
            
            foreach (string file in obj.Modules.Objects) {
                if (Common.GetExtension(file).Equals("prop")) {
                    string propPath = resourcePath + "\\" + file;
                    if (!File.Exists(propPath)) {
                        MessageBox.Show("Couldn't find " + file + "! Please navigate to the JP resource directory.", "ERROR!");
                        AdjustResourcePath(); }

                    Mesh m = StripMesh(file, obj.Name);
                        
                    if (m == null) {
                        continue; }

                    if (mesh == null) {
                        mesh = m;
                    } else {
                        mesh.Combine(m); }
                }
            }

            // Transform the mesh before adding the children
            if (mesh != null) {
                mesh.Transform(matrix); }

            foreach (SceneObject sceneObject in obj.children) {
                Mesh childMesh = ConstructMesh(sceneObject, matrix);

                if (childMesh == null) {
                    continue; }

                if (mesh == null) {
                    mesh = childMesh;
                } else {
                    mesh.Combine(childMesh); }
            }

            return mesh;
        }

        public Mesh GetMesh() {
            Mesh m = null;

            resourcePath = Common.GetPath(_stream.Name);
            foreach (SceneObject obj in Objects) {
                Mesh propMesh = ConstructMesh(obj, Matrix.identity);
                
                if (propMesh == null) {
                    continue; }

                if (m == null) {
                    m = propMesh;
                } else {
                    m.Combine(propMesh); }
            }

            return m;
        }
    }
}
