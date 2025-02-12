using System.ComponentModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace Hidden_Hills.MVVM.ViewModel
{
    public class DecryptViewModel
    {
        private string _decryptKey;
        public string DecryptKey
        {
            get => _decryptKey;
            set
            {
                _decryptKey = value;
                OnPropertyChanged(nameof(DecryptKey));
            }
        }

        private double _progress;
        public double Progress
        {
            get => _progress;
            set
            {
                _progress = value;
                OnPropertyChanged(nameof(Progress));
            }
        }

        private bool _isDecryptionCompleted;
        public bool IsDecryptionCompleted
        {
            get => _isDecryptionCompleted;
            set
            {
                _isDecryptionCompleted = value;
                OnPropertyChanged(nameof(IsDecryptionCompleted));
            }
        }

        public ICommand DecryptCommand => new RelayCommand(DecryptFiles);
        public ICommand ImportKeyCommand => new RelayCommand(ImportKeyFile);
        public ICommand SaveCommand => new RelayCommand(SaveDecryptedFiles, CanSaveDecryptedFiles);

        private void DecryptFiles()
        {
            // Logika szyfrowania
        }

        private void ImportKeyFile()
        {
            // Logika importowania klucza
        }

        private void SaveDecryptedFiles()
        {
            // Logika zapisywania
        }

        private bool CanSaveDecryptedFiles()
        {
            return IsDecryptionCompleted;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}