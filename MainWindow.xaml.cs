using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using PROG7312.Services;
using System.Threading.Tasks;
using System.Windows.Media;

namespace PROG7312
{
    public partial class MainWindow : Window
    {
        private readonly GPTServices _gptServices = new GPTServices();
        private MascotAnimator _mascotAnimator;
        private ButtonAnimator btnReportAnimator;
        private ButtonAnimator btnEventsAnimator;
        private ButtonAnimator btnRequestAnimator;

        private bool awaitingComponentClick = false;
        private ReportWindow reportWindow;
        private NoteBoardManager _noteBoard;


        public MainWindow()
        {
            InitializeComponent();

            // Setup mascot
            string walkPath = @"C:\Users\lab_services_student\Desktop\PROG7312\vrikkie\walk.png";
            _mascotAnimator = new MascotAnimator(MascotCanvas, MascotLayer, MascotImage, walkPath, SpeechBorder, SpeechTextBox);

            _mascotAnimator.ShowSpeech("Hullo, I am Oom Vrikkie, feel free to ask me things by clicking on me!", 5);
            _mascotAnimator.Start();

            // Button sprite sheet path
            string spritePath = @"C:\Users\lab_services_student\Desktop\PROG7312\Assets\button.png";

            // Frame definitions
            var defaultFrame = new Int32Rect(50, 158, 190, 112);
            var clickedFrame = new Int32Rect(245, 158, 190, 112);

            // Setup button animators
            btnReportAnimator = new ButtonAnimator(btnReportImage, spritePath, defaultFrame, clickedFrame);
            btnEventsAnimator = new ButtonAnimator(btnEventsImage, spritePath, defaultFrame, clickedFrame);
            btnRequestAnimator = new ButtonAnimator(btnRequestImage, spritePath, defaultFrame, clickedFrame);

            //init the noteboard manager
            _noteBoard = new NoteBoardManager(NotesCanvas);
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
        private void btnReport_Click(object sender, RoutedEventArgs e)
        {
            btnReportAnimator.AnimateClick();

            if (!awaitingComponentClick)
            {
                reportWindow = new ReportWindow();
                reportWindow.ReportSubmitted += _noteBoard.AddReportToBoard;
                this.Hide();
                reportWindow.ShowDialog();
                this.Show();
            }
        }

        private void btnEvents_Click(object sender, RoutedEventArgs e)
        {
            btnEventsAnimator.AnimateClick();
            MessageBox.Show("Still in Development!");
        }

        private void btnRequest_Click(object sender, RoutedEventArgs e)
        {
            btnRequestAnimator.AnimateClick();
            MessageBox.Show("Still in Development!");
        }
    }
}
