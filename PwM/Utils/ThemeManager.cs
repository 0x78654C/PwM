using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;

namespace PwM.Utils
{
    [SupportedOSPlatform("Windows")]
    public static class ThemeManager
    {
        public const string LightMode = "Light";
        public const string DarkMode = "Dark";

        private const string ThemeRegistryValue = "ThemeMode";
        private static bool s_initialized;

        private static readonly Dictionary<string, string> LightPalette = new()
        {
            ["AccentBrush"] = "#6366F1",
            ["AccentHoverBrush"] = "#4F46E5",
            ["AccentPressedBrush"] = "#4338CA",
            ["AppBackgroundBrush"] = "#F8FAFC",
            ["SurfaceBrush"] = "#FFFFFF",
            ["SurfaceAltBrush"] = "#F8FAFC",
            ["SurfaceHoverBrush"] = "#F1F5F9",
            ["SidebarBrush"] = "#0F172A",
            ["SidebarNavTextBrush"] = "#C7D2E3",
            ["SidebarNavDisabledTextBrush"] = "#7B8797",
            ["SidebarNavSelectedTextBrush"] = "#FFFFFF",
            ["SidebarNavSelectionBrush"] = "#4F46E5",
            ["SidebarVersionTextBrush"] = "#91A3BA",
            ["TitleBarButtonBrush"] = "#182235",
            ["TitleBarButtonHoverBrush"] = "#27364D",
            ["TitleBarButtonBorderBrush"] = "#40516B",
            ["TitleBarButtonForegroundBrush"] = "#D3DCE9",
            ["TextPrimaryBrush"] = "#0F172A",
            ["TextSecondaryBrush"] = "#64748B",
            ["TextTertiaryBrush"] = "#94A3B8",
            ["BorderBrush"] = "#E2E8F0",
            ["InputBorderBrush"] = "#CBD5E1",
            ["AccentSurfaceBrush"] = "#EEF2FF",
            ["AccentTextBrush"] = "#4338CA",
            ["DangerBrush"] = "#DC2626",
            ["DangerSurfaceBrush"] = "#FEF2F2",
            ["DangerHoverBrush"] = "#FEE2E2",
            ["DangerPressedBrush"] = "#FECACA",
            ["DangerTextBrush"] = "#991B1B",
            ["WarningSurfaceBrush"] = "#FFF7ED",
            ["WarningBorderBrush"] = "#FED7AA",
            ["WarningTextBrush"] = "#C2410C",
            ["SuccessTextBrush"] = "#15803D",
        };

        private static readonly Dictionary<string, string> DarkPalette = new()
        {
            ["AccentBrush"] = "#818CF8",
            ["AccentHoverBrush"] = "#6366F1",
            ["AccentPressedBrush"] = "#4F46E5",
            ["AppBackgroundBrush"] = "#090E1A",
            ["SurfaceBrush"] = "#111827",
            ["SurfaceAltBrush"] = "#1E293B",
            ["SurfaceHoverBrush"] = "#263449",
            ["SidebarBrush"] = "#0F172A",
            ["SidebarNavTextBrush"] = "#C7D2E3",
            ["SidebarNavDisabledTextBrush"] = "#7B8797",
            ["SidebarNavSelectedTextBrush"] = "#FFFFFF",
            ["SidebarNavSelectionBrush"] = "#4F46E5",
            ["SidebarVersionTextBrush"] = "#91A3BA",
            ["TitleBarButtonBrush"] = "#182235",
            ["TitleBarButtonHoverBrush"] = "#27364D",
            ["TitleBarButtonBorderBrush"] = "#40516B",
            ["TitleBarButtonForegroundBrush"] = "#D3DCE9",
            ["TextPrimaryBrush"] = "#F8FAFC",
            ["TextSecondaryBrush"] = "#CBD5E1",
            ["TextTertiaryBrush"] = "#94A3B8",
            ["BorderBrush"] = "#334155",
            ["InputBorderBrush"] = "#475569",
            ["AccentSurfaceBrush"] = "#1E1B4B",
            ["AccentTextBrush"] = "#C7D2FE",
            ["DangerBrush"] = "#F87171",
            ["DangerSurfaceBrush"] = "#450A0A",
            ["DangerHoverBrush"] = "#7F1D1D",
            ["DangerPressedBrush"] = "#991B1B",
            ["DangerTextBrush"] = "#FCA5A5",
            ["WarningSurfaceBrush"] = "#431407",
            ["WarningBorderBrush"] = "#7C2D12",
            ["WarningTextBrush"] = "#FDBA74",
            ["SuccessTextBrush"] = "#4ADE80",
        };

