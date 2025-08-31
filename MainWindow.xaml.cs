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

        public MainWindow()
        {
            InitializeComponent();

            // initialize animator with the layer and speech controls
            string walkPath = @"C:\Users\lab_services_student\Desktop\PROG7312\vrikkie\walk.png";
            _mascotAnimator = new MascotAnimator(MascotCanvas, MascotLayer, MascotImage, walkPath, SpeechBorder, SpeechTextBox);
            _mascotAnimator.ShowSpeech("Hello — click the mascot to ask me to explain a component.", 4);
            _mascotAnimator.Start();
        }

        // clicking the mascot now starts "ask a component" flow (replaces btnMasc)
        private void MascotImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            awaitingComponentClick = true;
            _mascotAnimator.ShowSpeech("Wat jou want explained?", 0); // show until we get the response
        }

        private async void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!awaitingComponentClick) return;
            awaitingComponentClick = false;

            var clickedElement = GetClickedControl(e.OriginalSource);
            if (clickedElement == null)
            {
                // immediate quick feedback
                _mascotAnimator.ShowSpeech("I couldn't identify what you clicked.", 3);
                return;
            }

            string elementName = string.IsNullOrEmpty(clickedElement.Name) ? "(unnamed)" : clickedElement.Name;
            string elementType = clickedElement.GetType().Name;

            // Show immediate prompt then ask GPT
            _mascotAnimator.ShowSpeech("Thinking...", 0);
            

            var response = await _gptServices.ExplainClickedElementAsync(clickedElement);

            if (string.IsNullOrWhiteSpace(response))
                response = "(No response)";

            // play thrust animation (use thrust sprite) and when frames done, pause and show response for 10s
            string thrustPath = @"C:\Users\lab_services_student\Desktop\PROG7312\vrikkie\thrust.png";
            var thrustSprite = new BitmapImage(new Uri(thrustPath));

            _mascotAnimator.PlayCustomAnimation(thrustSprite, row: 2, startFrame: 7, endFrame: 0, callback: () =>
            {
                // after frames finished: pause walking and show response for 10s, then resume
                _mascotAnimator.PauseWalking();
                _mascotAnimator.ShowSpeech(response, 10, onHidden: () =>
                {
                    _mascotAnimator.ResumeWalking();
                });
            });
        }

        private FrameworkElement GetClickedControl(object originalSource)
        {
            var current = originalSource as System.Windows.DependencyObject;
            while (current != null && !(current is Control))
            {
                current = System.Windows.Media.VisualTreeHelper.GetParent(current);
            }
            return current as FrameworkElement;
        }

        private void btnReport_Click(object sender, RoutedEventArgs e) => MessageBox.Show("Report clicked!");
        private void btnEvents_Click(object sender, RoutedEventArgs e) => MessageBox.Show("Welcome! This is the Events button.");
        private void btnRequest_Click(object sender, RoutedEventArgs e) => MessageBox.Show("Request clicked!");
    }
}
