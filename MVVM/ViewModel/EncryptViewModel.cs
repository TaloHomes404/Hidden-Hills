using System.Collections.ObjectModel;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace Hidden_Hills.MVVM.ViewModel
{
    public partial class EncryptViewModel : ObservableObject
    {
        // Automatycznie generowane właściwości (INotifyPropertyChanged)
        [ObservableProperty]
        private string selectedFilePath;

        [ObservableProperty]
        private string selectedAlgorithm;

        [ObservableProperty]
        private string encryptionKey;

        [ObservableProperty]
        private double progress;

        // Pole przechowujące zaszyfrowane dane (używane przy zapisie)
        private byte[] _encryptedData;

        // Lista dostępnych algorytmów
        public ObservableCollection<string> AvailableAlgorithms { get; } =
            new ObservableCollection<string> { "AES", "DES", "TripleDES" };

        // Komendy z CommunityToolkit
        public IRelayCommand ImportPackageCommand { get; }
        public IRelayCommand EncryptFilesCommand { get; }
        public IRelayCommand SaveFilesCommand { get; }

        public EncryptViewModel()
        {
            ImportPackageCommand = new RelayCommand(ImportPackage);
            EncryptFilesCommand = new AsyncRelayCommand(EncryptFilesAsync, CanEncrypt);
            SaveFilesCommand = new RelayCommand(SaveFiles, CanSave);
        }

        // Powiadomienia o zmianach właściwości, aby komenda mogła zaktualizować swoje CanExecute
        partial void OnSelectedFilePathChanged(string oldValue, string newValue)
        {
            EncryptFilesCommand.NotifyCanExecuteChanged();
        }
        partial void OnSelectedAlgorithmChanged(string oldValue, string newValue)
        {
            EncryptFilesCommand.NotifyCanExecuteChanged();
        }
        partial void OnEncryptionKeyChanged(string oldValue, string newValue)
        {
            EncryptFilesCommand.NotifyCanExecuteChanged();
        }


        //Pomocnicza zmienna która odpowiada za stan tekstu w przycisku
        //Ma być Import.pcap file a po zaimportowaniu zmienić napis na nazwe pliku
        public string ImportButtonText => string.IsNullOrEmpty(SelectedFilePath) ? "Import .PCAP File" : Path.GetFileName(SelectedFilePath);

        partial void OnSelectedFilePathChanging(string? oldValue, string newValue)
        {
            EncryptFilesCommand.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(ImportButtonText));
        }

        // Import pliku .pcap
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
                OnPropertyChanged(nameof(ImportButtonText));

            }
        }

        // Asynchroniczna metoda szyfrowania
        private async Task EncryptFilesAsync()
        {
            MessageBox.Show("Encryption process started.");

            // Odczyt zawartości pliku
            byte[] fileBytes;
            try
            {
                fileBytes = File.ReadAllBytes(SelectedFilePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error reading file: " + ex.Message);
                return;
            }

            // Wykonanie szyfrowania przy użyciu klucza podanego przez użytkownika
            byte[] encryptedBytes = null;
            try
            {
                if (SelectedAlgorithm == "AES")
                {
                    using (Aes aes = Aes.Create())
                    {
                        aes.KeySize = 128;   // 16 bajtów
                        aes.BlockSize = 128; // 16 bajtów
                        aes.Key = GetKeyBytes(EncryptionKey, 16); // Używamy klucza wpisanego przez użytkownika
                        aes.IV = new byte[16]; // Dla uproszczenia IV = 0 (NIE stosować w produkcji!)
                        using (ICryptoTransform encryptor = aes.CreateEncryptor())
                        {
                            encryptedBytes = encryptor.TransformFinalBlock(fileBytes, 0, fileBytes.Length);
                        }
                    }
                }
                else if (SelectedAlgorithm == "DES")
                {
                    using (DES des = DES.Create())
                    {
                        des.KeySize = 64;   // 8 bajtów
                        des.BlockSize = 64; // 8 bajtów
                        des.Key = GetKeyBytes(EncryptionKey, 8);
                        des.IV = new byte[8];
                        using (ICryptoTransform encryptor = des.CreateEncryptor())
                        {
                            encryptedBytes = encryptor.TransformFinalBlock(fileBytes, 0, fileBytes.Length);
                        }
                    }
                }
                else if (SelectedAlgorithm == "TripleDES")
                {
                    using (TripleDES tripleDes = TripleDES.Create())
                    {
                        tripleDes.KeySize = 192;   // 24 bajty
                        tripleDes.BlockSize = 64;  // 8 bajtów
                        tripleDes.Key = GetKeyBytes(EncryptionKey, 24);
                        tripleDes.IV = new byte[8];
                        using (ICryptoTransform encryptor = tripleDes.CreateEncryptor())
                        {
                            encryptedBytes = encryptor.TransformFinalBlock(fileBytes, 0, fileBytes.Length);
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Unsupported algorithm selected.");
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error during encryption: " + ex.Message);
                return;
            }

            // Symulacja postępu szyfrowania przez 10 sekund (100 kroków po 100 ms)
            Progress = 0;
            const int totalSteps = 100;
            for (int i = 0; i <= totalSteps; i++)
            {
                Progress = i;
                await Task.Delay(100);
            }

            // Zapis wyniku szyfrowania
            _encryptedData = encryptedBytes;

            MessageBox.Show("Encryption process completed.");
            SaveFilesCommand.NotifyCanExecuteChanged();
        }

        // Zapis zaszyfrowanego pliku
        private void SaveFiles()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Encrypted files (*.enc)|*.enc|All files (*.*)|*.*"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    File.WriteAllBytes(saveFileDialog.FileName, _encryptedData);
                    MessageBox.Show($"Encrypted file saved: {saveFileDialog.FileName}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error saving file: " + ex.Message);
                }
            }
        }

        // Warunki wykonania komend
        private bool CanEncrypt() =>
            !string.IsNullOrEmpty(SelectedFilePath) &&
            !string.IsNullOrEmpty(SelectedAlgorithm) &&
            !string.IsNullOrEmpty(EncryptionKey);

        private bool CanSave() => Progress >= 100 && _encryptedData != null;

        /// <summary>
        /// Pomocnicza metoda, która na podstawie wpisanego klucza tworzy tablicę bajtów o wymaganej długości.
        /// Jeśli długość wpisanego klucza jest mniejsza – dopełnia zerami, jeśli większa – przycina.
        /// </summary>
        /// <param name="key">Klucz wpisany przez użytkownika</param>
        /// <param name="requiredLength">Wymagana długość w bajtach</param>
        /// <returns>Tablica bajtów reprezentująca klucz</returns>
        private byte[] GetKeyBytes(string key, int requiredLength)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] paddedKey = new byte[requiredLength];
            Array.Copy(keyBytes, paddedKey, Math.Min(keyBytes.Length, requiredLength));
            return paddedKey;
        }
    }
}