using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Hidden_Hills.Core;
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

        public event PropertyChangedEventHandler PropertyChanged;

        public PackageViewModel()
        {
            _capturedPackets = new ObservableCollection<RawCapture>();
            AvailableDurations = new ObservableCollection<int> { 5, 10, 15, 30, 60 }; // Sekundy
            AvailablePorts = new ObservableCollection<int> { 80, 443, 21, 22, 25, 53, 8080 }; // Popularne porty

            _captureCommand = new RelayCommand(StartCapture, CanStartCapture);
            _saveCommand = new RelayCommand(SavePcap, CanSavePcap);
            _cancelCommand = new RelayCommand(CancelCapture, CanCancelCapture);

            CaptureDuration = AvailableDurations.First(); // Domyślnie pierwsza wartość
            SelectedPort = AvailablePorts.First(); // Domyślnie pierwszy port
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

        private async void StartCapture(object parameter)
        {
            IsCapturing = true;
            IsCaptureCompleted = false;
            Progress = 0;
            _capturedPackets.Clear();

            var devices = CaptureDeviceList.Instance;
            if (devices.Count < 1)
            {
                MessageBox.Show("Brak dostępnych interfejsów sieciowych.");
                IsCapturing = false;
                return;
            }

            var device = devices.FirstOrDefault();
            if (device == null)
            {
                MessageBox.Show("Nie znaleziono urządzenia do nasłuchiwania.");
                IsCapturing = false;
                return;
            }

            device.Open(DeviceModes.Promiscuous);
            device.OnPacketArrival += (sender, e) =>
            {
                var rawPacket = e.GetPacket();
                _capturedPackets.Add(rawPacket);
            };

            device.StartCapture();

            for (int i = 0; i < CaptureDuration; i++)
            {
                Progress = (i + 1) * 100 / CaptureDuration;
                await Task.Delay(1000);
            }

            device.StopCapture();
            device.Close();

            IsCapturing = false;
            IsCaptureCompleted = true;
            Progress = 100;
        }

        private bool CanStartCapture(object parameter)
        {
            return !IsCapturing;
        }

        private void SavePcap(object parameter)
        {
            if (_capturedPackets.Count == 0) return;

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "PCAP Files (*.pcap)|*.pcap",
                Title = "Zapisz przechwycone pakiety"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                using (var writer = new BinaryWriter(File.Create(saveFileDialog.FileName)))
                {
                    foreach (var packet in _capturedPackets)
                    {
                        writer.Write(packet.Data);
                    }
                }
                MessageBox.Show("Zapisano plik PCAP.");
            }
        }

        private bool CanSavePcap(object parameter)
        {
            return IsCaptureCompleted;
        }

        private void CancelCapture(object parameter)
        {
            _capturedPackets.Clear();
            Progress = 0;
            IsCapturing = false;
            IsCaptureCompleted = false;
        }

        private bool CanCancelCapture(object parameter)
        {
            return IsCapturing || IsCaptureCompleted;
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
