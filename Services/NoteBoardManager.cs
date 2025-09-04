using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;

namespace PROG7312.Services
{
    public class NoteBoardManager
    {
        private readonly Canvas _boardCanvas;
        private readonly Random _rng = new Random();
        private readonly List<Rect> _placedNotes = new List<Rect>();

        private const double NoteWidth = 150;
        private const double NoteHeight = 175;

        public NoteBoardManager(Canvas boardCanvas)
        {
            _boardCanvas = boardCanvas;
        }

        public void AddReportToBoard(ReportItem report)
        {
            // Root container for one note
            var noteRoot = new Grid
            {
                Width = NoteWidth,
                Height = NoteHeight
            };

            // Note background
            var noteBg = new Image
            {
                Source = new BitmapImage(new Uri("pack://application:,,,/Assets/note.png")),
                Stretch = Stretch.Fill,
                IsHitTestVisible = false
            };
            noteRoot.Children.Add(noteBg);

            // Content area
            var contentHost = new Grid
            {
                Margin = new Thickness(
                    NoteWidth * 0.12,
                    NoteHeight * 0.18,
                    NoteWidth * 0.12,
                    NoteHeight * 0.12
                ),
                IsHitTestVisible = true
            };

            var viewbox = new Viewbox { Stretch = Stretch.Fill };
            contentHost.Children.Add(viewbox);

            var content = new StackPanel { Orientation = Orientation.Vertical };

            var cat = new TextBlock
            {
                Text = report.Category,
                FontWeight = FontWeights.Bold,
                FontSize = 10,
                TextWrapping = TextWrapping.Wrap
            };

            var loc = new TextBlock
            {
                Text = report.Location,
                FontStyle = FontStyles.Italic,
                FontSize = 9,
                Margin = new Thickness(0, 2, 0, 4),
                TextWrapping = TextWrapping.Wrap
            };

            var desc = new TextBlock
            {
                Text = report.Description,
                FontSize = 9,
                TextWrapping = TextWrapping.Wrap,
                TextTrimming = TextTrimming.WordEllipsis,
                MaxHeight = 40
            };

            content.Children.Add(cat);
            content.Children.Add(loc);
            content.Children.Add(desc);
            viewbox.Child = content;

            noteRoot.Children.Add(contentHost);

            // Add a slight tilt
            noteRoot.RenderTransformOrigin = new Point(0.5, 0.1);
            noteRoot.RenderTransform = new RotateTransform(_rng.Next(-5, 6));

            // Position with overlap-check
            PlaceNoteRandomly(noteRoot);

            _boardCanvas.Children.Add(noteRoot);
        }

        private void PlaceNoteRandomly(UIElement note)
        {
            double boardW = _boardCanvas.ActualWidth;
            double boardH = _boardCanvas.ActualHeight;

            int attempts = 0;
            Rect newRect;
            do
            {
                double x = _rng.Next(20, (int)Math.Max(21, boardW - NoteWidth - 20));
                double y = _rng.Next(20, (int)Math.Max(21, boardH - NoteHeight - 90));

                newRect = new Rect(x, y, NoteWidth, NoteHeight);

                // Break after too many tries (prevents infinite loop if board too crowded)
                attempts++;
                if (attempts > 50) break;

            } while (OverlapsTooMuch(newRect));

            Canvas.SetLeft(note, newRect.X);
            Canvas.SetTop(note, newRect.Y);

            _placedNotes.Add(newRect);
        }

        private bool OverlapsTooMuch(Rect candidate)
        {
            foreach (var existing in _placedNotes)
            {
                Rect intersect = Rect.Intersect(existing, candidate);
                double overlapArea = intersect.Width * intersect.Height;
                double candidateArea = candidate.Width * candidate.Height;

                if (overlapArea > candidateArea * 0.3) // allow ≤30% overlap
                    return true;
            }
            return false;
        }
    }
}