        private static readonly Dictionary<Color, Color> DarkForegroundMap = CreateColorMap(
            ("#0F172A", "#F8FAFC"),
            ("#334155", "#E2E8F0"),
            ("#475569", "#CBD5E1"),
            ("#64748B", "#94A3B8"),
            ("#991B1B", "#FCA5A5"),
            ("#B91C1C", "#FCA5A5"),
            ("#15803D", "#4ADE80"),
            ("#C2410C", "#FDBA74"));

        private static readonly Dictionary<Color, Color> DarkBackgroundMap = CreateColorMap(
            ("#FFFFFF", "#111827"),
            ("#F8FAFC", "#1E293B"),
            ("#F1F5F9", "#263449"),
            ("#EEF2FF", "#1E1B4B"),
            ("#FEF2F2", "#450A0A"),
            ("#FEE2E2", "#7F1D1D"),
            ("#FECACA", "#991B1B"),
            ("#FFF7ED", "#431407"));

        private static readonly Dictionary<Color, Color> DarkBorderMap = CreateColorMap(
            ("#E2E8F0", "#334155"),
            ("#CBD5E1", "#475569"),
            ("#F1F5F9", "#1E293B"),
            ("#FED7AA", "#7C2D12"));

        private static readonly Dictionary<Color, Color> LightForegroundMap = Reverse(DarkForegroundMap);
        private static readonly Dictionary<Color, Color> LightBackgroundMap = Reverse(DarkBackgroundMap);
        private static readonly Dictionary<Color, Color> LightBorderMap = Reverse(DarkBorderMap);

        public static bool IsDarkTheme { get; private set; }
        public static string CurrentMode => IsDarkTheme ? DarkMode : LightMode;

        public static void Initialize()
        {
            if (s_initialized)
                return;

            s_initialized = true;
            EventManager.RegisterClassHandler(
                typeof(Window),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(Window_Loaded),
                true);

            Apply(LoadMode(), false);
        }

        public static void Apply(string mode, bool persist = true)
        {
            IsDarkTheme = string.Equals(mode, DarkMode, StringComparison.OrdinalIgnoreCase);
            var palette = IsDarkTheme ? DarkPalette : LightPalette;

            foreach (var entry in palette)
                Application.Current.Resources[entry.Key] = CreateBrush(entry.Value);

            foreach (Window window in Application.Current.Windows)
                ApplyToWindow(window);

            if (persist)
            {
                RegistryManagement.RegKey_CreateKey(
                    PwMLib.GlobalVariables.registryPath,
                    ThemeRegistryValue,
                    CurrentMode);
            }
        }

        private static string LoadMode()
        {
            var mode = RegistryManagement.RegKey_Read(
                "HKEY_CURRENT_USER\\" + PwMLib.GlobalVariables.registryPath,
                ThemeRegistryValue);

            return string.Equals(mode, DarkMode, StringComparison.OrdinalIgnoreCase)
                ? DarkMode
                : LightMode;
        }

