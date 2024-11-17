using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Timer.model;
using Timer.Model;
using Timer.Repository;
using Timer.service;
using Timer.Service;
using Timer.Utils;

namespace Timer {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private readonly TimeService _timeService;
        private readonly IScheduler _uiScheduler;
        private readonly TimeUtils _timeUtils = new TimeUtilsImpl();
        private IList<Button> _singularButtons;
        private IList<Button> _parallelButtons;
        private const double PressedButtonOpacity = 0.5;
        private const double DefaultButtonOpacity = 1;
        private readonly Color _activityAnimatedBackgroundColor = Color.FromRgb(37, 150, 190);
        private readonly Color _activityAnimatedBackgroundColor2 = Color.FromRgb(37, 190, 92);
        private readonly Brush _activityForegroundBrush = new SolidColorBrush(Color.FromRgb(255, 255, 255));
        private readonly Brush _activityBackgroundBrush = new SolidColorBrush(Color.FromRgb(37, 150, 190));
        private string? _activeActivityName;
        private readonly Dictionary<DayOfWeek, Tuple<TextBlock, ListBox>> _weekDaysControls;
        private readonly MediaPlayer _copySoundPlayer = CreateMediaPlayer("Pop_sound.mp3");
        private DateOnly _dayOfWeekSummary = DateOnly.FromDateTime(DateTime.Today);

        public MainWindow() {
            InitializeComponent();
            InitializeButtonLists();
            _timeService = new TimeServiceImpl(new TimeRepositoryImpl(_timeUtils), _timeUtils);
            _uiScheduler = new SynchronizationContextScheduler(SynchronizationContext.Current!);
            SubscribeToTimeEvents();
            LoadLatestActivity();
            LoadLatestActivities();
            _weekDaysControls = InitWeekDaysControls();
            LoadWeekSummary();
        }

        private static bool IsActivityNameValid(string? name) => !(name?.Trim()).IsNullOrEmpty();

        public void OnCreateActivityClick(object sender, RoutedEventArgs e)
        {
            StartActivity(InputActivityName.Text);
        }

        public void OnStepButtonClick(object sender, RoutedEventArgs e) {
            if (!IsActivityNameValid(_activeActivityName)) return;
            var button = (Button)sender;

            if (button == ButtonMeeting)
                OnStepButtonClick(button, Step.MEETING);

            if (button == ButtonOther)
                OnStepButtonClick(button, Step.OTHER);

            if (button == ButtonInvestigate)
                OnStepButtonClick(button, Step.INVESTIGATE);

            if (button == ButtonImplement)
                OnStepButtonClick(button, Step.IMPLEMENT);

            if (button == ButtonWaitForReview) {
                var step = IsPressed(button) ? Step.WAIT_FOR_REVIEW__END : Step.WAIT_FOR_REVIEW__START;
                OnStepButtonClick(button, step);
            }

            if (button == ButtonResolveComments) {
                if (IsPressed(ButtonWaitForReview))
                    OnStepButtonClick(ButtonWaitForReview, Step.WAIT_FOR_REVIEW__END);
                OnStepButtonClick(button, Step.RESOLVE_COMMENTS);
            }

            if (button == ButtonDoReview)
                OnStepButtonClick(button, Step.DO_REVIEW);

            if (button == ButtonLoading) {
                var step = IsPressed(button) ? Step.LOADING__END : Step.LOADING__START;
                OnStepButtonClick(button, step);
            }

            if (button == ButtonPause)
                OnStepButtonClick(button, Step.PAUSE);
        }

        public void OnSelectActivityClick(object sender, RoutedEventArgs e) {
            var button = sender as Button;
            StartActivity(button!.Tag.ToString()!);
        }

        public void OnSelectPinnedActivityClick(object sender, RoutedEventArgs e) {
            OnSelectActivityClick(sender, e);
            OnStepButtonClick(ButtonImplement, Step.IMPLEMENT);
            LoadLatestActivities();
        }

