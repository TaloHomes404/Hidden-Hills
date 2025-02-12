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
        // Właściwości związane z importem pliku PCAP
        [ObservableProperty]
        private string selectedFilePath;

        // Właściwość używana do zmiany tekstu przycisku Import Package
        public string ImportButtonText => string.IsNullOrEmpty(SelectedFilePath)
            ? "Import .enc File"
            : Path.GetFileName(SelectedFilePath);

        // Właściwości wyboru algorytmu dekripcji
        [ObservableProperty]
        private string selectedAlgorithm;

        public ObservableCollection<string> AvailableAlgorithms { get; } =
            new ObservableCollection<string> { "AES", "DES", "TripleDES" };

        // Właściwości związane z kluczem dekripcji – tylko jeden sposób można wybrać:
        // Klucz z pliku
        [ObservableProperty]
        private string importedKeyFilePath;
        [ObservableProperty]
        private string importedKeyContent;

        // Klucz wpisywany ręcznie
        [ObservableProperty]
        private string decryptionTextKey;

        // Czy pokazać pole tekstowe dla wpisania klucza
        [ObservableProperty]
        private bool isKeyTextInputVisible;

        // Tekst przycisku do importu klucza – zmienia się na nazwę pliku, gdy plik zostanie wybrany
        public string ImportKeyButtonText => string.IsNullOrEmpty(ImportedKeyFilePath)
            ? "Import Key File"
            : Path.GetFileName(ImportedKeyFilePath);

        //Pomocnicza zmienna do zmiany widoczności textboxa (trzeba text = visible)
        public Visibility KeyTextVisibility => IsKeyTextInputVisible ? Visibility.Visible : Visibility.Collapsed;

        //Pomocniczna metoda do zmiany widoku (aktualizacji UI w oparciu o nową wartość (zmiana widzoczności)
        partial void OnIsKeyTextInputVisibleChanged(bool oldValue, bool newValue)
        {
            OnPropertyChanged(nameof(KeyTextVisibility));
        }
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

        // Wartość efektywnego klucza – używamy klucza z pliku, jeśli jest dostępny, w przeciwnym razie klucza wpisanego
        private string EffectiveKey => !string.IsNullOrEmpty(ImportedKeyContent)
            ? ImportedKeyContent
            : DecryptionTextKey;

        // Aktualizacja komend przy zmianie właściwości
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

        // Metoda importująca plik PCAP
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
                DecryptFilesCommand.NotifyCanExecuteChanged();
            }
        }

        // Metoda importująca plik klucza (TXT)
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
                DecryptFilesCommand.NotifyCanExecuteChanged();
            }
        }

        // Metoda wyświetlająca pole tekstowe do wpisania klucza
        private void EnterKeyText()
        {
            IsKeyTextInputVisible = true;
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
                        aes.IV = new byte[16]; // Dla uproszczenia IV = 0
                        using (ICryptoTransform decryptor = aes.CreateDecryptor())
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
                        using (ICryptoTransform decryptor = des.CreateDecryptor())
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
                        using (ICryptoTransform decryptor = tripleDes.CreateDecryptor())
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

            // Symulacja postępu dekripcji przez 10 sekund (100 kroków po 100 ms)
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

        // Warunki umożliwiające wykonanie komendy dekripcji
        private bool CanDecrypt() =>
            !string.IsNullOrEmpty(SelectedFilePath) &&
            !string.IsNullOrEmpty(SelectedAlgorithm) &&
            !string.IsNullOrEmpty(EffectiveKey);

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