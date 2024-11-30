using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Timer.model;
using Timer.Model;
using Timer.service;
using Timer.Utils;

namespace Timer {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private readonly ActivityService _activityService;
        private readonly IScheduler _uiScheduler;
        private IList<Button> _buttonList;
        public ObservableCollection<string> RecentActivities { get; set; } = new();

        public MainWindow() {
            InitializeComponent();
            DataContext = this;
            InitializeButtonList();
            _activityService = new();
            _uiScheduler = new SynchronizationContextScheduler(SynchronizationContext.Current!);
            SubscribeToTimeEvents();
            LoadLatestActivities();
            LoadLatestActivity();
        }

        private bool IsActivityNameValid() => InputActivityNameCombobox.Text.Trim().Length > 0;

        public void OnCreateActivityClick(object sender, RoutedEventArgs e) {
            if (!IsActivityNameValid()) return;
            var timeLog = _activityService.CreateActivity(InputActivityNameCombobox.Text);

            UpdateWindow(timeLog);
            LoadLatestActivities();
        }

        public void OnStepButtonClick(object sender, RoutedEventArgs e) {
            if (!IsActivityNameValid()) return;
            var button = (Button)sender;

            if (button == ButtonDownload)
                OnStepButtonClick(button, Step.DOWNLOAD);

            if (button == ButtonLoad)
                OnStepButtonClick(button, Step.LOAD);

            if (button == ButtonEdit)
                OnStepButtonClick(button, Step.EDIT);

            if (button == ButtonFreezeReload)
                OnStepButtonClick(button, Step.FREEZE_RELOAD);

            if (button == ButtonPause)
                OnStepButtonClick(button, Step.PAUSE);

            if (button == ButtonExport)
                OnStepButtonClick(button, Step.EXPORT);
        }

        public void OnStepButtonClick(Button button, Step step) {
            _activityService.StartStep(step);
            MakeSingleButtonPressed(button);
        }

        public void OnOpenFile(object sender, RoutedEventArgs e) => _activityService.OpenActivityFile(InputActivityNameCombobox.Text);
        public void OnOpenFileLocation(object sender, RoutedEventArgs e) => _activityService.OpenActivityFile();

        private void InitializeButtonList() {
            _buttonList = new List<Button> {
                ButtonDownload,
                ButtonLoad,
                ButtonEdit,
                ButtonFreezeReload,
                ButtonPause,
                ButtonExport
            };

            MakeSingleButtonPressed(null);
        }

        private void MakeSingleButtonPressed(Button? pressedButton) {
            foreach (var button in _buttonList) {
                button.Opacity = button == pressedButton ? 0.5 : 1;
            }
        }

        private void SubscribeToTimeEvents() {
            _activityService.TimeUpdates
                .ObserveOn(_uiScheduler)
                .Subscribe(timeEvent => {
                    switch(timeEvent.Step) {
                        case Step.DOWNLOAD: LabelDownloadTime.Content = timeEvent.Duration.ToString(); break;
                        case Step.LOAD: LabelLoadingTime.Content = timeEvent.Duration.ToString(); break;
                        case Step.EDIT: LabelEditTime.Content = timeEvent.Duration.ToString(); break;
                        case Step.FREEZE_RELOAD: LabelFreezeReloadTime.Content = timeEvent.Duration.ToString(); break;
                        case Step.EXPORT: LabelExportTime.Content = timeEvent.Duration.ToString(); break;
                        case Step.TOTAL: LabelTotalTime.Content = timeEvent.Duration.ToString(); break;
                    }
                });
        }

        public void OnActivitySelected(object sender, SelectionChangedEventArgs e) {
            if (!InputActivityNameCombobox.IsDropDownOpen || InputActivityNameCombobox.SelectedItem == null) return;
            OnActivitySelected();
        }

        private void OnActivitySelected(object sender, KeyEventArgs e) {
            if (e.Key != Key.Up && e.Key != Key.Down || InputActivityNameCombobox.SelectedItem == null) return;
            OnActivitySelected();
        }

        private void OnActivitySelected() {
            var selectedText = InputActivityNameCombobox.SelectedItem.ToString()!;
            var timeLog = _activityService.CreateActivity(selectedText);
            UpdateWindow(timeLog);
        }

        private void LoadLatestActivity() {
            var (activityName, timeLog) = _activityService.LoadLatestActivity();
            if (activityName == null) return;

            InputActivityNameCombobox.Text = activityName;
            UpdateWindow(timeLog);
        }

        private void LoadLatestActivities() {
            var activities = _activityService.GetLatestActivities(20);
            RecentActivities.Clear();
            foreach (var activity in activities) {
                RecentActivities.Add(activity);
            }
        }

        private Button? GetButtonForStep(Step? step) =>
            step switch {
                Step.DOWNLOAD => ButtonDownload,
                Step.LOAD => ButtonLoad,
                Step.EDIT => ButtonEdit,
                Step.FREEZE_RELOAD => ButtonFreezeReload,
                Step.EXPORT => ButtonExport,
                Step.PAUSE => ButtonPause,
                _ => null
        };

        private void UpdateWindow(TimeLog? timeLog) {
            var buttonToPress = GetButtonForStep(timeLog?.Step);
            MakeSingleButtonPressed(buttonToPress);
            UpdateStartActivityTime(timeLog?.DateTime);
        }

        private void UpdateStartActivityTime(DateTime? dateTime) {
            LabelStartActivityTime.Content = dateTime == null ? string.Empty : TimeUtils.FormatDateTime((DateTime)dateTime);
        }

        private void InputActivityNameCombobox_DropDownOpened(object sender, EventArgs e) => InputActivityNameCombobox.IsEditable = false;
        private void InputActivityNameCombobox_DropDownClosed(object sender, EventArgs e) => InputActivityNameCombobox.IsEditable = true;
    }
}
