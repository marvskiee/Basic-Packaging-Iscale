using Formatter.Helper;
using ISCALE.MVVM.Model;
using ISCALE.MVVM.View.ModalViews;
using ISCALE.Utilities;
using ISCALE.Utilities.Services;
using Microsoft.Data.Sqlite;
using MVVMCORE;
using System.Windows;

namespace ISCALE.MVVM.ViewModel.AdminVMS
{
    public class AdminChangePasswordVM : ViewModelBase
    {
        private readonly UserServices _userService;
        private readonly User _currentUser;

        #region Bind Password
        private string _oldPassword;
        public string OldPassword
        {
            get { return _oldPassword; }
            set { _oldPassword = value; OnPropertyChanged(); }
        }
        private string _newPassword;
        public string NewPassword
        {
            get { return _newPassword; }
            set { _newPassword = value; OnPropertyChanged(); }
        }
        private string _confirmPassword;
        public string ConfirmPassword
        {
            get { return _confirmPassword; }
            set { _confirmPassword = value; OnPropertyChanged(); }
        }
        #endregion

        public AdminChangePasswordVM(SqliteConnection connection, User currentUser)
        {
            _userService = new UserServices(connection);
            _currentUser = currentUser;

        }

        #region Relay
        public RelayCommand SubmitCommand => new(Submit, CanSubmit);
        public RelayCommand ClearCommand => new(Clear, CanClear);
        #endregion

        #region Command
        private async void Submit(object p)
        {
            FileHelper.DisableWindow();
            ModalResultView loadingView = new(Application.Current.MainWindow, "loading");
            try
            {
                loadingView.Show();
                await Task.Delay(500);
                User newUser = new User
                {
                    Id = _currentUser.Id,
                    Password = CryptographyHelper.Encrypt(NewPassword, Constants.PASSWORD_HASH)
                };
                ServiceResult passwordResult = await _userService.UpdatePassword(newUser, OldPassword);
                if (!passwordResult.Success)
                    throw new CustomException(passwordResult.ErrorMessage, passwordResult.ErrorType);
                loadingView.Close();
                ModalResultView successView = new(Application.Current.MainWindow, "success", "Password Changed.");
                successView.ShowDialog();
                Clear(null);
            }
            catch (CustomException ex)
            {
                FileHelper.CatchError(ex);
            }
            catch (Exception ex)
            {
                FileHelper.CatchSystemError(ex);
            }
            loadingView.Close();
            FileHelper.EnableWindow();
        }
        private void Clear(object p)
        {
            OldPassword = string.Empty;
            NewPassword = string.Empty;
            ConfirmPassword = string.Empty;
        }
        #endregion

        #region CanCommand
        private bool CanClear(object p)
        {
            return !string.IsNullOrWhiteSpace(OldPassword) || !string.IsNullOrWhiteSpace(NewPassword) || !string.IsNullOrWhiteSpace(ConfirmPassword);
        }
        private bool CanSubmit(object p)
        {
            bool isMatch = NewPassword == ConfirmPassword;
            return !string.IsNullOrWhiteSpace(OldPassword) && !string.IsNullOrWhiteSpace(NewPassword) && !string.IsNullOrWhiteSpace(ConfirmPassword) && isMatch;
        }
        #endregion
    }
}
