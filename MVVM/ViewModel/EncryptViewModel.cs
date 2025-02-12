using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace Hidden_Hills.MVVM.ViewModel
{
    public partial class EncryptViewModel : ObservableObject
    {
        [ObservableProperty]
        private string selectedFilePath;

        [ObservableProperty]
        private string selectedAlgorithm;

        [ObservableProperty]
        private string encryptionKey;

        [ObservableProperty]
        private double progress;

        public ObservableCollection<string> AvailableAlgorithms { get; } =
            new ObservableCollection<string> { "AES", "DES", "TripleDES" };

        // Komendy
        public IRelayCommand ImportPackageCommand { get; }
        public IRelayCommand EncryptFilesCommand { get; }
        public IRelayCommand SaveFilesCommand { get; }

        public EncryptViewModel()
        {
            ImportPackageCommand = new RelayCommand(ImportPackage);
            EncryptFilesCommand = new AsyncRelayCommand(EncryptFilesAsync, CanEncrypt);
            SaveFilesCommand = new RelayCommand(SaveFiles, CanSave);
        }

        private void ImportPackage()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "PCAP files (*.pcap)|*.pcap|All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                SelectedFilePath = openFileDialog.FileName;
                MessageBox.Show($"Imported file: {SelectedFilePath}");
                EncryptFilesCommand.NotifyCanExecuteChanged();
            }
        }

        private async Task EncryptFilesAsync()
        {
            MessageBox.Show("Encryption process started.");
            Progress = 0;

            const int totalSteps = 100;
            for (int i = 0; i <= totalSteps; i++)
            {
                Progress = i;
                await Task.Delay(100);
            }

            MessageBox.Show("Encryption process completed.");
            SaveFilesCommand.NotifyCanExecuteChanged();
        }

        private void SaveFiles()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Encrypted files (*.enc)|*.enc|All files (*.*)|*.*"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                MessageBox.Show($"Encrypted file saved: {saveFileDialog.FileName}");
            }
        }

        private bool CanEncrypt() =>
            !string.IsNullOrEmpty(SelectedFilePath) &&
            !string.IsNullOrEmpty(SelectedAlgorithm) &&
            !string.IsNullOrEmpty(EncryptionKey);

        private bool CanSave() => Progress >= 100;
    }
}