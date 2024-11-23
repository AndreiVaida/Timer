using Ninject;
using System;
using System.Windows;
using System.Windows.Controls;
using Timer.General;
using Timer.Service;
using Timer.Utils;

namespace Timer.ui {
    /// <summary>
    /// Interaction logic for ActivitySelectorControl.xaml
    /// </summary>
    public partial class ActivitySelectorControl : UserControl {
        private readonly ActivityService _timeService = NinjectKernel.Kernel.Get<ActivityService>();
        private string? _activeActivityName;

        public ActivitySelectorControl() {
            InitializeComponent();
            LoadLatestActivities();
            LoadPinnedActivities();
            StartLatestActivity();
        }

        public void OnCreateActivityClick(object sender, RoutedEventArgs e) {
            StartActivity(InputActivityName.Text);
            LoadLatestActivities();
        }

        public void OnSelectActivityClick(object sender, RoutedEventArgs e) {
            var button = (Button)sender;
            StartActivity(button.Tag.ToString()!);
        }

        private void LoadLatestActivities() {
            ActivitiesListBox.Items.Clear();

            var activities = _timeService.GetLatestActivities(20);
            activities.ForEach(activityName => {
                var activityButton = UIUtils.CreateActivityButton(activityName, OnSelectActivityClick, activityName.Equals(_activeActivityName));
                ActivitiesListBox.Items.Add(activityButton);
            });
        }

        private void StartLatestActivity() {
            if (!ActivitiesListBox.Items.IsEmpty) {
                var activity = (Button) ActivitiesListBox.Items[0];
                StartActivity((string)activity.Tag);
            }
        }

        private void LoadPinnedActivities() {
            PinnedActivitiesListBox.Items.Add(UIUtils.CreateActivityButtonWithTitle("TINT-185", "Meetings", OnSelectActivityClick));
            PinnedActivitiesListBox.Items.Add(UIUtils.CreateActivityButtonWithTitle("TINT-186", "Education", OnSelectActivityClick));
        }

        private void StartActivity(string activityName) {
            if (!IsActivityNameValid(activityName)) return;

            InputActivityName.Text = activityName;
            _activeActivityName = activityName;
            _timeService.CreateActivity(activityName);
            LoadLatestActivities();
        }

        private static bool IsActivityNameValid(string? name) => !(name?.Trim()).IsNullOrEmpty();
    }
}
