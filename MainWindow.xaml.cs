using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using PROG7312.Services;

namespace PROG7312
{
    public partial class MainWindow : Window
    {
        private readonly GPTServices _gptServices = new GPTServices();
        private MascotAnimator _mascotAnimator;
        private bool awaitingComponentClick = false;
        private ReportWindow reportWindow;

        public MainWindow()
        {
            InitializeComponent();

            // initialize animator with the layer and speech controls
            string walkPath = @"C:\Users\lab_services_student\Desktop\PROG7312\vrikkie\walk.png";
            _mascotAnimator = new MascotAnimator(MascotCanvas, MascotLayer, MascotImage, walkPath, SpeechBorder, SpeechTextBox);

            // Greeting
            _mascotAnimator.ShowSpeech("Hullo, I am Oom Vrikkie, feel free to ask me things by clicking on me!", 5);
            _mascotAnimator.Start();
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

            // pause + thinking bubble
            _mascotAnimator.ShowSpeech("Thinking...", 0);

            // GPT explain
            var response = await _gptServices.ExplainClickedElementAsync(clickedElement);
            awaitingComponentClick = false;
            if (string.IsNullOrWhiteSpace(response))
                response = "(No response)";

            // show result then resume
            _mascotAnimator.ShowSpeech(response, 10, onHidden: () =>
            {
                _mascotAnimator.ResumeWalking();
            });
        }
        private void btnReport_Click(object sender, RoutedEventArgs e)
        {
            if (awaitingComponentClick)
            {
                return;
            }
            else {
                reportWindow = new ReportWindow();
                this.Hide();
                reportWindow.ShowDialog();

            }
            
        }
        private void btnEvents_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Still in Development!"); 
        }
        private void btnRequest_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Still in Development!");
        }
    }
}
