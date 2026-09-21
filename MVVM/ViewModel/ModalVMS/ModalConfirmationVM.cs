using MVVMCORE;

namespace ISCALE.MVVM.ViewModel.ModalVMS
{
    public class ModalConfirmationVM : ViewModelBase
    {
        private string _title;
        public string Title
        {
            get { return _title; }
            set { _title = value; OnPropertyChanged(); }
        }

        private string _body;

        public string Body
        {
            get { return _body; }
            set { _body = value; OnPropertyChanged(); }
        }
        public Action<bool?> CloseAction { get; set; }

        public ModalConfirmationVM(string title, string body)
        {
            Title = title;
            Body = body;
        }

        #region Relay
        public RelayCommand CancelCommand => new RelayCommand(Cancel);
        public RelayCommand ConfirmCommand => new RelayCommand(Confirm);

        #endregion

        private void Cancel(object parameter)
        {
            CloseAction?.Invoke(false);
        }
        private void Confirm(object parameter)
        {
            CloseAction?.Invoke(true);
        }
    }
}