// 此文件主体由 AI 生成，应作为独立模块，尽量减少与其他内容的耦合。
// 使用 ModernTooltipService 接管后，TooltipService 仅有 IsEnabled、ShowOnDisabled、InitialShowDelay 仍然有效，其他属性不再生效。

using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Media3D;
using System.Windows.Threading;

namespace MeloongCore.Wpf;
public static class ModernTooltipService {

    // 控制是否使用现代化 Tooltip 渲染；默认为 true。是否允许显示 Tooltip 仍由 ToolTipService.IsEnabled 控制。
    public static void SetUseModernTooltip(DependencyObject element, bool value) => element.SetValue(UseModernTooltipProperty, value);
    public static bool GetUseModernTooltip(DependencyObject element) => (bool) element.GetValue(UseModernTooltipProperty);

    // 控制 Tooltip 是否跟随鼠标移动；默认为 true。
    public static void SetFollowMouse(DependencyObject element, bool value) => element.SetValue(FollowMouseProperty, value);
    public static bool GetFollowMouse(DependencyObject element) => (bool) element.GetValue(FollowMouseProperty);

    /// <summary>
    /// 全局启用现代 Tooltip。启用后会接管所有 <see cref="FrameworkElement.ToolTip"/>。
    /// </summary>
    internal static void Init() {
        if (initialized) return;
        initialized = true;

        EventManager.RegisterClassHandler(typeof(FrameworkElement), UIElement.MouseEnterEvent, new MouseEventHandler(OnMouseEnter), true);
        EventManager.RegisterClassHandler(typeof(FrameworkElement), UIElement.MouseMoveEvent, new MouseEventHandler(OnMouseMove), true);
        EventManager.RegisterClassHandler(typeof(FrameworkElement), UIElement.MouseLeaveEvent, new MouseEventHandler(OnMouseLeave), true);
        EventManager.RegisterClassHandler(typeof(FrameworkElement), UIElement.PreviewMouseUpEvent, new MouseButtonEventHandler(OnPreviewMouseUp), true);
        EventManager.RegisterClassHandler(typeof(FrameworkElement), ToolTipService.ToolTipOpeningEvent, new ToolTipEventHandler(OnToolTipOpening), true);
        EventManager.RegisterClassHandler(typeof(FrameworkElement), FrameworkElement.UnloadedEvent, new RoutedEventHandler(static (sender, _) => {
            if (sender is FrameworkElement owner && ReferenceEquals(owner, currentOwner)) Close(false);
        }), true);

        // Loaded 负责常规路径；PreviewMouseDown 兜底处理 Init 晚于控件 Loaded 的情况。
        EventManager.RegisterClassHandler(typeof(ComboBox), FrameworkElement.LoadedEvent, new RoutedEventHandler(static (sender, _) => HookComboBoxDropDown(sender as ComboBox)), true);
        EventManager.RegisterClassHandler(typeof(ComboBox), UIElement.PreviewMouseDownEvent, new MouseButtonEventHandler(static (sender, _) => HookComboBoxDropDown(sender as ComboBox)), true);
    }

    #region 字段与属性

    // 附加属性
    public static readonly DependencyProperty UseModernTooltipProperty = DependencyProperty.RegisterAttached(
        "UseModernTooltip", typeof(bool), typeof(ModernTooltipService), new PropertyMetadata(true));
    public static readonly DependencyProperty FollowMouseProperty = DependencyProperty.RegisterAttached(
        "FollowMouse", typeof(bool), typeof(ModernTooltipService), new PropertyMetadata(true));
    private static readonly DependencyProperty IsComboBoxDropDownHookedProperty = DependencyProperty.RegisterAttached(
        "IsComboBoxDropDownHooked", typeof(bool), typeof(ModernTooltipService), new PropertyMetadata(false));

