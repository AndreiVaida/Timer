using System;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media;
using System.Windows;
using System.Windows.Documents;
using System.Collections.Generic;
using System.Linq;
using Timer.Model;

namespace Timer.Utils {
    public static class UIUtils {
        private const double DefaultButtonOpacity = 1;
        private const double PressedButtonOpacity = 0.5;
        private static readonly Color _activityAnimatedBackgroundColor = Color.FromRgb(37, 150, 190);
        private static readonly Color _activityAnimatedBackgroundColor2 = Color.FromRgb(37, 190, 92);
        private static readonly Brush _activityForegroundBrush = new SolidColorBrush(Color.FromRgb(255, 255, 255));
        private static readonly Brush _activityBackgroundBrush = new SolidColorBrush(Color.FromRgb(37, 150, 190));

        public static bool IsPressed(this Button button) => button.Opacity != DefaultButtonOpacity;
        public static void Press(this Button button, double opacity = PressedButtonOpacity) => button.Opacity = opacity;
        public static void Unpress(this Button button) => button.Opacity = DefaultButtonOpacity;

        public static Button CreateActivityButton(string activityName, object content) => new() {
            Tag = activityName,
            Content = content,
            Background = _activityBackgroundBrush,
            Foreground = _activityForegroundBrush,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        public static Button CreateActivityButton(string activityName, RoutedEventHandler onSelectActivityClick, bool isPressed = false) {
            var button = CreateActivityButton(activityName, activityName);
            button.Click += onSelectActivityClick;

            if (isPressed) {
                AnimateButtonBackground(button);
                button.Press(0.75);
            }

            return button;
        }

        public static Button CreateActivityButtonWithTitle(string activityName, string title, RoutedEventHandler onSelectActivityClick) {
            var button = CreateActivityButton(activityName, onSelectActivityClick);
            button.Content = CreateCustomButtonContent(title, activityName);
            return button;
        }

        public static void AnimateButtonBackground(Button button) {
            var colorAnimation = new ColorAnimation {
                From = _activityAnimatedBackgroundColor2,
                To = _activityAnimatedBackgroundColor,
                Duration = TimeSpan.FromSeconds(1),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };
            button.Background = _activityBackgroundBrush.Clone();
            button.Background.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
        }

        public static void SetWorkingTime(this TextBlock label, DateOnly date, IList<Activity> activities) {
            var totalDuration = new TimeSpan(activities.Sum(activity => activity.Duration.Ticks));
            label.Text = $"{date.ToShortDateString()}\n{totalDuration.Hours}h {totalDuration.Minutes}m";
        }

        private static TextBlock CreateCustomButtonContent(string title, string subtitle) {
            var mainTextBlock = new TextBlock { TextAlignment = TextAlignment.Center };
            var innerText1 = new TextBlock {
                Text = subtitle,
                FontSize = 10,
                Margin = new Thickness(0, -3, 0, -10),
            };
            var lineBreak = new LineBreak();
            var innerText2 = new TextBlock {
                Text = title,
                Margin = new Thickness(0, -3, 0, 0),
            };
            mainTextBlock.Inlines.Add(innerText1);
            mainTextBlock.Inlines.Add(lineBreak);
            mainTextBlock.Inlines.Add(innerText2);

            return mainTextBlock;
        }
    }
}
