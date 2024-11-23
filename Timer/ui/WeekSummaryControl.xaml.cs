using Ninject;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Timer.General;
using Timer.Model;
using Timer.Service;
using Timer.Utils;

namespace Timer.ui {
    /// <summary>
    /// Interaction logic for WeekSummaryControl.xaml
    /// </summary>
    public partial class WeekSummaryControl : UserControl {
        private readonly ActivityService _timeService = NinjectKernel.Kernel.Get<ActivityService>();
        private readonly Dictionary<DayOfWeek, Tuple<TextBlock, ListBox>> _weekDaysControls;
        private readonly MediaPlayer _copySoundPlayer = CreateMediaPlayer("Pop_sound.mp3");
        private DateOnly _dayOfWeekSummary = DateOnly.FromDateTime(DateTime.Today);
        public WeekSummaryControl() {
            InitializeComponent();
            _weekDaysControls = InitWeekDaysControls();
            LoadWeekSummary();
        }

        public void OnChangeWeekSummaryButtonClick(object sender, RoutedEventArgs e) {
            var button = sender as Button;
            var daysToAdd = int.Parse((string)button!.Tag) * 7;
            _dayOfWeekSummary = _dayOfWeekSummary.AddDays(daysToAdd);
            LoadWeekSummary();
        }

        private static void ChangeButtonContent(Button button, string newText, TimeSpan duration) {
            var originalContent = button.Content;
            if (originalContent.Equals(newText)) return;

            button.Content = newText;
            var timer = new DispatcherTimer { Interval = duration };
            timer.Tick += (s, args) => {
                button.Content = originalContent;
                timer.Stop();
            };
            timer.Start();
        }

        private Dictionary<DayOfWeek, Tuple<TextBlock, ListBox>> InitWeekDaysControls() => new() {
            {DayOfWeek.Monday, Tuple.Create(TextMondayWorkedTime, MondayActivitiesListBox)},
            {DayOfWeek.Tuesday, Tuple.Create(TextTuesdayWorkedTime, TuesdayActivitiesListBox)},
            {DayOfWeek.Wednesday, Tuple.Create(TextWednesdayWorkedTime, WednesdayActivitiesListBox)},
            {DayOfWeek.Thursday, Tuple.Create(TextThursdayWorkedTime, ThursdayActivitiesListBox)},
            {DayOfWeek.Friday, Tuple.Create(TextFridayWorkedTime, FridayActivitiesListBox)}
        };

        private void LoadWeekSummary() {
            CleanWeekSummary();
            var activitiesOnEachDay = _timeService.GetWeekSummary(_dayOfWeekSummary);

            foreach (var (date, activities) in activitiesOnEachDay) {
                var (label, listBox) = _weekDaysControls[date.DayOfWeek];
                label.SetWorkingTime(date, activities);
                SetActivitySummary(listBox, activities);
            }
        }

        private void CleanWeekSummary() {
            foreach (var (label, listbox) in _weekDaysControls.Values) {
                label.Text = "00:00:00";
                listbox.Items.Clear();
            }
        }

        private void SetActivitySummary(ListBox listBox, List<Activity> activities) {
            activities.ForEach(activity => {
                var duration = $"{activity.Duration.Hours}h {activity.Duration.Minutes}m";
                var content = new TextBlock { Text = $"{activity.Name}\n{duration}", TextAlignment = TextAlignment.Center };
                var button = CreateActivitySummaryButton(activity.Name, content);
                listBox.Items.Add(button);
            });
        }

        private static MediaPlayer CreateMediaPlayer(string audioFileName) {
            var player = new MediaPlayer();
            player.Open(new Uri($"Resources/{audioFileName}", UriKind.Relative));
            return player;
        }

        private Button CreateActivitySummaryButton(string activityName, TextBlock content) {
            var button = UIUtils.CreateActivityButton(activityName, content);
            button.Height = 35;
            button.Click += (_, _) => {
                Clipboard.SetText(activityName);
                PlaySound();
                ChangeButtonContent(button, $"{activityName} copiat!", TimeSpan.FromSeconds(2));
            };
            return button;
        }

        private void PlaySound() {
            _copySoundPlayer.Position = TimeSpan.Zero;
            _copySoundPlayer.Play();
        }
    }
}