    // 常量
    private const double closedScale = 0.97;
    private const int shadowRadius = 18, contentMaxWidth = 676;
    private static readonly Thickness contentMargin = new(12, 11, 12, 8);
    private static readonly Brush backgroundBrush = new SolidColorBrush(Colors.White);
    private static readonly Brush borderBrush = new SolidColorBrush(Color.FromRgb(0xD6, 0xD6, 0xD6));
    private static readonly Brush foregroundBrush = new SolidColorBrush(Color.FromRgb(0x52, 0x52, 0x52));
    private static readonly DependencyPropertyDescriptor toolTipPropertyDescriptor =
        DependencyPropertyDescriptor.FromProperty(FrameworkElement.ToolTipProperty, typeof(FrameworkElement));

    // 状态字段
    private static readonly DispatcherTimer openTimer = new();
    private static bool initialized;
    private static int animationToken;
    private static Point lastMousePoint;
    private static Popup? popup;
    private static Border? popupCard;
    private static ScaleTransform? popupScale;
    private static FrameworkElement? currentOwner;
    private static FrameworkElement? observedToolTipOwner;
    private static ToolTip? borrowedToolTip;
    private static object? borrowedContent;

    static ModernTooltipService() {
        backgroundBrush.Freeze();
        borderBrush.Freeze();
        foregroundBrush.Freeze();

        openTimer.Tick += (_, _) => {
            openTimer.Stop();
            if (currentOwner is not null) Show(currentOwner, lastMousePoint);
        };
    }

    #endregion

    #region 事件入口

    private static void OnMouseEnter(object sender, MouseEventArgs e) {
        if (sender is not FrameworkElement reference) return;
        reference.Dispatcher.BeginInvoke(new Action(() => RefreshCurrentOwner(reference)), DispatcherPriority.Input);
    }

    private static void OnMouseMove(object sender, MouseEventArgs e) {
        if (sender is not FrameworkElement reference) return;

        if (Mouse.LeftButton == MouseButtonState.Pressed) {
            if (currentOwner is FrameworkElement pressedOwner) {
                lastMousePoint = Mouse.GetPosition(pressedOwner);
                if (GetFollowMouse(pressedOwner) && popup is { IsOpen: true }) {
                    UpdatePosition(pressedOwner, lastMousePoint);
                }
            } else if (popup is { IsOpen: true, PlacementTarget: FrameworkElement popupOwner } && GetFollowMouse(popupOwner)) {
                lastMousePoint = Mouse.GetPosition(popupOwner);
                UpdatePosition(popupOwner, lastMousePoint);
            }
            return;
        }

        RefreshCurrentOwner(reference);
        if (currentOwner is FrameworkElement owner) {
            lastMousePoint = Mouse.GetPosition(owner);
            if (GetFollowMouse(owner) && popup is { IsOpen: true }) UpdatePosition(owner, lastMousePoint);
            return;
        }

        if (popup is { IsOpen: true, PlacementTarget: FrameworkElement closingOwner } && GetFollowMouse(closingOwner)) {
            lastMousePoint = Mouse.GetPosition(closingOwner);
            UpdatePosition(closingOwner, lastMousePoint);
        }
    }

    private static void OnMouseLeave(object sender, MouseEventArgs e) {
        if (sender is not FrameworkElement owner || !ReferenceEquals(owner, currentOwner)) return;

        if (!(owner.IsEnabled || !ToolTipService.GetShowOnDisabled(owner) || !IsPointInside(owner, Mouse.GetPosition(owner)))) return;

        var nextOwner = FindCurrentTooltipOwner(owner);
        if (nextOwner is not null && !ReferenceEquals(nextOwner, owner)) {
            BeginShow(nextOwner, Mouse.GetPosition(nextOwner));
            return;
        }

        Close(true);
    }

    private static void OnPreviewMouseUp(object sender, MouseButtonEventArgs e) {
        if (sender is not FrameworkElement reference) return;

        reference.Dispatcher.BeginInvoke(new Action(() => {
            if (currentOwner is null) return;

            var owner = FindCurrentTooltipOwner(reference);
            if (owner is null) {
                Close(true);
            } else {
                BeginShow(owner, Mouse.GetPosition(owner));
            }
        }), DispatcherPriority.Input);
    }

