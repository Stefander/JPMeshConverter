using System;
using System.IO;
using System.Windows.Forms;

namespace JPMeshConverter {
    public partial class Form1 : Form {
        private ModelFileDialog fileDialog;

        public Form1() {
            InitializeComponent();
            fileDialog = new ModelFileDialog(System.AppDomain.CurrentDomain.BaseDirectory);
        }

        private void OnConvert(object sender, EventArgs e) {
            // Show the user a file dialog
            if (!fileDialog.Open()) {
                return;
            }

            // Parse the model data
            D3DReader reader = new D3DReader(fileDialog.FileName);

            // If the model couldn't be parsed correctly, error and return
            Mesh mesh = reader.MeshData;
            if (reader.MeshData == null) {
                MessageBox.Show("ERROR: Model could not be parsed.","Export failed!");
                return;
            }

            // Output the mesh data to a Wavefront OBJ
            String outputName = fileDialog.FileName.Replace(".d3dmesh", ".obj");
            File.WriteAllText(outputName, mesh.ToString());

            // Show the user it worked
            MessageBox.Show(outputName+"\n\nProcessed "+mesh.Chunks.Count+" chunks.\n"+mesh.Vertices.Count + " vertices exported.", "Export successful!");
        }
    }
}
