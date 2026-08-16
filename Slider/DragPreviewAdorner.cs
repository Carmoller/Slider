using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace Slider
{
    internal class DragPreviewAdorner : Adorner
    {
        private readonly VisualBrush _brush;
        private Point _mouseCoords;
        private readonly Size _tileSize;

        public DragPreviewAdorner(UIElement adornedElement, FrameworkElement tileElement)
            : base(adornedElement)
        {
            // 1. Create a live visual mirror of the exact tile
            _brush = new VisualBrush(tileElement);
            _tileSize = tileElement.RenderSize;
            IsHitTestVisible = false; // Bypasses hit testing so it doesn't block drops
        }

        public void UpdatePosition(Point point)
        {
            _mouseCoords = point;
            InvalidateVisual(); // Forces WPF to instantly redraw the tile at the new position
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            // Center the 38x38 baseline rectangle perfectly on the mouse coordinates.
            // Because the Adorner layer lives inside the Viewbox tree, WPF automatically
            // blows this 38x38 box up to match the visual size of your list items!
            Rect rect = new Rect(_mouseCoords.X - (_tileSize.Width / 2),
                                 _mouseCoords.Y - (_tileSize.Height / 2),
                                 _tileSize.Width, _tileSize.Height);

            drawingContext.DrawRectangle(_brush, null, rect);
        }
    }
}