    private static void OnToolTipOpening(object sender, ToolTipEventArgs e) {
        if (sender is not FrameworkElement owner || !CanShowModernTooltip(owner) || !TryGetToolTip(owner, out _, out _)) return;

        e.Handled = true;
        if (ShouldSuppressTooltip(owner)) {
            Close(false);
            return;
        }

        // 启用状态控件走 MouseEnter 延迟逻辑；禁用控件只能从原生 Opening 事件接管。
        if (owner.IsEnabled) return;

        if (!ReferenceEquals(currentOwner, owner)) HideImmediately();
        currentOwner = owner;
        openTimer.Stop();
        lastMousePoint = Mouse.GetPosition(owner);
        Show(owner, lastMousePoint);
    }

    #endregion

    #region Tooltip 目标解析

    private static void RefreshCurrentOwner(FrameworkElement reference) {
        var owner = FindCurrentTooltipOwner(reference);
        if (currentOwner is { IsEnabled: false } disabledOwner &&
            ToolTipService.GetShowOnDisabled(disabledOwner) &&
            IsPointInside(disabledOwner, Mouse.GetPosition(disabledOwner))) {
            return;
        }

        if (ShouldSuppressTooltip(owner)) {
            // 鼠标捕获仍在当前拥有者分支内时，保留现有气泡，避免拖选或按压时闪烁。
            if (currentOwner is not null &&
                Mouse.Captured is DependencyObject captured &&
                IsSameTreeBranch(captured, currentOwner) &&
                IsPointInside(currentOwner, Mouse.GetPosition(currentOwner))) {
                return;
            }

            Close(false);
            return;
        }

        if (owner is not null) {
            BeginShow(owner, Mouse.GetPosition(owner));
        } else if (currentOwner is { IsEnabled: false }) {
            Close(true); // 禁用的控件不会触发 MouseLeave，需要由其他元素收到的 MouseMove 主动关闭
        }
    }

    private static FrameworkElement? FindCurrentTooltipOwner(FrameworkElement reference) {
        static FrameworkElement? FindOwnerInAncestors(DependencyObject? start) {
            for (var current = start; current is not null; current = GetParent(current)) {
                if (current is FrameworkElement owner && CanShowModernTooltip(owner) && TryGetToolTip(owner, out _, out _)) {
                    return owner;
                }
            }

            return null;
        }

        var owner = FindOwnerInAncestors(Mouse.DirectlyOver as DependencyObject);
        if (owner is not null) return owner;

        return reference.InputHitTest(Mouse.GetPosition(reference)) is DependencyObject hit
            ? FindOwnerInAncestors(hit)
            : null;
    }

    private static bool CanShowModernTooltip(FrameworkElement owner) =>
        GetUseModernTooltip(owner) && ToolTipService.GetIsEnabled(owner) && (owner.IsEnabled || ToolTipService.GetShowOnDisabled(owner));

    private static bool TryGetToolTip(FrameworkElement owner, out object? content, out ToolTip? sourceToolTip) {
        var raw = owner.ToolTip;
        sourceToolTip = raw as ToolTip;
        content = sourceToolTip is null
            ? raw
            : sourceToolTip.Content ?? (ReferenceEquals(sourceToolTip, borrowedToolTip) ? borrowedContent : null);
        return content is not null && (content is not string text || text.Length > 0);
    }

    private static bool ShouldSuppressTooltip(FrameworkElement? owner) {
        if (Mouse.LeftButton == MouseButtonState.Pressed || Mouse.Captured is null) return false;
        if (Mouse.Captured is not DependencyObject captured) return true;
        if (owner is null) return true;

        return !IsSameTreeBranch(captured, owner);
    }

    private static bool IsPointInside(FrameworkElement owner, Point point) =>
        point.X >= 0 && point.Y >= 0 && point.X <= owner.ActualWidth && point.Y <= owner.ActualHeight;

    private static bool IsSameTreeBranch(DependencyObject first, DependencyObject second) {
        if (ReferenceEquals(first, second)) return true;

        for (var current = GetParent(second); current is not null; current = GetParent(current)) {
            if (ReferenceEquals(current, first)) return true;
        }

        for (var current = GetParent(first); current is not null; current = GetParent(current)) {
            if (ReferenceEquals(current, second)) return true;
        }

        return false;
    }

