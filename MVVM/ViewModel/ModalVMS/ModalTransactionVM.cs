using ISCALE.MVVM.Model;
using MVVMCORE;
using System.Windows;

namespace ISCALE.MVVM.ViewModel.ModalVMS
{
    public class ModalTransactionVM : ViewModelBase
    {

		private Transaction _currentTransaction;
		public Transaction CurrentTransaction
        {
			get { return _currentTransaction; }
			set { _currentTransaction = value; OnPropertyChanged(); }
		}

        public Action<bool?> CloseAction { get; set; }

        public ModalTransactionVM(Transaction currentTransaction)
		{
			CurrentTransaction = currentTransaction;
		}

		#region Relay
		public RelayCommand CloseCommand => new RelayCommand(Close);
        #endregion

        #region Command
		private void Close(object parameter)
		{
			CloseAction?.Invoke(true);
		}
        #endregion


    }
}
