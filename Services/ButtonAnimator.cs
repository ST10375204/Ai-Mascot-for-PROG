using System;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows;

namespace PROG7312.Services
{
    public class ButtonAnimator
    {
        private readonly Image _buttonImage;
        private readonly BitmapImage _sprite;
        private readonly Int32Rect _defaultFrame;
        private readonly Int32Rect _clickedFrame;

        public ButtonAnimator(Image buttonImage, string spritePath, Int32Rect defaultFrame, Int32Rect clickedFrame)
        {
            _buttonImage = buttonImage;
            _sprite = new BitmapImage(new Uri(spritePath, UriKind.Absolute));
            _defaultFrame = defaultFrame;
            _clickedFrame = clickedFrame;

            // Set initial frame
            _buttonImage.Source = new CroppedBitmap(_sprite, _defaultFrame);
        }

        public async void AnimateClick()
        {
            _buttonImage.Source = new CroppedBitmap(_sprite, _clickedFrame);
            await Task.Delay(150);
            _buttonImage.Source = new CroppedBitmap(_sprite, _defaultFrame);
        }
    }
}
