﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using Timer.service;

namespace Timer {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private readonly TimeService _timeService;
        //public string DownloadTotalTime { get; set; }

        public MainWindow() {
            InitializeComponent();
            _timeService = new();
        }

        private bool IsActivityNameValid() => InputActivityName.Text.Trim().Length > 0;

        private void OnCreateActivityClick(object sender, RoutedEventArgs e) {
            if (!IsActivityNameValid()) return;
            _timeService.CreateActivity(InputActivityName.Text);
        }

        private void OnDownloadClick(object sender, RoutedEventArgs e) {
            if (!IsActivityNameValid()) return;
            _timeService.Download();
        }

        private void OnLoadingClick(object sender, RoutedEventArgs e) {
            if (!IsActivityNameValid()) return;
            _timeService.Loading();
        }

        private void OnEditingClick(object sender, RoutedEventArgs e) {
            if (!IsActivityNameValid()) return;
            _timeService.Editing();
        }

        private void OnFreezeReloadClick(object sender, RoutedEventArgs e) {
            if (!IsActivityNameValid()) return;
            _timeService.FreezeReload();
        }

        private void OnPauseClick(object sender, RoutedEventArgs e) {
            if (!IsActivityNameValid()) return;
            _timeService.Pause();
        }

        private void OnExportClick(object sender, RoutedEventArgs e) {
            if (!IsActivityNameValid()) return;
            _timeService.Export();
        }
    }
}
