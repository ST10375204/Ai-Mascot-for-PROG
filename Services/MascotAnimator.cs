using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace PROG7312.Services
{
    public class MascotAnimator
    {
        private readonly Canvas _rootCanvas;
        private readonly Canvas _mascotLayer;
        private readonly Image _mascotImage;
        private readonly Border _speechBorder;
        private readonly TextBox _speechTextBox;

        private DispatcherTimer _animTimer;
        private DispatcherTimer _hideTimer;
        private DispatcherTimer _directionTimer;

        private BitmapImage _defaultSheet;
        private BitmapImage _activeSheet;

        private int _frameIndex = 0;
        private int _frameStart = 0;
        private int _frameEnd = 0;
        private int _frameStep = 1;

        private bool _customAnimation = false;
        private bool _pausedWalking = false;

        // Frame constants
        private const int FrameWidth = 64;
        private const int FrameHeight = 64;
        private const int FramesPerRow = 9;

        // Sprite sheet rows
        private const int RowUp = 0;
        private const int RowLeft = 1;
        private const int RowDown = 2;
        private const int RowRight = 3;
        private const int RowThrust = 4;

        private int _directionRow = RowRight;
        private double _speed = 2.5;

        private readonly Random _rng = new Random();

        // edge margin so bubble never clips off screen
        private const double EdgeMargin = 80;

        private Action _animationFinishedAction;

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

            _mascotImage.Source = new CroppedBitmap(_activeSheet, new Int32Rect(0, 0, FrameWidth, FrameHeight));

            _animTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(120) };
            _animTimer.Tick += Animate;

            // Timer to pick random direction every 3–6 seconds
            _directionTimer = new DispatcherTimer();
            _directionTimer.Interval = TimeSpan.FromSeconds(_rng.Next(3, 7));
            _directionTimer.Tick += (s, e) => PickRandomDirection();
        }

        public void Start()
        {
            _animTimer.Start();
            _directionTimer.Start();
        }

        public void Stop()
        {
            _animTimer.Stop();
            _directionTimer.Stop();
        }

        private void PickRandomDirection()
        {
            if (_customAnimation || _pausedWalking) return;

            int choice = _rng.Next(4); // 0=up,1=down,2=left,3=right
            switch (choice)
            {
                case 0: _directionRow = RowUp; break;
                case 1: _directionRow = RowDown; break;
                case 2: _directionRow = RowLeft; break;
                case 3: _directionRow = RowRight; break;
            }

            // reset timer randomly again
            _directionTimer.Interval = TimeSpan.FromSeconds(_rng.Next(3, 7));
        }

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

                var localCallback = callback;
                void finished()
                {
                    _activeSheet = _defaultSheet;
                    localCallback?.Invoke();
                }
                _animationFinishedAction = finished;
            });
        }

        private void Animate(object sender, EventArgs e)
        {
            double x = Canvas.GetLeft(_mascotLayer);
            if (double.IsNaN(x)) x = 0;
            double y = Canvas.GetTop(_mascotLayer);
            if (double.IsNaN(y)) y = 0;

            double layerW = _mascotLayer.ActualWidth > 0 ? _mascotLayer.ActualWidth : _mascotImage.Width;
            double layerH = _mascotLayer.ActualHeight > 0 ? _mascotLayer.ActualHeight : _mascotImage.Height;
            double canvasW = _rootCanvas.ActualWidth;
            double canvasH = _rootCanvas.ActualHeight;

            if (_customAnimation)
            {
                _mascotImage.Source = new CroppedBitmap(_activeSheet, new Int32Rect(_frameIndex * FrameWidth, _directionRow * FrameHeight, FrameWidth, FrameHeight));

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
                // Move according to current direction
                switch (_directionRow)
                {
                    case RowRight: x += _speed; break;
                    case RowLeft: x -= _speed; break;
                    case RowDown: y += _speed; break;
                    case RowUp: y -= _speed; break;
                }

                // Detect collisions with padding edges
                bool leftCollision = (x <= EdgeMargin);
                bool topCollision = (y <= EdgeMargin);
                bool rightCollision = (canvasW > 0) && (x + layerW >= canvasW - EdgeMargin);
                bool bottomCollision = (canvasH > 0) && (y + layerH >= canvasH - EdgeMargin);

                if (leftCollision || topCollision || rightCollision || bottomCollision)
                {
                    // Choose a new direction that moves away from collided edges.
                    // If multiple edges collided, pick one of the safe opposite directions at random.
                    _directionRow = PickDirectionAwayFromCollisions(leftCollision, topCollision, rightCollision, bottomCollision);

                    // Nudge the layer inside bounds to avoid repeated collision next tick
                    if (canvasW > 0)
                    {
                        x = Math.Max(EdgeMargin, Math.Min(x, canvasW - layerW - EdgeMargin));
                    }
                    else
                    {
                        x = Math.Max(EdgeMargin, x);
                    }

                    if (canvasH > 0)
                    {
                        y = Math.Max(EdgeMargin, Math.Min(y, canvasH - layerH - EdgeMargin));
                    }
                    else
                    {
                        y = Math.Max(EdgeMargin, y);
                    }
                }

                Canvas.SetLeft(_mascotLayer, x);
                Canvas.SetTop(_mascotLayer, y);

                _mascotImage.Source = new CroppedBitmap(_activeSheet, new Int32Rect(_frameIndex * FrameWidth, _directionRow * FrameHeight, FrameWidth, FrameHeight));
                _frameIndex = (_frameIndex + 1) % FramesPerRow;
            }
        }
        private int PickDirectionAwayFromCollisions(bool left, bool top, bool right, bool bottom)
        {
            var candidates = new List<int>();

            // If hit left, moving right is safe
            if (left) candidates.Add(RowRight);
            // If hit right, moving left is safe
            if (right) candidates.Add(RowLeft);
            // If hit top, moving down is safe
            if (top) candidates.Add(RowDown);
            // If hit bottom, moving up is safe
            if (bottom) candidates.Add(RowUp);

            // If nothing explicitly collided (shouldn't happen), fallback to a random direction
            if (candidates.Count == 0)
            {
                candidates.Add(RowLeft);
                candidates.Add(RowRight);
                candidates.Add(RowUp);
                candidates.Add(RowDown);
            }

            // If current direction is in candidates, prefer flipping to that opposite; otherwise random pick
            // (This helps ensure a clear bounce-back feel)
            int pick = candidates[_rng.Next(candidates.Count)];
            return pick;
        }


        // When clicked > thrust, pause, think, resume walk
        public void OnClicked()
        {
            PauseWalking();
            PlayCustomAnimation(_defaultSheet, RowThrust, 0, 4, () =>
            {
                // after thrust, pause for 2 seconds
                var pauseTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
                pauseTimer.Tick += (s, e) =>
                {
                    pauseTimer.Stop();
                    ShowSpeech("Hmm... let me think a bit...");
                    ResumeWalking();
                };
                pauseTimer.Start();
            });
        }

        public void ShowSpeech(string text, double durationSeconds = 2, Action onHidden = null)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (_speechTextBox == null || _speechBorder == null)
                {
                    onHidden?.Invoke();
                    return;
                }

                _speechTextBox.Text = string.IsNullOrWhiteSpace(text) ? "(no text)" : text;
                _speechBorder.Visibility = Visibility.Visible;

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
            if (_frameIndex >= FramesPerRow) _frameIndex = 0;
        }


    }
}
