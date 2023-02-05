using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
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

        public MainWindow() {
            InitializeComponent();
            InitializeButtonLists();
            _timeService = new TimeServiceImpl(new TimeRepositoryImpl(_timeUtils), _timeUtils);
            _uiScheduler = new SynchronizationContextScheduler(SynchronizationContext.Current!);
            SubscribeToTimeEvents();
            LoadLatestActivity();
        }

        private bool IsActivityNameValid() => InputActivityName.Text.Trim().Length > 0;

        private void OnCreateActivityClick(object sender, RoutedEventArgs e) {
            if (!IsActivityNameValid()) return;
            var timeLog = _timeService.CreateActivity(InputActivityName.Text);

            HandleButtonsPressState(null);

            if (timeLog?.Step == Step.PAUSE)
                UpdateWindow(timeLog);
        }

        private void OnStepButtonClick(object sender, RoutedEventArgs e) {
            if (!IsActivityNameValid()) return;
            var button = (Button)sender;

            if (button == ButtonMeeting)
                OnStepButtonClick(button, Step.MEETING);

            if (button == ButtonInvestigate)
                OnStepButtonClick(button, Step.INVESTIGATE);

            if (button == ButtonImplement)
                OnStepButtonClick(button, Step.IMPLEMENT);

            if (button == ButtonWaitForReview) {
                var step = IsPressed(button) ? Step.WAIT_FOR_REVIEW__END : Step.WAIT_FOR_REVIEW__START;
                OnStepButtonClick(button, step);
            }

            if (button == ButtonResolveComments)
                OnStepButtonClick(button, Step.RESOLVE_COMMENTS);

            if (button == ButtonDoReview)
                OnStepButtonClick(button, Step.DO_REVIEW);

            if (button == ButtonLoading) {
                var step = IsPressed(button) ? Step.LOADING__END : Step.LOADING__START;
                OnStepButtonClick(button, step);
            }

            if (button == ButtonPause)
                OnStepButtonClick(button, Step.PAUSE);
        }

        private void OnStepButtonClick(Button button, Step step) {
            _timeService.StartStep(step);
            HandleButtonsPressState(button);
        }

        private void InitializeButtonLists() {
            _singularButtons = new List<Button> {
                ButtonMeeting,
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

        private static void Press(Button button) => button.Opacity = PressedButtonOpacity;

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

        private void LoadLatestActivity() {
            var (activityName, timeLog) = _timeService.LoadLatestActivity();
            if (activityName == null) return;

            InputActivityName.Text = activityName;
            if (timeLog?.Step == Step.PAUSE)
                Press(ButtonPause);
        }

        private Button? GetButtonForStep(Step? step) =>
            step switch {
                Step.MEETING => ButtonMeeting,
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
            UpdateStartActivityTime(timeLog?.DateTime);
        }

        private void UpdateStartActivityTime(DateTime? dateTime) {
            LabelStartActivityTime.Content = dateTime == null ? string.Empty : _timeUtils.FormatDateTime((DateTime)dateTime);
        }
    }
}
