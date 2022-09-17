using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Timer.model;
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

        public MainWindow() {
            InitializeComponent();
            InitializeButtonList();
            _timeService = new();
            _uiScheduler = new SynchronizationContextScheduler(SynchronizationContext.Current!);
            SubscribeToTimeEvents();
        }

        private bool IsActivityNameValid() => InputActivityName.Text.Trim().Length > 0;

        private void OnCreateActivityClick(object sender, RoutedEventArgs e) {
            if (!IsActivityNameValid()) return;
            _timeService.CreateActivity(InputActivityName.Text);
        }

        private void OnStepButtonClick(object sender, RoutedEventArgs e) {
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

        private void OnStepButtonClick(Button button, Step step) {
            _timeService.StartStep(step);
            MakeSingleButtonPressed(button);
        }

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
            _timeService.TimeUpdates
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
    }
}
