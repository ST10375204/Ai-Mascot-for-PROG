using Microsoft.Win32;
using System.Windows;
using System.Windows.Documents;

namespace PROG7312
{
    public partial class ReportWindow : Window
    {
        private ReportQueue reports;           // Our FIFO queue
        private string selectedImagePath;      // Stores selected image path

        public ReportWindow()
        {
            InitializeComponent();

            reports = new ReportQueue();

            // Populate sample categories
            lstCategory.Items.Add(new ReportItem("", "Pothole", "", ""));
            lstCategory.Items.Add(new ReportItem("", "Graffiti", "", ""));
            lstCategory.Items.Add(new ReportItem("", "Streetlight", "", ""));
        }

        // Select an image file
        private void imgFile_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg";

            if (dlg.ShowDialog() == true)
            {
                selectedImagePath = dlg.FileName;
                MessageBox.Show($"Selected image: {selectedImagePath}");
            }
        }

        // Submit report via imgSubmit
        private void imgSubmit_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (lstCategory.SelectedItem == null)
            {
                MessageBox.Show("Please select a category.");
                return;
            }

            string location = txtLocation.Text;

            // Get text from RichTextBox
            TextRange tr = new TextRange(rchDesc.Document.ContentStart, rchDesc.Document.ContentEnd);
            string description = tr.Text.Trim();

            string category = (lstCategory.SelectedItem as ReportItem).Category;

            // Create new report
            ReportItem report = new ReportItem(location, category, description, selectedImagePath ?? "");

            // Enqueue into FIFO queue
            reports.Enqueue(report);

            MessageBox.Show($"Report submitted:\nCategory: {category}\nLocation: {location}");

            // Reset UI
            txtLocation.Text = "";
            rchDesc.Document.Blocks.Clear();
            rchDesc.Document.Blocks.Add(new Paragraph(new Run("")));
            lstCategory.SelectedItem = null;
            selectedImagePath = null;
        }
    }
}
