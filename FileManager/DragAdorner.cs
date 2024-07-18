using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;
using System;

public class DragAdorner : Adorner
{
    private readonly Border border;
    private readonly StackPanel panel;
    private readonly TextBlock textBlock;
    private readonly Image image;
    private Point currentPosition;

    public DragAdorner(UIElement adornedElement, string initialText, string folderIconPath, string fileIconPath, bool isFolder)
        : base(adornedElement)
    {
        // Determine the correct icon based on the item type
        string iconPath = isFolder ? folderIconPath : fileIconPath;

        // Creating a StackPanel to hold the image and text
        panel = new StackPanel
        {
            Orientation = Orientation.Horizontal
        };

        // Creating and configuring the Image
        image = new Image
        {
            Source = new BitmapImage(new Uri(iconPath, UriKind.RelativeOrAbsolute)),
            Width = 16, // Set desired width
            Height = 16, // Set desired height
            Margin = new Thickness(0, 0, 5, 0) // Margin between image and text
        };

        // Creating and configuring the TextBlock
        textBlock = new TextBlock
        {
            Text = initialText,
            FontSize = 14
        };

        // Adding the Image and TextBlock to the StackPanel
        panel.Children.Add(image);
        panel.Children.Add(textBlock);

        // Wrapping the StackPanel in a Border to add padding
        border = new Border
        {
            Padding = new Thickness(5),
            Background = Brushes.LightYellow,
            Opacity = 0.8,
            Child = panel
        };

        this.AddVisualChild(border);
        this.IsHitTestVisible = false;
    }

    public void UpdateText(string text)
    {
        textBlock.Text = text;
        this.InvalidateVisual();
    }

    public void UpdatePosition(Point position)
    {
        currentPosition = position;
        this.InvalidateVisual();
    }

    protected override Size MeasureOverride(Size constraint)
    {
        border.Measure(constraint);
        return border.DesiredSize;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        border.Arrange(new Rect(currentPosition, border.DesiredSize));
        return finalSize;
    }

    protected override Visual GetVisualChild(int index)
    {
        return border;
    }

    protected override int VisualChildrenCount => 1;
}