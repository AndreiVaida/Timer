using System;
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

        private void OnDownloadClick(object sender, RoutedEventArgs e) {
            _timeService.Download(InputActivityName.Text);
        }

        private void OnLoadingClick(object sender, RoutedEventArgs e) {

        }

        private void OnEditingClick(object sender, RoutedEventArgs e) {

        }

        private void OnBlockedClick(object sender, RoutedEventArgs e) {

        }

        private void OnPauseClick(object sender, RoutedEventArgs e) {

        }

        private void OnExportClick(object sender, RoutedEventArgs e) {

        }
    }
}
