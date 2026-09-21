using MVVMCORE;
using ISCALE.MVVM.Model;
using ISCALE.MVVM.View;
using ISCALE.MVVM.ViewModel.OperatorVMS.ComponentVM;
using System.Configuration;
using System.Windows;
using ISCALE.Utilities;
using ISCALE.MVVM.ViewModel.ModalVMS;
using ISCALE.MVVM.View.ModalViews;

namespace ISCALE.Core.Abstract
{
    public abstract class AOperatorVM : ViewModelBase
    {
        #region General Bind
        private User _currentUser;
        public User CurrentUser
        {
            get { return _currentUser; }
            set { _currentUser = value; OnPropertyChanged(); }
        }
        #endregion

        #region Form Bind
        private bool _isFormEnabled;
        public bool IsFormEnabled
        {
            get { return _isFormEnabled; }
            set { _isFormEnabled = value; OnPropertyChanged(); }
        }
        #endregion

        public AOperatorVM(User currentUser)
        {
            CurrentUser = currentUser;
            Application.Current.Exit += OnApplicationExit;
        }

        private LoginView loginView = new LoginView();
        protected async void Logout(object parameter)
        {
            try
            {
                FileHelper.DisableWindow();
                ModalConfirmationVM dialogVM = new ModalConfirmationVM("Logout", "Are you sure you want to logout?");
                ModalConfirmationView dialog = new ModalConfirmationView
                {
                    DataContext = dialogVM,
                    Owner = Application.Current.MainWindow
                };
                dialogVM.CloseAction = (result) => dialog.DialogResult = result;

                bool? result = dialog.ShowDialog();

                if (result == true)
                {

                    ModalResultView resultModalView = new ModalResultView(Application.Current.MainWindow, "loading", "Logging out...");
                    resultModalView.Show();

                    await Task.Delay(500);

                    loginView.Show();

                    Application.Current.MainWindow?.Close();
                    resultModalView.Close();
                    Application.Current.MainWindow = loginView;
                }
                else
                    FileHelper.EnableWindow();

            }
            catch (Exception ex)
            {
                FileHelper.CatchSystemError(ex);
            }
        }
        protected abstract void OnFormClose();
        private void OnApplicationExit(object sender, ExitEventArgs e)
        {
            OnFormClose();
        }
    }
}