        public void OnChangeWeekSummaryButtonClick(object sender, RoutedEventArgs e) {
            var button = sender as Button;
            var daysToAdd = int.Parse((string)button!.Tag) * 7;
            _dayOfWeekSummary = _dayOfWeekSummary.AddDays(daysToAdd);
            LoadWeekSummary();
        }

        private void StartActivity(string activityName)
        {
            if (!IsActivityNameValid(activityName)) return;
            _activeActivityName = activityName;
            InputActivityName.Text = activityName;
            var timeLog = _timeService.CreateActivity(activityName);

            HandleButtonsPressState(null);

            if (timeLog?.Step == Step.PAUSE)
                UpdateWindow(timeLog);

            LoadLatestActivities();
        }

        private void OnStepButtonClick(Button button, Step step) {
            _timeService.StartStep(step);
            HandleButtonsPressState(button);
        }

        private void InitializeButtonLists() {
            _singularButtons = new List<Button> {
                ButtonMeeting,
                ButtonOther,
                ButtonInvestigate,
                ButtonImplement,
                ButtonResolveComments,
                ButtonDoReview,
                ButtonPause
            };
            _parallelButtons = new List<Button> {
                ButtonWaitForReview,
                ButtonLoading
            };

            HandleButtonsPressState(null);
        }

        private void HandleButtonsPressState(Button? pressedButton) {
            if (pressedButton == null) {
                Unpress(_singularButtons);
                Unpress(_parallelButtons);
                return;
            }

            if (pressedButton == ButtonPause) {
                Press(pressedButton);
                Unpress(_singularButtons.Except(pressedButton));
                Unpress(_parallelButtons);
                return;
            }

            if (IsParallelTask(pressedButton)) {
                if (IsPressed(pressedButton))
                    Unpress(pressedButton);
                else {
                    Press(pressedButton);
                    Unpress(_singularButtons);
                }
            }
            else {
                Press(pressedButton);
                Unpress(_singularButtons.Except(pressedButton));
            }
        }

        private bool IsParallelTask(Button button) => _parallelButtons.Contains(button);

        private static bool IsPressed(Button button) => button.Opacity == PressedButtonOpacity;

        private static void Press(Button button, double opacity = PressedButtonOpacity) => button.Opacity = opacity;

        private static void Unpress(Button button) => button.Opacity = DefaultButtonOpacity;

        private static void Unpress(IList<Button> buttonList) {
            foreach (var button in buttonList)
                Unpress(button);
        }

        private void SubscribeToTimeEvents() {
            _timeService.TimeUpdates
                .ObserveOn(_uiScheduler)
                .Subscribe(timeEvent => {
                    var (label, button) = timeEvent.Step switch {
                        Step.MEETING => (LabelMeetingTime, ButtonMeeting),
                        Step.OTHER => (LabelOtherTime, ButtonOther),
                        Step.INVESTIGATE => (LabelInvestigateTime, ButtonInvestigate),
                        Step.IMPLEMENT => (LabelImplementTime, ButtonImplement),
                        Step.WAIT_FOR_REVIEW__START or Step.WAIT_FOR_REVIEW__END => (LabelWaitForReviewTime, ButtonWaitForReview),
                        Step.RESOLVE_COMMENTS => (LabelResolveCommentsTime, ButtonResolveComments),
                        Step.DO_REVIEW => (LabelDoReviewTime, ButtonDoReview),
                        Step.LOADING__START or Step.LOADING__END => (LabelLoadingTime, ButtonLoading),
                        Step.TOTAL => (LabelTotalTime, null),
                        _ => throw new InvalidEnumArgumentException($"Unhandled Step of TimeEvent: {timeEvent}")
                    };

                    var duration = timeEvent.Duration.ToString();
                    label.Content = timeEvent.Step.IsParallelStart() ? $"🔄 {duration}" : duration;
                    if (button != null && timeEvent.IsActive)
                        Press(button);
                });
        }

