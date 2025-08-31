using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace PROG7312.Services
{
    public class MascotAnimator
    {
        private readonly Canvas _rootCanvas;        // top-level canvas
        private readonly Canvas _mascotLayer;       // entire layer that moves (contains image + speech)
        private readonly Image _mascotImage;        // the image inside the layer
        private readonly Border _speechBorder;      // speech container (inside layer)
        private readonly TextBox _speechTextBox;    // authoritative textbox inside the border

        private DispatcherTimer _animTimer;
        private DispatcherTimer _hideTimer;

        private BitmapImage _defaultSheet;
        private BitmapImage _activeSheet;

        private int _frameIndex = 0;
        private int _directionRow = 3; // walking default row
        private int _frameStart = 0;
        private int _frameEnd = 0;
        private int _frameStep = 1;

        private bool _customAnimation = false;
        private bool _pausedWalking = false;

        private const int FrameWidth = 64;
        private const int FrameHeight = 64;
        private const int FramesPerRow = 9;

        private double _speed = 3;
        private bool _movingRight = true;

        public MascotAnimator(Canvas rootCanvas, Canvas mascotLayer, Image mascotImage, string defaultSpritePath,
                              Border speechBorder = null, TextBox speechTextBox = null)
        {
            _rootCanvas = rootCanvas ?? throw new ArgumentNullException(nameof(rootCanvas));
            _mascotLayer = mascotLayer ?? throw new ArgumentNullException(nameof(mascotLayer));
            _mascotImage = mascotImage ?? throw new ArgumentNullException(nameof(mascotImage));

            _speechBorder = speechBorder;
            _speechTextBox = speechTextBox;

            _defaultSheet = new BitmapImage(new Uri(defaultSpritePath));
            _activeSheet = _defaultSheet;

            // set initial image
            _mascotImage.Source = new CroppedBitmap(_activeSheet, new Int32Rect(0, 0, FrameWidth, FrameHeight));

            _animTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(120) };
            _animTimer.Tick += Animate;
        }

        public void Start() => _animTimer.Start();
        public void Stop() => _animTimer.Stop();

        // Play custom animation frames on provided sprite sheet.
        // callback invoked when frames finish (on UI thread).
        public void PlayCustomAnimation(BitmapImage spriteSheet, int row, int startFrame, int endFrame, Action callback = null)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _activeSheet = spriteSheet ?? _defaultSheet;

                _customAnimation = true;
                _frameIndex = startFrame;
                _frameStart = startFrame;
                _frameEnd = endFrame;
                _frameStep = startFrame < endFrame ? 1 : -1;
                _directionRow = row;

                // Wrap callback to restore sprite and ensure callback executes on finish
                var localCallback = callback;
                void finished()
                {
                    _activeSheet = _defaultSheet;
                    localCallback?.Invoke();
                }

                // set animation finished action
                _animationFinishedAction = finished;
            });
        }

        // internal action invoked when custom animation finishes
        private Action _animationFinishedAction;

        private void Animate(object sender, EventArgs e)
        {
            // get mascot layer coords (we move the entire layer)
            double x = Canvas.GetLeft(_mascotLayer);
            if (double.IsNaN(x)) x = 0;
            double y = Canvas.GetTop(_mascotLayer);
            if (double.IsNaN(y)) y = 0;

            if (_customAnimation)
            {
                var rect = new Int32Rect(_frameIndex * FrameWidth, _directionRow * FrameHeight, FrameWidth, FrameHeight);
                _mascotImage.Source = new CroppedBitmap(_activeSheet, rect);

                if (_frameIndex == _frameEnd)
                {
                    _customAnimation = false;
                    _animationFinishedAction?.Invoke();
                    _animationFinishedAction = null;
                }
                else
                {
                    _frameIndex += _frameStep;
                }
            }
            else if (!_pausedWalking)
            {
                // Move the whole mascot layer left/right
                if (_movingRight)
                {
                    x += _speed;
                    if (x + (_mascotLayer.ActualWidth > 0 ? _mascotLayer.ActualWidth : _mascotImage.Width) >= _rootCanvas.ActualWidth)
                        _movingRight = false;
                }
                else
                {
                    x -= _speed;
                    if (x <= 0) _movingRight = true;
                }

                Canvas.SetLeft(_mascotLayer, x);

                // walking frame (we still use active sheet for frames)
                var rectWalk = new Int32Rect(_frameIndex * FrameWidth, _directionRow * FrameHeight, FrameWidth, FrameHeight);
                _mascotImage.Source = new CroppedBitmap(_activeSheet, rectWalk);

                _frameIndex = (_frameIndex + 1) % FramesPerRow;
            }

            // speech box lives inside the layer so no need to reposition; if needed, we could tweak offsets here.
        }

        /// <summary>
        /// Shows speech text inside the speech TextBox (it is inside the layer and thus moves with the mascot).
        /// durationSeconds=0 means show until explicitly hidden.
        /// onHidden invoked when the speech hides.
        /// </summary>
        public void ShowSpeech(string text, double durationSeconds = 2, Action onHidden = null)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (_speechTextBox == null || _speechBorder == null)
                {
                    onHidden?.Invoke();
                    return;
                }

                // set text
                _speechTextBox.Text = string.IsNullOrWhiteSpace(text) ? "(no text)" : text;

                // show border (it is inside layer so already moves with mascot)
                _speechBorder.Visibility = Visibility.Visible;

                // cancel previous hide timer
                _hideTimer?.Stop();
                _hideTimer = null;

                if (durationSeconds > 0)
                {
                    _hideTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(durationSeconds) };
                    _hideTimer.Tick += (s, e) =>
                    {
                        _hideTimer.Stop();
                        _hideTimer = null;
                        _speechBorder.Visibility = Visibility.Collapsed;
                        onHidden?.Invoke();
                    };
                    _hideTimer.Start();
                }
            });
        }

        public void HideSpeech()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _hideTimer?.Stop();
                _hideTimer = null;
                if (_speechBorder != null) _speechBorder.Visibility = Visibility.Collapsed;
            });
        }

        public void PauseWalking() => _pausedWalking = true;
        public void ResumeWalking()
        {
            _pausedWalking = false;
            _activeSheet = _defaultSheet;
        }
    }
}