    private static DependencyObject? GetParent(DependencyObject current) {
        if (current is Visual or Visual3D && VisualTreeHelper.GetParent(current) is { } visualParent) return visualParent;

        return LogicalTreeHelper.GetParent(current);
    }

    #endregion

    #region 显示生命周期

    private static void BeginShow(FrameworkElement owner, Point point) {
        if (!CanShowModernTooltip(owner) || !TryGetToolTip(owner, out _, out _)) return;
        if (ShouldSuppressTooltip(owner)) {
            Close(false);
            return;
        }

        if (owner is ComboBox comboBox) HookComboBoxDropDown(comboBox);
        if (ReferenceEquals(currentOwner, owner)) {
            lastMousePoint = point;
            if (popup is not { IsOpen: true } && !openTimer.IsEnabled) StartOpenTimer(owner);
            return;
        }

        HideImmediately();
        currentOwner = owner;
        lastMousePoint = point;
        StartOpenTimer(owner);
    }

    private static void StartOpenTimer(FrameworkElement owner) {
        openTimer.Stop();

        var delay = Math.Max(0, ToolTipService.GetInitialShowDelay(owner));
        if (delay == 0) {
            Show(owner, lastMousePoint);
            return;
        }

        openTimer.Interval = TimeSpan.FromMilliseconds(delay);
        openTimer.Start();
    }

    private static void Show(FrameworkElement owner, Point point) {
        if (!ReferenceEquals(owner, currentOwner) || !CanShowModernTooltip(owner) || !TryGetToolTip(owner, out var content, out var sourceToolTip)) return;

        if (popup is null) {
            popupScale = new ScaleTransform(closedScale, closedScale);
            popupCard = new Border {
                Background = backgroundBrush, BorderBrush = borderBrush, BorderThickness = new(1),
                CornerRadius = new(8), MaxWidth = 700, SnapsToDevicePixels = true, UseLayoutRounding = true,
                RenderTransform = popupScale, RenderTransformOrigin = new(0, 0),
                Effect = new DropShadowEffect { Opacity = 0.15, BlurRadius = shadowRadius, ShadowDepth = 0, Color = Colors.Black }
            };

            var root = new Grid { Margin = new(shadowRadius + 1), SnapsToDevicePixels = true, UseLayoutRounding = true };
            root.Children.Add(popupCard);

            popup = new Popup {
                AllowsTransparency = true, IsHitTestVisible = false, StaysOpen = true, PopupAnimation = PopupAnimation.None,
                Placement = PlacementMode.Relative, Child = root
            };
        }

        popup!.PlacementTarget = owner;
        UpdatePosition(owner, point);
        ObserveToolTip(owner);
        UpdateContent(owner, content!, sourceToolTip);

        animationToken++;
        popupCard!.Opacity = 0;
        popupScale!.ScaleX = closedScale;
        popupScale.ScaleY = closedScale;
        popup.IsOpen = true;
        Animate(1, 1, null);
    }

    private static void UpdateContent(FrameworkElement owner, object content, ToolTip? sourceToolTip) {
        popupCard!.DataContext = sourceToolTip?.DataContext ?? owner.DataContext;
        popupCard.FlowDirection = owner.FlowDirection;

        // 先还原上一次借出的 Visual，再挂载新的内容，避免同一元素拥有两个逻辑父级。
        popupCard.Child = null;
        RestoreBorrowedContent();

        if (sourceToolTip is not null && content is DependencyObject contentObject && ReferenceEquals(LogicalTreeHelper.GetParent(contentObject), sourceToolTip)) {
            borrowedToolTip = sourceToolTip;
            borrowedContent = content;
            sourceToolTip.Content = null;
        }

        var hasTemplate = sourceToolTip?.ContentTemplate is not null || sourceToolTip?.ContentTemplateSelector is not null;
        popupCard.Child = content is string stringContent && !hasTemplate
            ? new TextBlock {
                Text = stringContent, TextWrapping = TextWrapping.Wrap, Foreground = foregroundBrush,
                Margin = contentMargin, FontSize = 12.5, LineHeight = 17, MaxWidth = contentMaxWidth
            }
            : new ContentPresenter {
                Content = content, ContentTemplate = sourceToolTip?.ContentTemplate, ContentTemplateSelector = sourceToolTip?.ContentTemplateSelector,
                ContentStringFormat = sourceToolTip?.ContentStringFormat, Margin = contentMargin, MaxWidth = contentMaxWidth
            };
    }

