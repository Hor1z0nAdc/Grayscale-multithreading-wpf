using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Threshold
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private Algorithms algorithms = new Algorithms();
        private BitmapImage originalImage;

        private int numOfThreads = 2;
        public int NumOfThreads
        {
            get { return numOfThreads; }
            set
            {
                numOfThreads = value;
                OnPropertyChanged();
            }
        }

        private string selectedAlgorithm = "Average gray scale";
        public  string SelectedAlgorithm
        {
            get { return selectedAlgorithm; }
            set
            {
                selectedAlgorithm = value.Replace("System.Windows.Controls.ComboBoxItem: ", "");     
                OnPropertyChanged();
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            this.Height = (System.Windows.SystemParameters.PrimaryScreenHeight * 0.85);
            this.Width = (System.Windows.SystemParameters.PrimaryScreenWidth * 0.85);
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
            DataContext = this;
        }

        private void btnOpenImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Title = "Select an image";
            fileDialog.Filter = "All supported graphics|*.jpg;*.jpeg;*.png|" +
              "JPEG (*.jpg;*.jpeg)|*.jpg;*.jpeg|" +
              "Portable Network Graphic (*.png)|*.png";

            if (fileDialog.ShowDialog() == true)
            {
                originalImage = new BitmapImage(new Uri(fileDialog.FileName));
                originalImageContainer.Source = originalImage;
                processedImageContainer.Source = null;
                algorithms.prepareProcess(originalImage);
            }
        }

        private void processSequential_Click(object sender, RoutedEventArgs e)
        {
            processedImageContainer.Source = null;

            if (originalImage != null)
            {
                Stopwatch timer = new Stopwatch();
                timer.Start();
                BitmapImage processedBitmap = algorithms.startProcessingImage(selectedAlgorithm);
                timer.Stop();
                timeTakenBlock.Text = "Time taken: " + timer.ElapsedMilliseconds + " ms";
                processedImageContainer.Source = processedBitmap;

                MessageBox.Show("The sequential process has finished", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void processParallel_Click(object sender, RoutedEventArgs e)
        {
            processedImageContainer.Source = null;

            if (originalImage != null)
            {
                Stopwatch timer = new Stopwatch();
                timer.Start();
                BitmapImage processedBitmap = algorithms.startProcessingImage(SelectedAlgorithm, NumOfThreads);
                timer.Stop();
                timeTakenBlock.Text = "Time taken: " + timer.ElapsedMilliseconds + " ms";
                processedImageContainer.Source = processedBitmap;

                MessageBox.Show("The parallel process has finished", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
