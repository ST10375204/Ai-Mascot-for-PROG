using Microsoft.Win32;
using PROG7312.Services;
using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace PROG7312
{
    public partial class ReportWindow : Window
    {
        private ReportQueue reports;
        private string selectedImagePath;
        private MainWindow mWindow;

        private readonly GPTServices _gptServices = new GPTServices();
        private MascotAnimator _mascotAnimator;
        private bool awaitingComponentClick = false;

        // ButtonAnimators
        private ButtonAnimator submitAnimator;
        private ButtonAnimator filePickerAnimator;
        private ButtonAnimator backAnimator;

        public event Action<ReportItem> ReportSubmitted;
        public ReportWindow()
        {
            InitializeComponent();

            string walkPath = @"C:\Users\lab_services_student\Desktop\PROG7312\vrikkie\walk.png";
            _mascotAnimator = new MascotAnimator(MascotCanvas, MascotLayer, MascotImage, walkPath, SpeechBorder, SpeechTextBox);

            _mascotAnimator.ShowSpeech("This is the Report page, where you can report issues!", 5);
            _mascotAnimator.Start();
            reports = new ReportQueue();

            // Populate sample categories
            lstCategory.Items.Add(new ReportItem("", "Pothole", "", ""));
            lstCategory.Items.Add(new ReportItem("", "Graffiti", "", ""));
            lstCategory.Items.Add(new ReportItem("", "Streetlight", "", ""));
            lstCategory.Items.Add(new ReportItem("", "Vagrant", "", ""));

            // Initialize ButtonAnimators
            // Button sprite sheet path
            string spritePath = @"C:\Users\lab_services_student\Desktop\PROG7312\Assets\button.png";

            // Frame definitions
            var defaultFrame = new Int32Rect(50, 158, 190, 112);
            var clickedFrame = new Int32Rect(245, 158, 190, 112);

            // Setup button animators
            submitAnimator = new ButtonAnimator(btnSubmitImage, spritePath, defaultFrame, clickedFrame);
            filePickerAnimator = new ButtonAnimator(btnFilePickerImage, spritePath, defaultFrame, clickedFrame);
            backAnimator = new ButtonAnimator(btnBackImage, spritePath, defaultFrame, clickedFrame);  }


        private void HandleButtonClick(ButtonAnimator animator, Action action)
        {
            animator.AnimateClick();
            action?.Invoke();
        }

        private void btnSubmit_Click(object sender, RoutedEventArgs e)
        {
            if (!awaitingComponentClick)
            {
                HandleButtonClick(submitAnimator, SubmitReport);
            }

        }

        private void btnFilePicker_Click(object sender, RoutedEventArgs e)
        {
            if (!awaitingComponentClick)
            {
                HandleButtonClick(filePickerAnimator, PickFile);
            }
            
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            if (!awaitingComponentClick) {
                mWindow = new MainWindow();
                mWindow.Show();
                this.Close();
            }
      
        }

        private void SubmitReport()
        {
            if (lstCategory.SelectedItem == null)
            {
                MessageBox.Show("Please select a category.");
                return;
            }

            string location = txtLocation.Text;
            TextRange tr = new TextRange(rchDesc.Document.ContentStart, rchDesc.Document.ContentEnd);
            string description = tr.Text.Trim();
            string category = (lstCategory.SelectedItem as ReportItem).Category;

            var report = new ReportItem(location, category, description, selectedImagePath ?? "");
            reports.Enqueue(report);

            // Notify the main window
            ReportSubmitted?.Invoke(report);

            MessageBox.Show($"Report submitted:\nCategory: {category}\nLocation: {location}");
            Close();
        }

        private void PickFile()
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg";

            if (dlg.ShowDialog() == true)
            {
                selectedImagePath = dlg.FileName;
                MessageBox.Show($"Selected image: {selectedImagePath}");
            }
        }

        private void MascotImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            awaitingComponentClick = true;
            _mascotAnimator.PauseWalking();

            string thrustPath = @"C:\Users\lab_services_student\Desktop\PROG7312\vrikkie\thrust.png";
            var thrustSprite = new BitmapImage(new Uri(thrustPath));

            _mascotAnimator.PlayCustomAnimation(thrustSprite, row: 2, startFrame: 7, endFrame: 0, callback: () =>
            {
                _mascotAnimator.ShowSpeech("Wat jou want explained?", 0);
            });
        }

        private async void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!awaitingComponentClick) return;

            var clickedElement = _gptServices.ResolveControl(e.OriginalSource);
            if (clickedElement == null)
            {
                _mascotAnimator.ShowSpeech("I couldn’t identify what you clicked.", 3);
                _mascotAnimator.ResumeWalking();
                return;
            }

            _mascotAnimator.ShowSpeech("Thinking...", 0);

            var response = await _gptServices.ExplainClickedElementAsync(clickedElement);
            awaitingComponentClick = false;
            if (string.IsNullOrWhiteSpace(response)) response = "(No response)";

            _mascotAnimator.ShowSpeech(response, 10, onHidden: () =>
            {
                _mascotAnimator.ResumeWalking();
            });
        }

    }
}
