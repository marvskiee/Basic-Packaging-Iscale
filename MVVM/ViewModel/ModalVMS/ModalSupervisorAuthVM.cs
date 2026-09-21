using ISCALE.MVVM.Model;
using ISCALE.Utilities.Services;
using Microsoft.Data.Sqlite;
using MVVMCORE;
using System.Collections.ObjectModel;
using System.Windows;

namespace ISCALE.MVVM.ViewModel.ModalVMS
{
    public class ModalSupervisorAuthVM : ViewModelBase
    {
        private readonly string _defaultMessage = "Please call your supervisor to continue this action.";
        private AuthServices _userServices;

        private User CurrentUser;

        private Style _messageStyle;

        public Style MessageStyle
        {
            get { return _messageStyle; }
            set { _messageStyle = value; OnPropertyChanged(); }
        }

        private string _message;
        public string Message
        {
            get { return _message; }
            set { _message = value; OnPropertyChanged(); }
        }

        private string _accessCode;
        public string AccessCode
        {
            get { return _accessCode; }
            set { _accessCode = value; OnPropertyChanged(); }
        }

        public Action<bool?> CloseAction { get; set; }

        private string _note;

        public string Note
        {
            get { return _note; }
            set { _note = value; OnPropertyChanged(); }
        }

        public ObservableCollection<TransactionItem> DeletedItem { get; set; }

        public ModalSupervisorAuthVM(User user, List<TransactionItem> deletedItem, SqliteConnection connection)
        {
            _userServices = new AuthServices(connection);
            CurrentUser = user;
            Message = _defaultMessage;
            DeletedItem = new ObservableCollection<TransactionItem>(deletedItem);
            MessageStyle = (Style)Application.Current.FindResource("NormalAlert_patched");
        }

        #region Relay
        public RelayCommand CancelCommand => new RelayCommand(Cancel);
        public RelayCommand SubmitCommand => new RelayCommand(Submit);

        #endregion

        #region Command
        private void Cancel(object parameter)
        {
            CloseAction?.Invoke(false);
        }
        private async void Submit(object parameter)
        {
            Message = _defaultMessage;
            MessageStyle = (Style)Application.Current.FindResource("NormalAlert_patched");

            ServiceResult result = await _userServices.Login(CurrentUser.Username, AccessCode);
            if (result.Success)
            {
                CloseAction?.Invoke(true);
            }
            else
            {
                Message = "Wrong Access Code!";
                MessageStyle = (Style)Application.Current.FindResource("ErrorAlert_patched");
            }

        }

        #endregion
    }
}
