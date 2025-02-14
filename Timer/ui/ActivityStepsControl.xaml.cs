using Ninject;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Timer.General;
using Timer.model;
using Timer.Service;
using Timer.Utils;

namespace Timer.ui;

/// <summary>
/// Interaction logic for ActivityStepsControl.xaml
/// </summary>
public partial class ActivityStepsControl {
    private readonly ActivityService _timeService = NinjectKernel.Kernel.Get<ActivityService>();
    private readonly IScheduler _uiScheduler;
    private IList<Button> _singularButtons;
    private IList<Button> _parallelButtons;

    public ActivityStepsControl() {
        InitializeComponent();
        InitializeButtonLists();
        _uiScheduler = new SynchronizationContextScheduler(SynchronizationContext.Current!);
        SubscribeToTimeEvents();
    }

    public void OnStepButtonClick(object sender, RoutedEventArgs e) {
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
            var step = button.IsPressed() ? Step.WAIT_FOR_REVIEW__END : Step.WAIT_FOR_REVIEW__START;
            OnStepButtonClick(button, step);
        }

        if (button == ButtonResolveComments) {
            if (ButtonWaitForReview.IsPressed())
                OnStepButtonClick(ButtonWaitForReview, Step.WAIT_FOR_REVIEW__END);
            OnStepButtonClick(button, Step.RESOLVE_COMMENTS);
        }

        if (button == ButtonDoReview)
            OnStepButtonClick(button, Step.DO_REVIEW);

        if (button == ButtonLoading) {
            var step = button.IsPressed() ? Step.LOADING__END : Step.LOADING__START;
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
        _singularButtons = [
            ButtonMeeting,
            ButtonOther,
            ButtonInvestigate,
            ButtonImplement,
            ButtonResolveComments,
            ButtonDoReview,
            ButtonPause
        ];
        _parallelButtons = [
            ButtonWaitForReview,
            ButtonLoading
        ];

        HandleButtonsPressState(null);
    }

    private void HandleButtonsPressState(Button? pressedButton) {
        if (pressedButton == null) {
            Unpress(_singularButtons);
            Unpress(_parallelButtons);
            return;
        }

        if (pressedButton == ButtonPause) {
            pressedButton.Press();
            Unpress(_singularButtons.Except(pressedButton));
            Unpress(_parallelButtons);
            return;
        }

        if (IsParallelTask(pressedButton)) {
            if (pressedButton.IsPressed())
                pressedButton.Unpress();
            else {
                pressedButton.Press();
                Unpress(_singularButtons);
            }
        }
        else {
            pressedButton.Press();
            Unpress(_singularButtons.Except(pressedButton));
        }
    }

    private bool IsParallelTask(Button button) => _parallelButtons.Contains(button);

    private static void Unpress(IList<Button> buttonList) {
        foreach (var button in buttonList)
            button.Unpress();
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
                    Step.TOTAL => (LabelTotalTime, ButtonPause),
                    _ => throw new InvalidEnumArgumentException($"Unhandled Step of TimeEvent: {timeEvent}")
                };

                var duration = timeEvent.Duration.ToString();
                label.Content = timeEvent.Step.IsParallelStart() ? $"🔄 {duration}" : duration;

                var isActive = button != ButtonPause ? timeEvent.IsActive : !timeEvent.IsActive;
                if (isActive) button.Press();
                else button.Unpress();

                LabelActivityName.Content = timeEvent.ActivityName;
            });
    }
}