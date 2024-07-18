/*using System.Security.Policy;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows;

public class DragAdorner : Adorner
{
    private VisualBrush _brush;
    private double _leftOffset;
    private double _topOffset;

    public DragAdorner(UIElement adornedElement, UIElement feedback) : base(adornedElement)
    {
        _brush = new VisualBrush(feedback);
        IsHitTestVisible = false;
    }

    public void UpdatePosition(double left, double top)
    {
        _leftOffset = left;
        _topOffset = top;
        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        drawingContext.DrawRectangle(_brush, null, new Rect(_leftOffset, _topOffset, AdornedElement.RenderSize.Width, AdornedElement.RenderSize.Height));
    }
}*/

