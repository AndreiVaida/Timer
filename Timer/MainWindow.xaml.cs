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

        public MainWindow() {
            InitializeComponent();
            _timeService = new();
            _uiScheduler = new SynchronizationContextScheduler(SynchronizationContext.Current!);
            SubscribeToTimeEvents();
        }

        private bool IsActivityNameValid() => InputActivityName.Text.Trim().Length > 0;

        private void OnCreateActivityClick(object sender, RoutedEventArgs e) {
            if (!IsActivityNameValid()) return;
            _timeService.CreateActivity(InputActivityName.Text);
        }

        private void OnDownloadClick(object sender, RoutedEventArgs e) {
            if (!IsActivityNameValid()) return;
            _timeService.StartStep(Step.DOWNLOAD);
        }

        private void OnLoadingClick(object sender, RoutedEventArgs e) {
            if (!IsActivityNameValid()) return;
            _timeService.StartStep(Step.LOAD);
        }

        private void OnEditingClick(object sender, RoutedEventArgs e) {
            if (!IsActivityNameValid()) return;
            _timeService.StartStep(Step.EDIT);
        }

        private void OnFreezeReloadClick(object sender, RoutedEventArgs e) {
            if (!IsActivityNameValid()) return;
            _timeService.StartStep(Step.FREEZE_RELOAD);
        }

        private void OnPauseClick(object sender, RoutedEventArgs e) {
            if (!IsActivityNameValid()) return;
            _timeService.StartStep(Step.PAUSE);
        }

        private void OnExportClick(object sender, RoutedEventArgs e) {
            if (!IsActivityNameValid()) return;
            _timeService.StartStep(Step.EXPORT);
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
