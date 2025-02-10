using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using SharpPcap;

namespace Hidden_Hills.MVVM.ViewModel
{
    public class PackageViewModel : INotifyPropertyChanged
    {
        private int _progress;
        private bool _isCapturing;
        private bool _isCaptureCompleted;
        private ObservableCollection<RawCapture> _capturedPackets;
        private int _selectedPort;
        private int _captureDuration;
        private ICommand _captureCommand;
        private ICommand _saveCommand;
        private ICommand _cancelCommand;
        private ICaptureDevice _currentDevice;

        private CancellationTokenSource _cancellationTokenSource;

        public event PropertyChangedEventHandler PropertyChanged;

        public PackageViewModel()
        {
            _capturedPackets = new ObservableCollection<RawCapture>();
            AvailableDurations = new ObservableCollection<int> { 5, 10, 15, 30, 60 };
            AvailablePorts = new ObservableCollection<int> { 80, 443, 21, 22, 25, 53, 8080 };

            _captureCommand = new RelayCommand(StartCapture, CanStartCapture);
            _saveCommand = new RelayCommand(SavePcap, CanSavePcap);
            _cancelCommand = new RelayCommand(CancelCapture, CanCancelCapture);

            CaptureDuration = AvailableDurations.First();
            SelectedPort = AvailablePorts.First();
        }

        public ObservableCollection<int> AvailableDurations { get; }
        public ObservableCollection<int> AvailablePorts { get; }

        public int Progress
        {
            get => _progress;
            set
            {
                _progress = value;
                OnPropertyChanged(nameof(Progress));
            }
        }

        public bool IsCapturing
        {
            get => _isCapturing;
            set
            {
                _isCapturing = value;
                OnPropertyChanged(nameof(IsCapturing));
            }
        }

        public bool IsCaptureCompleted
        {
            get => _isCaptureCompleted;
            set
            {
                _isCaptureCompleted = value;
                OnPropertyChanged(nameof(IsCaptureCompleted));
            }
        }

        public int SelectedPort
        {
            get => _selectedPort;
            set
            {
                _selectedPort = value;
                OnPropertyChanged(nameof(SelectedPort));
            }
        }

        public int CaptureDuration
        {
            get => _captureDuration;
            set
            {
                _captureDuration = value;
                OnPropertyChanged(nameof(CaptureDuration));
            }
        }

        public ICommand CaptureCommand => _captureCommand;
        public ICommand SaveCommand => _saveCommand;
        public ICommand CancelCommand => _cancelCommand;

        private async void StartCapture()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;


            MessageBox.Show("Rozpoczynam przechwytywanie pakietów...", "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);

            try
            {
                Debug.WriteLine("StartCapture() rozpoczęło działanie");

                var devices = CaptureDeviceList.Instance;
                if (devices.Count < 1)
                {
                    Debug.WriteLine("Brak dostępnych urządzeń");
                    MessageBox.Show("Brak dostępnych interfejsów sieciowych.");
                    IsCapturing = false;
                    return;
                }

                _currentDevice = devices.FirstOrDefault();
                if (_currentDevice == null)
                {
                    Debug.WriteLine("Nie znaleziono urządzenia");
                    MessageBox.Show("Nie znaleziono urządzenia do nasłuchiwania.");
                    IsCapturing = false;
                    return;
                }

                Debug.WriteLine($"Znaleziono urządzenie: {_currentDevice.Description}");

                _currentDevice.Open(DeviceModes.Promiscuous);
                _capturedPackets.Clear();
                Progress = 0;
                IsCapturing = true;
                IsCaptureCompleted = false;

                _currentDevice.OnPacketArrival += (sender, e) =>
                {
                    var packet = e.GetPacket(); // Pobranie pakietu

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _capturedPackets.Add(new RawCapture(packet.LinkLayerType, packet.Timeval, packet.Data.ToArray())); // Kopia danych
                        Progress = (_capturedPackets.Count * 100) / (CaptureDuration * 10);
                        Debug.WriteLine($"Przechwycono pakiet: {packet}");
                    });
                };

                _currentDevice.StartCapture();

                // Czekaj określoną ilość sekund, następnie zatrzymaj przechwytywanie
                await Task.Delay(CaptureDuration * 1000, token);
                if (!token.IsCancellationRequested)
                {
                    StopCapture();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Błąd w StartCapture: {ex.Message}");
                MessageBox.Show($"Błąd: {ex.Message}");
                IsCapturing = false;
            }
        }

        private void StopCapture()
        {
            if (_currentDevice != null && _currentDevice.Started)
            {
                _currentDevice.StopCapture();
                _currentDevice.Close();
                _currentDevice = null;
            }

            IsCapturing = false;
            IsCaptureCompleted = _capturedPackets.Count > 0;
            MessageBox.Show("Przechwytywanie zakończone.", "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private bool CanStartCapture()
        {
            return !IsCapturing;
        }

        private void SavePcap()
        {
            if (_capturedPackets.Count == 0)
            {
                MessageBox.Show("Brak pakietów do zapisania.", "Informacja", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SaveFileDialog saveFileDialog = new()
            {
                Filter = "PCAP Files (*.pcap)|*.pcap",
                Title = "Zapisz przechwycone pakiety"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    using var writer = new BinaryWriter(File.Create(saveFileDialog.FileName));
                    foreach (var packet in _capturedPackets)
                    {
                        writer.Write(packet.Data);
                    }
                    MessageBox.Show("Zapisano plik PCAP.", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Błąd podczas zapisu: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private bool CanSavePcap()
        {
            return IsCaptureCompleted && _capturedPackets.Count > 0;
        }

        private void CancelCapture()
        {
            _cancellationTokenSource?.Cancel(); // Anulowanie `Task.Delay()`
            StopCapture();
            _capturedPackets.Clear();
            Progress = 0;
            IsCaptureCompleted = false;
        }

        private bool CanCancelCapture()
        {
            return IsCapturing || IsCaptureCompleted;
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
