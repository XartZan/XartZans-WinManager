using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using XartZans.WinManager.App.Services;
using XartZans.WinManager.App.ViewModels;

namespace XartZans.WinManager.App;

public partial class MainWindow : Window
{
    private const double LanguageMenuCollapsedSize = 48;
    private const double LanguageMenuExpandedWidth = 286;
    private readonly MainViewModel _viewModel;
    private bool _isClosingLanguageMenu;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel(new SystemSnapshotService(), App.Logger);
        DataContext = _viewModel;
        LanguagePopup.DataContext = _viewModel;
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void Maximize_Click(object sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void LanguageButton_Click(object sender, RoutedEventArgs e)
    {
        if (LanguagePopup.IsOpen)
        {
            CloseLanguageMenu();
            return;
        }

        _viewModel.RefreshLanguagesCommand.Execute(null);
        LanguagePopup.PlacementTarget = LanguageButton;
        LanguageMenuBody.Opacity = 0;
        LanguageMenuCard.BeginAnimation(FrameworkElement.WidthProperty, null);
        LanguageMenuCard.BeginAnimation(FrameworkElement.HeightProperty, null);
        LanguageMenuCard.Width = LanguageMenuCollapsedSize;
        LanguageMenuCard.Height = LanguageMenuCollapsedSize;
        LanguagePopup.IsOpen = true;
        LanguagePopup.VerticalOffset = 3;
        LanguagePopup.HorizontalOffset = LanguageButton.ActualWidth - LanguageMenuCollapsedSize;

        var expandWidth = new DoubleAnimation(LanguageMenuCollapsedSize, LanguageMenuExpandedWidth, TimeSpan.FromMilliseconds(190))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
            FillBehavior = FillBehavior.Stop
        };
        expandWidth.Completed += (_, _) =>
        {
            LanguageMenuCard.BeginAnimation(FrameworkElement.WidthProperty, null);
            LanguageMenuCard.Width = LanguageMenuExpandedWidth;
            LanguageMenuBody.Opacity = 1;

            var itemCount = Math.Max(1, _viewModel.Languages.Count);
            var menuHeight = Math.Min(440, 108 + Math.Min(itemCount, 7) * 44);
            AnimateMenuSize(FrameworkElement.HeightProperty, menuHeight, TimeSpan.FromMilliseconds(210), null);
        };
        LanguageMenuCard.BeginAnimation(FrameworkElement.WidthProperty, expandWidth);
    }

    private void LanguageOption_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: LanguageOption language })
        {
            LocalizationService.SelectLanguage(language.Code);
        }

        CloseLanguageMenu();
    }

    private void OpenLanguageFolder_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.OpenLanguagesFolderCommand.Execute(null);
        CloseLanguageMenu();
    }

    private void Window_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (!LanguagePopup.IsOpen || _isClosingLanguageMenu ||
            IsDescendantOf(e.OriginalSource as DependencyObject, LanguageButton) ||
            IsDescendantOf(e.OriginalSource as DependencyObject, LanguageMenuCard))
        {
            return;
        }

        CloseLanguageMenu();
    }

    private void LanguageMenuCard_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (LanguagePopup.IsOpen)
        {
            LanguagePopup.HorizontalOffset = LanguageButton.ActualWidth - e.NewSize.Width;
        }
    }

    private void CloseLanguageMenu()
    {
        if (!LanguagePopup.IsOpen || _isClosingLanguageMenu)
        {
            return;
        }

        _isClosingLanguageMenu = true;
        AnimateMenuSize(FrameworkElement.HeightProperty, LanguageMenuCollapsedSize, TimeSpan.FromMilliseconds(140), () =>
        {
            LanguageMenuBody.Opacity = 0;
            AnimateMenuSize(FrameworkElement.WidthProperty, LanguageMenuCollapsedSize, TimeSpan.FromMilliseconds(150), () =>
            {
                LanguagePopup.IsOpen = false;
                _isClosingLanguageMenu = false;
            });
        });
    }

    private void AnimateMenuSize(DependencyProperty property, double target, TimeSpan duration, Action? completed)
    {
        var start = property == FrameworkElement.WidthProperty ? LanguageMenuCard.ActualWidth : LanguageMenuCard.ActualHeight;
        LanguageMenuCard.BeginAnimation(property, null);
        LanguageMenuCard.SetCurrentValue(property, start);

        var animation = new DoubleAnimation(start, target, duration)
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut },
            FillBehavior = FillBehavior.Stop
        };
        animation.Completed += (_, _) =>
        {
            LanguageMenuCard.BeginAnimation(property, null);
            LanguageMenuCard.SetCurrentValue(property, target);
            completed?.Invoke();
        };
        LanguageMenuCard.BeginAnimation(property, animation);
    }

    private static bool IsDescendantOf(DependencyObject? source, DependencyObject ancestor)
    {
        while (source is not null)
        {
            if (ReferenceEquals(source, ancestor))
            {
                return true;
            }

            source = source is Visual or System.Windows.Media.Media3D.Visual3D
                ? System.Windows.Media.VisualTreeHelper.GetParent(source)
                : LogicalTreeHelper.GetParent(source);
        }

        return false;
    }

    private void Window_Closed(object? sender, EventArgs e) => _viewModel.Dispose();
}
