using ISCALE.MVVM.Model;
using ISCALE.MVVM.View;
using ISCALE.MVVM.View.ModalViews;
using ISCALE.MVVM.ViewModel.AdminVMS;
using ISCALE.Utilities;
using Microsoft.Data.Sqlite;
using MVVMCORE;
using System.Windows;

namespace ISCALE.MVVM.ViewModel
{
    public class AdminVM : ViewModelBase
    {
        private readonly SqliteConnection _connection;
        #region init
        private Visibility _settingVisibility = Visibility.Collapsed;
        public Visibility SettingVisibility
        {
            get { return _settingVisibility; }
            set { _settingVisibility = value; OnPropertyChanged(); }
        }

        private User _currentUser;
        public User CurrentUser
        {
            get { return _currentUser; }
            set { _currentUser = value; OnPropertyChanged(); }
        }

        private string _avatar;
        public string Avatar
        {
            get { return _avatar; }
            set { _avatar = value; OnPropertyChanged(); }
        }

        private object _currentView;
        public object CurrentViewModel
        {
            get => _currentView;
            set
            {
                _currentView = value;
                OnPropertyChanged(nameof(CurrentViewModel));
            }
        }
        #endregion
        public AdminVM(User user, SqliteConnection connection)
        {
            _connection = connection;
            CurrentViewModel = new AdminUserVM(_connection);
            CurrentUser = user;
            _avatar = Constants.ADMIN_AVATAR;
        }

        #region Relay
        public RelayCommand SettingCommand => new RelayCommand(null);
        public RelayCommand ChangeViewCommand => new RelayCommand(ChangeView);
        public RelayCommand LogoutCommand => new RelayCommand(Logout);

        #endregion

        #region Command
        private void ChangeView(object parameter)
        {
            switch (parameter)
            {
                case "AdminUserView":
                    CurrentViewModel = new AdminUserVM(_connection);
                    break;
                case "AdminProductView":
                    CurrentViewModel = new AdminProductVM(_connection);
                    break;
                case "AdminTransactionView":
                    CurrentViewModel = new AdminTransactionVM(_connection);
                    break;
                case "AdminShiftScheduleView":
                    CurrentViewModel = new AdminShiftScheduleVM(_connection);
                    break;
                case "AdminChangePasswordView":
                    CurrentViewModel = new AdminChangePasswordVM(_connection, CurrentUser);
                    break;
                default:
                    break;
            }
        }
        private async void Logout(object parameter)
        {
            FileHelper.DisableWindow();

            ModalResultView resultModalView = new ModalResultView(Application.Current.MainWindow, "loading", "Logging out...");
            resultModalView.Show();

            await Task.Delay(100);

            var loginView = new LoginView();
            loginView.Show();

            Application.Current.MainWindow?.Close();
            Application.Current.MainWindow = loginView;
        }
        
        #endregion
    }
}