        private void LoadLatestActivities()
        {
            ActivitiesListBox.Items.Clear();

            var activities = _timeService.GetLatestActivities(20);
            activities.ForEach(activityName => {
                var activityButton = CreateClickableActivityButton(activityName);
                ActivitiesListBox.Items.Add(activityButton);
            });
        }

        private Button CreateClickableActivityButton(string activityName)
        {
            var button = CreateActivityButton(activityName, activityName);
            button.Click += OnSelectActivityClick;

            if (activityName == _activeActivityName) {
                AnimateButtonBackground(button);
                Press(button, 0.75);
            }

            return button;
        }

        private Button CreateActivitySummaryButton(string activityName, TextBlock content) {
            var button = CreateActivityButton(activityName, content);
            button.Height = 35;
            button.Click += (_, _) => Clipboard.SetText(activityName);
            button.Click += (_, _) => {
               _copySoundPlayer.Position = TimeSpan.Zero;
               _copySoundPlayer.Play();
            };
            button.Click += (_, _) => ChangeButtonContent(button, $"{activityName} copiat!", TimeSpan.FromSeconds(2));
            return button;
        }

        private Button CreateActivityButton(string activityName, object content) => new()
        {
            Tag = activityName,
            Content = content,
            Background = _activityBackgroundBrush,
            Foreground = _activityForegroundBrush,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        private void AnimateButtonBackground(Button button)
        {
            var colorAnimation = new ColorAnimation
            {
                From = _activityAnimatedBackgroundColor2,
                To = _activityAnimatedBackgroundColor,
                Duration = TimeSpan.FromSeconds(1),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };
            button.Background = _activityBackgroundBrush.Clone();
            button.Background.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
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

        private void LoadLatestActivity() {
            var (activityName, timeLog) = _timeService.LoadLatestActivity();
            if (activityName == null) return;

            _activeActivityName = activityName;
            InputActivityName.Text = activityName;

            if (timeLog?.Step == Step.PAUSE)
                Press(ButtonPause);
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

            foreach (var (date, activities) in activitiesOnEachDay)
            {
                var (label, listBox) = _weekDaysControls[date.DayOfWeek];
                SetWorkingTime(label, date, activities);
                SetActivitySummary(listBox, activities);
            }
        }

        private void CleanWeekSummary() {
            foreach (var (label, listbox) in _weekDaysControls.Values) {
                label.Text = "00:00:00";
                listbox.Items.Clear();
            }
        }

        private void SetWorkingTime(TextBlock label, DateOnly date, IList<Activity> activities) {
            var totalDuration = new TimeSpan(activities.Sum(activity => activity.Duration.Ticks));
            label.Text = $"{date.ToShortDateString()}\n{totalDuration.Hours}h {totalDuration.Minutes}m";
        }

        private void SetActivitySummary(ListBox listBox, List<Activity> activities) {
            activities.ForEach(activity =>
            {
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

        private Button? GetButtonForStep(Step? step) =>
            step switch {
                Step.MEETING => ButtonMeeting,
                Step.OTHER => ButtonOther,
                Step.INVESTIGATE => ButtonInvestigate,
                Step.IMPLEMENT => ButtonImplement,
                Step.WAIT_FOR_REVIEW__START or Step.WAIT_FOR_REVIEW__END => ButtonWaitForReview,
                Step.RESOLVE_COMMENTS => ButtonResolveComments,
                Step.DO_REVIEW => ButtonDoReview,
                Step.LOADING__START or Step.LOADING__END => ButtonLoading,
                Step.PAUSE => ButtonPause,
                _ => null
        };

        private void UpdateWindow(TimeLog? timeLog) {
            var buttonToPress = GetButtonForStep(timeLog?.Step);
            HandleButtonsPressState(buttonToPress);
        }
    }
}
