using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Timer.model;
using Timer.Model;
using Timer.service;
using Timer.Utils;

namespace Timer {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private readonly TimeService _timeService;
        private readonly IScheduler _uiScheduler;
        private IList<Button> _buttonList;
        private const double PressedButtonOpacity = 0.5;
        private const double DefaultButtonOpacity = 1;

        public MainWindow() {
            InitializeComponent();
            InitializeButtonList();
            _timeService = new();
            _uiScheduler = new SynchronizationContextScheduler(SynchronizationContext.Current!);
            SubscribeToTimeEvents();
            LoadLatestActivity();
        }

        private bool IsActivityNameValid() => InputActivityName.Text.Trim().Length > 0;

        private void OnCreateActivityClick(object sender, RoutedEventArgs e) {
            if (!IsActivityNameValid()) return;
            var timeLog = _timeService.CreateActivity(InputActivityName.Text);

            UpdateWindow(timeLog);
        }

        private void OnStepButtonClick(object sender, RoutedEventArgs e) {
            if (!IsActivityNameValid()) return;
            var button = (Button)sender;

            if (button == ButtonDiscuss)
                OnStepButtonClick(button, Step.DISCUSS);

            if (button == ButtonImplement)
                OnStepButtonClick(button, Step.IMPLEMENT);

            if (button == ButtonWaitForReview)
                OnStepButtonClick(button, Step.WAIT_FOR_REVIEW);

            if (button == ButtonResolveComments)
                OnStepButtonClick(button, Step.RESOLVE_COMMENTS);

            if (button == ButtonDoReview)
                OnStepButtonClick(button, Step.DO_REVIEW);

            if (button == ButtonPause)
                OnStepButtonClick(button, Step.PAUSE);
        }

        private void OnStepButtonClick(Button button, Step step) {
            _timeService.StartStep(step);
            HandleButtonsPressState(button);
        }

        private void InitializeButtonList() {
            _buttonList = new List<Button> {
                ButtonDiscuss,
                ButtonImplement,
                ButtonWaitForReview,
                ButtonResolveComments,
                ButtonDoReview,
                ButtonPause
            };

            HandleButtonsPressState(null);
        }

        private void HandleButtonsPressState(Button? pressedButton) {
            if (pressedButton == null)
            {
                UnpressButtons(_buttonList);
                return;
            }

            if (pressedButton == ButtonPause)
            {
                PressButton(pressedButton);
                UnpressButtons(_buttonList.Except(new List<Button> { pressedButton }).ToList());
                return;
            }

            if (IsParalelTask(pressedButton))
            {
                if (IsPressed(pressedButton))
                    UnpressButton(pressedButton);
                else
                {
                    PressButton(pressedButton);
                    UnpressButtons(_buttonList.Except(new List<Button> { pressedButton }).ToList());
                }
            }
            else
            {
                PressButton(pressedButton);
                var buttonsToUnpress = _buttonList.Where(btn => !IsParalelTask(btn) && btn != pressedButton).ToList();
                UnpressButtons(buttonsToUnpress);
            }
        }

        private bool IsParalelTask(Button? button) => button == ButtonWaitForReview;

        private static bool IsPressed(Button button) => button.Opacity == PressedButtonOpacity;

        private static void PressButton(Button button) => button.Opacity = PressedButtonOpacity;

        private static void UnpressButton(Button button) => button.Opacity = DefaultButtonOpacity;

        private static void UnpressButtons(IList<Button> buttonList)
        {
            foreach (var button in buttonList)
                UnpressButton(button);
        }

        private void SubscribeToTimeEvents() {
            _timeService.TimeUpdates
                .ObserveOn(_uiScheduler)
                .Subscribe(timeEvent => {
                    switch(timeEvent.Step) {
                        case Step.DISCUSS: LabelDiscussTime.Content = timeEvent.Duration.ToString(); break;
                        case Step.IMPLEMENT: LabelImplementTime.Content = timeEvent.Duration.ToString(); break;
                        case Step.WAIT_FOR_REVIEW: LabelWaitForReviewTime.Content = timeEvent.Duration.ToString(); break;
                        case Step.RESOLVE_COMMENTS: LabelResolveCommentsTime.Content = timeEvent.Duration.ToString(); break;
                        case Step.DO_REVIEW: LabelDoReviewTime.Content = timeEvent.Duration.ToString(); break;
                        case Step.TOTAL: LabelTotalTime.Content = timeEvent.Duration.ToString(); break;
                    }
                });
        }

        private void LoadLatestActivity() {
            var (activityName, timeLog) = _timeService.LoadLatestActivity();
            if (activityName == null) return;

            InputActivityName.Text = activityName;
            UpdateWindow(timeLog);
        }

        private Button? GetButtonForStep(Step? step) =>
            step switch {
                Step.DISCUSS => ButtonDiscuss,
                Step.IMPLEMENT => ButtonImplement,
                Step.WAIT_FOR_REVIEW => ButtonWaitForReview,
                Step.RESOLVE_COMMENTS => ButtonResolveComments,
                Step.DO_REVIEW => ButtonDoReview,
                Step.PAUSE => ButtonPause,
                _ => null
        };

        private void UpdateWindow(TimeLog? timeLog) {
            var buttonToPress = GetButtonForStep(timeLog?.Step);
            HandleButtonsPressState(buttonToPress);
            UpdateStartActivityTime(timeLog?.DateTime);
        }

        private void UpdateStartActivityTime(DateTime? dateTime) {
            LabelStartActivityTime.Content = dateTime == null ? string.Empty : TimeUtils.FormatDateTime((DateTime)dateTime);
        }
    }
}
