using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace InfiniteCanvas
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        double _pixelOverflow = 200d;

        public MainPage()
        {
            this.InitializeComponent();
            SizeChanged += MainPage_SizeChanged;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Inker.InkPresenter.StrokesCollected += InkPresenter_StrokesCollected;
        }

        private void InkPresenter_StrokesCollected(Windows.UI.Input.Inking.InkPresenter sender, Windows.UI.Input.Inking.InkStrokesCollectedEventArgs args)
        {
            AdjustCanvasSize();
        }

        private void MainPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.NewSize.Height + _pixelOverflow > Root.ActualHeight)
                Root.Height = e.NewSize.Height + _pixelOverflow;
            if (e.NewSize.Width + _pixelOverflow > Root.ActualWidth)
                Root.Width = e.NewSize.Width + _pixelOverflow;
        }

        private void MainCanvas_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (e.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Pen)
                return;

            var position = e.GetPosition(Root);

            List<UIElement> elements = new List<UIElement>(VisualTreeHelper.FindElementsInHostCoordinates(position, MainCanvas));
            if (elements.Count > 0)
                return;

            RichEditBox box = new RichEditBox();
            box.Width = Root.Width > 500 ? Math.Min(Root.ActualWidth / 2, 500) : Root.Width;
            box.LostFocus += Box_LostFocus;
            box.SizeChanged += Box_SizeChanged;
            box.Style = App.Current.Resources["InfiniteRichEditBoxStyle"] as Style;

            Canvas.SetLeft(box, position.X);
            Canvas.SetTop(box, position.Y);

            MainCanvas.Children.Add(box);
            box.Focus(FocusState.Keyboard);

            AdjustCanvasSize();
        }

        private void Box_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            AdjustCanvasSize();
        }

        private void Box_LostFocus(object sender, RoutedEventArgs e)
        {
            var box = sender as RichEditBox;
            box.Document.GetText(Windows.UI.Text.TextGetOptions.None, out var value);

            if (string.IsNullOrWhiteSpace(value))
            {
                box.LostFocus -= Box_LostFocus;
                box.SizeChanged -= Box_SizeChanged;
                MainCanvas.Children.Remove(box);
            }
        }

        private void AdjustCanvasSize()
        {
            // find outmost bounds
            var maxLeft = Root.ActualWidth - _pixelOverflow;
            var maxTop = Root.ActualHeight - _pixelOverflow;

            foreach (var child in MainCanvas.Children)
            {
                var element = child as FrameworkElement;
                var point = child.TransformToVisual(Root).TransformPoint(new Point());
                if (point.X + element.ActualWidth > maxLeft) maxLeft = point.X + element.ActualWidth;
                if (point.Y + element.ActualHeight > maxTop) maxTop = point.Y + element.ActualHeight;
            }

            var inkBounds = Inker.InkPresenter.StrokeContainer.BoundingRect;
            if (inkBounds.Width + inkBounds.Left > maxLeft) maxLeft = inkBounds.Width + inkBounds.Left;
            if (inkBounds.Height + inkBounds.Top > maxTop) maxTop = inkBounds.Height + inkBounds.Top;

            Root.Width = maxLeft + _pixelOverflow;
            Root.Height = maxTop + _pixelOverflow;
        }
    }
}