    private static void ObserveToolTip(FrameworkElement owner) {
        if (ReferenceEquals(observedToolTipOwner, owner)) return;
        StopObservingToolTip();
        observedToolTipOwner = owner;
        toolTipPropertyDescriptor.AddValueChanged(owner, OnToolTipChanged);
    }

    private static void StopObservingToolTip() {
        if (observedToolTipOwner is null) return;
        toolTipPropertyDescriptor.RemoveValueChanged(observedToolTipOwner, OnToolTipChanged);
        observedToolTipOwner = null;
    }

    private static void OnToolTipChanged(object? sender, EventArgs e) {
        if (sender is not FrameworkElement owner || !ReferenceEquals(owner, currentOwner) || popup is not { IsOpen: true }) return;
        if (!CanShowModernTooltip(owner) || !TryGetToolTip(owner, out var content, out var sourceToolTip)) {
            Close(true);
            return;
        }

        UpdateContent(owner, content!, sourceToolTip);
    }

    private static void Close(bool animated) {
        openTimer.Stop();
        currentOwner = null;
        StopObservingToolTip();

        if (popup is not { IsOpen: true } || popupCard is null || !animated) {
            HideImmediately();
            return;
        }

        var token = ++animationToken;
        Animate(0, closedScale, (_, _) => {
            if (token != animationToken) return;
            HideImmediately();
        });
    }

    private static void HideImmediately() {
        openTimer.Stop();
        animationToken++;
        StopObservingToolTip();

        if (popup is not null) popup.IsOpen = false;
        if (popupCard is not null) {
            popupCard.BeginAnimation(UIElement.OpacityProperty, null);
            popupCard.Child = null;
        }

        if (popupScale is not null) {
            popupScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            popupScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
        }

        RestoreBorrowedContent();
    }

    private static void RestoreBorrowedContent() {
        if (borrowedToolTip is null) return;

        borrowedToolTip.Content = borrowedContent;
        borrowedToolTip = null;
        borrowedContent = null;
    }

    #endregion

    #region Popup 渲染与动画

    private static void UpdatePosition(FrameworkElement owner, Point point) {
        popup!.PlacementTarget = owner;
        popup.HorizontalOffset = Math.Round(point.X + 15);
        popup.VerticalOffset = Math.Round(point.Y + 25);
    }

    private static void Animate(double opacity, double scale, EventHandler? completed) {
        var duration = new Duration(TimeSpan.FromMilliseconds(70));
        var opacityAnimation = new DoubleAnimation(opacity, duration) {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        if (completed is not null) opacityAnimation.Completed += completed;

        var scaleAnimation = new DoubleAnimation(scale, duration) {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };

        popupCard!.BeginAnimation(UIElement.OpacityProperty, opacityAnimation, HandoffBehavior.SnapshotAndReplace);
        popupScale!.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation, HandoffBehavior.SnapshotAndReplace);
        popupScale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation, HandoffBehavior.SnapshotAndReplace);
    }

    #endregion

    // WPF 原生 Tooltip 会在 ComboBox 下拉弹出时关闭；这里保持同样行为。
    private static void HookComboBoxDropDown(ComboBox? comboBox) {
        if (comboBox is null || (bool) comboBox.GetValue(IsComboBoxDropDownHookedProperty)) return;
        comboBox.SetValue(IsComboBoxDropDownHookedProperty, true);
        comboBox.DropDownOpened += static (_, _) => { if (currentOwner is not null) Close(true); };
    }

}
