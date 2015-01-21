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
using System.IO;
using System.Windows.Forms;

namespace JPAssetReader {
    public partial class Form1 : Form {
        private AssetFileDialog fileDialog;

        public Form1() {
            InitializeComponent();
            fileDialog = new AssetFileDialog(System.AppDomain.CurrentDomain.BaseDirectory);
        }

        private JurassicReader ReadFile(string fileName) {
            JurassicReader reader = new JurassicReader();
            if (!reader.Read(fileDialog.FileName)) {
                MessageBox.Show(fileDialog.FileName+"\n\nERROR: File encountered an error while parsing!", "Export failed!");
                return null;
            }

            string extension = Common.GetExtension(fileDialog.FileName);

            return reader; 

            if (reader.reader is IMeshReader) {
                Mesh mesh = reader.mesh;

                // Output the mesh data to a Wavefront OBJ
                String meshOutputName = fileDialog.FileName.Replace("." + Common.GetExtension(fileDialog.FileName), ".obj");
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
            if (!fileDialog.Open()) {
                return;
            }

            // Parse the model data
            ReadFile(fileDialog.FileName);
        }

        private void OnMassRead(object sender, EventArgs e) {
            if (!fileDialog.Open()) {
                return;
            }

            string fileName = fileDialog.FileName;
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