        private static void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is Window window)
                ApplyToWindow(window);
        }

        private static void ApplyToWindow(Window window)
        {
            var visited = new HashSet<DependencyObject>();
            ApplyElement(window, visited, false);
        }

        private static void ApplyElement(
            DependencyObject element,
            HashSet<DependencyObject> visited,
            bool preserveThemeForeground)
        {
            if (element is null || !visited.Add(element))
                return;

            var foregroundMap = IsDarkTheme ? DarkForegroundMap : LightForegroundMap;
            var backgroundMap = IsDarkTheme ? DarkBackgroundMap : LightBackgroundMap;
            var borderMap = IsDarkTheme ? DarkBorderMap : LightBorderMap;
            preserveThemeForeground |= element is FrameworkElement frameworkElement &&
                string.Equals(
                    frameworkElement.Tag as string,
                    "PreserveThemeForeground",
                    StringComparison.Ordinal);

            switch (element)
            {
                case Control control:
                    if (!preserveThemeForeground)
                        ReplaceBrush(control, Control.ForegroundProperty, foregroundMap);
                    ReplaceBrush(control, Control.BackgroundProperty, backgroundMap);
                    ReplaceBrush(control, Control.BorderBrushProperty, borderMap);
                    break;
                case Border border:
                    ReplaceBrush(border, Border.BackgroundProperty, backgroundMap);
                    ReplaceBrush(border, Border.BorderBrushProperty, borderMap);
                    break;
                case Panel panel:
                    ReplaceBrush(panel, Panel.BackgroundProperty, backgroundMap);
                    break;
                case TextBlock textBlock:
                    if (!preserveThemeForeground)
                        ReplaceBrush(textBlock, TextBlock.ForegroundProperty, foregroundMap);
                    ReplaceBrush(textBlock, TextBlock.BackgroundProperty, backgroundMap);
                    break;
                case TextElement textElement:
                    if (!preserveThemeForeground)
                        ReplaceBrush(textElement, TextElement.ForegroundProperty, foregroundMap);
                    ReplaceBrush(textElement, TextElement.BackgroundProperty, backgroundMap);
                    break;
                case Shape shape:
                    if (!preserveThemeForeground)
                    {
                        ReplaceBrush(shape, Shape.FillProperty, backgroundMap);
                        ReplaceBrush(shape, Shape.StrokeProperty, borderMap);
                    }
                    break;
            }

            if (element is Visual || element is Visual3D)
            {
                var visualChildren = VisualTreeHelper.GetChildrenCount(element);
                for (var index = 0; index < visualChildren; index++)
                    ApplyElement(
                        VisualTreeHelper.GetChild(element, index),
                        visited,
                        preserveThemeForeground);
            }

            foreach (var child in LogicalTreeHelper.GetChildren(element))
            {
                if (child is DependencyObject dependencyObject)
                    ApplyElement(dependencyObject, visited, preserveThemeForeground);
            }
        }

        private static void ReplaceBrush(
            DependencyObject element,
            DependencyProperty property,
            IReadOnlyDictionary<Color, Color> colorMap)
        {
            if (element.GetValue(property) is not SolidColorBrush brush ||
                !colorMap.TryGetValue(brush.Color, out var replacement))
            {
                return;
            }

            element.SetCurrentValue(property, new SolidColorBrush(replacement));
        }

        private static SolidColorBrush CreateBrush(string color) =>
            new((Color)ColorConverter.ConvertFromString(color));

        private static Dictionary<Color, Color> CreateColorMap(
            params (string source, string destination)[] values)
        {
            var result = new Dictionary<Color, Color>();
            foreach (var (source, destination) in values)
            {
                result[(Color)ColorConverter.ConvertFromString(source)] =
                    (Color)ColorConverter.ConvertFromString(destination);
            }

            return result;
        }

        private static Dictionary<Color, Color> Reverse(IReadOnlyDictionary<Color, Color> source)
        {
            var result = new Dictionary<Color, Color>();
            foreach (var entry in source)
                result[entry.Value] = entry.Key;
            return result;
        }
    }
}
