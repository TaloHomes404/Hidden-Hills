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
    public partial class DecryptViewModel : ObservableObject
    {
        // Właściwości związane z importem pliku (zaszyfrowanego)
        [ObservableProperty]
        private string selectedFilePath;

        // Tekst przycisku importu – wyświetla nazwę pliku, gdy zostanie zaimportowany
        public string ImportButtonText => string.IsNullOrEmpty(SelectedFilePath)
            ? "Import .enc File"
            : Path.GetFileName(SelectedFilePath);

        // Właściwości wyboru algorytmu dekripcji
        [ObservableProperty]
        private string selectedAlgorithm;
        public ObservableCollection<string> AvailableAlgorithms { get; } =
            new ObservableCollection<string> { "AES", "DES", "TripleDES" };

        // Właściwości związane z kluczem dekripcji – tylko jedna metoda ma być użyta:
        // Klucz z pliku
        [ObservableProperty]
        private string importedKeyFilePath;
        [ObservableProperty]
        private string importedKeyContent;

        // Klucz wpisywany ręcznie
        [ObservableProperty]
        private string decryptionTextKey;

        // Flaga sterująca widocznością pola tekstowego do wpisania klucza
        [ObservableProperty]
        private bool isKeyTextInputVisible;

        // Właściwość konwertująca flagę na Visibility – używana w XAML
        public Visibility KeyTextVisibility => IsKeyTextInputVisible ? Visibility.Visible : Visibility.Collapsed;
        partial void OnIsKeyTextInputVisibleChanged(bool oldValue, bool newValue)
        {
            OnPropertyChanged(nameof(KeyTextVisibility));
        }

        // Tekst przycisku importu klucza – pokazuje nazwę pliku, gdy zostanie zaimportowany
        public string ImportKeyButtonText => string.IsNullOrEmpty(ImportedKeyFilePath)
            ? "Import Key File"
            : Path.GetFileName(ImportedKeyFilePath);

        // Pasek postępu
        [ObservableProperty]
        private double progress;

        // Przechowywane odszyfrowane dane
        private byte[] _decryptedData;

        // Komendy
        public IRelayCommand ImportPackageCommand { get; }
        public IRelayCommand ImportKeyFileCommand { get; }
        public IRelayCommand EnterKeyTextCommand { get; }
        public IRelayCommand DecryptFilesCommand { get; }
        public IRelayCommand SaveFilesCommand { get; }

        public DecryptViewModel()
        {
            ImportPackageCommand = new RelayCommand(ImportPackage);
            ImportKeyFileCommand = new RelayCommand(ImportKeyFile);
            EnterKeyTextCommand = new RelayCommand(EnterKeyText);
            DecryptFilesCommand = new AsyncRelayCommand(DecryptFilesAsync, CanDecrypt);
            SaveFilesCommand = new RelayCommand(SaveFiles, CanSave);
        }

        // Efektywny klucz – używamy klucza z pliku, jeśli dostępny, w przeciwnym razie klucza wpisanego
        private string EffectiveKey => !string.IsNullOrWhiteSpace(ImportedKeyContent)
            ? ImportedKeyContent
            : DecryptionTextKey;

        // Aktualizacja komend i właściwości przy zmianach
        partial void OnSelectedFilePathChanged(string oldValue, string newValue)
        {
            OnPropertyChanged(nameof(ImportButtonText));
            DecryptFilesCommand.NotifyCanExecuteChanged();
        }
        partial void OnSelectedAlgorithmChanged(string oldValue, string newValue)
        {
            DecryptFilesCommand.NotifyCanExecuteChanged();
        }
        partial void OnDecryptionTextKeyChanged(string oldValue, string newValue)
        {
            // Gdy wpisany klucz ulegnie zmianie, upewniamy się, że import klucza jest czyszczony
            if (!string.IsNullOrWhiteSpace(newValue))
            {
                ImportedKeyFilePath = string.Empty;
                ImportedKeyContent = string.Empty;
            }
            DecryptFilesCommand.NotifyCanExecuteChanged();
        }
        partial void OnImportedKeyContentChanged(string oldValue, string newValue)
        {
            DecryptFilesCommand.NotifyCanExecuteChanged();
        }
        partial void OnImportedKeyFilePathChanged(string oldValue, string newValue)
        {
            OnPropertyChanged(nameof(ImportKeyButtonText));
        }

        // Metoda importująca zaszyfrowany plik (tylko .enc)
        private void ImportPackage()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Encrypted files (*.enc)|*.enc|All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                SelectedFilePath = openFileDialog.FileName;
                MessageBox.Show($"Imported file: {SelectedFilePath}");
                DecryptFilesCommand.NotifyCanExecuteChanged();
            }
        }

        // Metoda importująca plik klucza (TXT) – po imporcie czyścimy ewentualny wpisany klucz
        private void ImportKeyFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                ImportedKeyFilePath = openFileDialog.FileName;
                try
                {
                    ImportedKeyContent = File.ReadAllText(ImportedKeyFilePath);
                    MessageBox.Show($"Imported key file: {Path.GetFileName(ImportedKeyFilePath)}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error reading key file: " + ex.Message);
                }
                // Po imporcie klucza z pliku wyłączamy opcję wpisywania tekstowego
                IsKeyTextInputVisible = false;
                DecryptionTextKey = string.Empty;
                DecryptFilesCommand.NotifyCanExecuteChanged();
            }
        }

        // Metoda wyświetlająca pole tekstowe do wpisania klucza – czyszczenie importowanego klucza
        private void EnterKeyText()
        {
            IsKeyTextInputVisible = !IsKeyTextInputVisible;
            // Czyszczenie klucza z pliku, jeśli był wcześniej zaimportowany
            ImportedKeyFilePath = string.Empty;
            ImportedKeyContent = string.Empty;
            DecryptFilesCommand.NotifyCanExecuteChanged();
        }

        // Asynchroniczna metoda dekripcji
        private async Task DecryptFilesAsync()
        {
            MessageBox.Show("Decryption process started.");

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

            byte[] decryptedBytes = null;
            try
            {
                if (SelectedAlgorithm == "AES")
                {
                    using (Aes aes = Aes.Create())
                    {
                        aes.KeySize = 128;
                        aes.BlockSize = 128;
                        aes.Key = GetKeyBytes(EffectiveKey, 16);
                        aes.IV = new byte[16]; // dla uproszczenia – stałe IV (NIE stosować w produkcji!)
                        using (var decryptor = aes.CreateDecryptor())
                        {
                            decryptedBytes = decryptor.TransformFinalBlock(fileBytes, 0, fileBytes.Length);
                        }
                    }
                }
                else if (SelectedAlgorithm == "DES")
                {
                    using (DES des = DES.Create())
                    {
                        des.KeySize = 64;
                        des.BlockSize = 64;
                        des.Key = GetKeyBytes(EffectiveKey, 8);
                        des.IV = new byte[8];
                        using (var decryptor = des.CreateDecryptor())
                        {
                            decryptedBytes = decryptor.TransformFinalBlock(fileBytes, 0, fileBytes.Length);
                        }
                    }
                }
                else if (SelectedAlgorithm == "TripleDES")
                {
                    using (TripleDES tripleDes = TripleDES.Create())
                    {
                        tripleDes.KeySize = 192;
                        tripleDes.BlockSize = 64;
                        tripleDes.Key = GetKeyBytes(EffectiveKey, 24);
                        tripleDes.IV = new byte[8];
                        using (var decryptor = tripleDes.CreateDecryptor())
                        {
                            decryptedBytes = decryptor.TransformFinalBlock(fileBytes, 0, fileBytes.Length);
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
                MessageBox.Show("Error during decryption: " + ex.Message);
                return;
            }

            // Symulacja postępu dekripcji przez 10 sekund (100 kroków, 100 ms każdy)
            Progress = 0;
            const int totalSteps = 100;
            for (int i = 0; i <= totalSteps; i++)
            {
                Progress = i;
                await Task.Delay(100);
            }

            _decryptedData = decryptedBytes;
            MessageBox.Show("Decryption process completed.");
            SaveFilesCommand.NotifyCanExecuteChanged();
        }

        // Zapis odszyfrowanego pliku
        private void SaveFiles()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "PCAP files (*.pcap)|*.pcap|All files (*.*)|*.*"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    File.WriteAllBytes(saveFileDialog.FileName, _decryptedData);
                    MessageBox.Show($"Decrypted file saved: {saveFileDialog.FileName}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error saving file: " + ex.Message);
                }
            }
        }

        // Warunki umożliwiające wykonanie dekripcji – wymagane: plik, algorytm oraz niepusty efektywny klucz
        private bool CanDecrypt() =>
            !string.IsNullOrEmpty(SelectedFilePath) &&
            !string.IsNullOrEmpty(SelectedAlgorithm) &&
            !string.IsNullOrWhiteSpace(EffectiveKey);

        private bool CanSave() => Progress >= 100 && _decryptedData != null;

        /// <summary>
        /// Pomocnicza metoda, która przekształca podany klucz (string) na tablicę bajtów o wymaganej długości.
        /// Jeśli klucz jest krótszy – dopełnia zerami, jeśli dłuższy – przycina.
        /// </summary>
        private byte[] GetKeyBytes(string key, int requiredLength)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] paddedKey = new byte[requiredLength];
            Array.Copy(keyBytes, paddedKey, Math.Min(keyBytes.Length, requiredLength));
            return paddedKey;
        }
    }
}