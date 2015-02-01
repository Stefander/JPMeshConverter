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
    public partial class Form1 : Form {
        private OpenResourceDialog resourceDialog;
        private OpenResourceDialog textureDialog;

        public Form1() {
            InitializeComponent();
            string baseDirectory = System.AppDomain.CurrentDomain.BaseDirectory;
            resourceDialog = new OpenResourceDialog(baseDirectory, "Open JP Resource", "JP Resource Files|*.d3dmesh;*.lang;*.prop;*.scene;*.skl|D3D Mesh|*.d3dmesh|Localization|*.lang|All files (*.*)|*.*");
            textureDialog = new OpenResourceDialog(baseDirectory, "Open JP texture directory","JP Textures|*.dds|All files (*.*)|*.*");
        }

        private JurassicReader ReadFile(string fileName) {
            JurassicReader reader = new JurassicReader();
            if (!reader.Read(resourceDialog.FileName)) {
                MessageBox.Show(resourceDialog.FileName+"\n\nERROR: File encountered an error while parsing!", "Export failed!");
                return null;
            }

            string resourceName = resourceDialog.FileName;
            string extension = Common.GetExtension(resourceName);
            
            if (reader.reader is IMeshReader) {
                Mesh mesh = reader.mesh;

                if (mesh == null) {
                    return reader; }

                // Fetch missing textures
                string texType = "dds";
                List<string> missingTextures = new List<string>();
                string path = Common.GetPath(resourceName);
                foreach (string texture in mesh.GetTextures()) {
                    if (texture.StartsWith("color_")) {
                        continue; }

                    if (!File.Exists(path + "\\" + texture + "."+texType)) {
                        missingTextures.Add(texture);
                    }
                }

                if (missingTextures.Count > 0) {
                    MessageBox.Show("Missing "+missingTextures.Count+" texture"+(missingTextures.Count > 1 ? "s" : "")+"! Please specify the texture directory.");
                    string destinationDir = Common.GetPath(resourceDialog.FileName);
                    if (textureDialog.Open()) {
                        string texturePath = Common.GetPath(textureDialog.FileName);
                        foreach(string texture in missingTextures) {
                            string textureName = texture+"."+texType;
                            if(!File.Exists(texturePath+"\\"+textureName)) {
                                MessageBox.Show("Couldn't locate "+textureName+"!");
                                continue;
                            }
                            File.Copy(texturePath+"\\"+textureName,destinationDir+"\\"+textureName);
                        }
                    }
                }

                // Output the mesh data to a Wavefront OBJ
                String meshOutputName = resourceName.Replace("." + Common.GetExtension(resourceName), ".obj");
                String mtlOutputName = meshOutputName.Replace(".obj", ".mtl");
                String mtlName = mtlOutputName.Substring(mtlOutputName.LastIndexOf("\\") + 1);
                File.WriteAllText(meshOutputName, mesh.GetObjData(mtlName));
                File.WriteAllText(mtlOutputName, mesh.GetMtlData());

                // Show the user it worked
                MessageBox.Show(meshOutputName + "\n\nProcessed " + mesh.Chunks.Count + " chunks.\n" + mesh.Vertices.Count + " vertices exported.", "Export successful!");
            }

            return reader;
        }

        private void OnConvert(object sender, EventArgs e) {
            // Show the user a file dialog
            if (!resourceDialog.Open()) {
                return;
            }

            // Parse the model data
            ReadFile(resourceDialog.FileName);
        }

        private void OnMassRead(object sender, EventArgs e) {
            if (!resourceDialog.Open()) {
                return;
            }

            string fileName = resourceDialog.FileName;
            string fileExtension = Common.GetExtension(fileName);
            string fileDirectory = Common.GetPath(fileName);

            foreach (string file in Directory.GetFiles(fileDirectory)) {
                string extension = Common.GetExtension(file);

                // Only process files with the same extension, for neatness
                if (fileExtension.Equals(extension)) {
                    JurassicReader r = new JurassicReader();
                    r.Read(file);
                }
            }
        }
    }
}
