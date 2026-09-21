using ISCALE.MVVM.Model;
using ISCALE.MVVM.View.ModalViews;
using ISCALE.MVVM.View.OperatorViews;
using ISCALE.MVVM.ViewModel;
using ISCALE.Utilities;
using ISCALE.Utilities.Services;
using Microsoft.Data.Sqlite;
using System.Windows;
using System.Windows.Input;

namespace ISCALE.MVVM.View
{
    public partial class LoginView : Window
    {
        private readonly SqliteConnection _connection;
        private User currentUser;
        private AuthServices _authServices;
        public LoginView()
        {
            InitializeComponent();
            _connection = SqliteDBConfig.GetConnection();
            _authServices = new AuthServices(_connection);
            TBX_USERNAME.Focus();
            currentUser = new User();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void BTN_LOGIN_Click(object sender, RoutedEventArgs e)
        {
            Login();
        }

        private void TBX_USERNAME_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                Login();
        }

        private async Task Login()
        {
            ModalResultView loadingModal = new ModalResultView(Application.Current.MainWindow, "loading", "Logging In...");
            loadingModal.Show();
            FileHelper.DisableWindow();
            await Task.Delay(200);
            try
            {
                if (!string.IsNullOrEmpty(TBX_USERNAME.Text.Trim()))
                {
                    ServiceResult loginResult = await _authServices.Login(TBX_USERNAME.Text.Trim(), TBX_PASSWORD.Password);
                    if (!loginResult.Success)
                        throw new CustomException(loginResult.ErrorMessage, loginResult.ErrorType);

                    if (loginResult.Data is User user)
                    {
                        Window nextWindow = null;

                        switch (user.Role)
                        {
                            case "Admin":
                                nextWindow = new AdminView { DataContext = new AdminVM(user, _connection) };
                                break;
                            case "Supervisor":
                                nextWindow = new WeigherView(user, _connection);
                                break;
                            default:
                                throw new Exception(Constants.USER_NOT_EXISTED);
                        }

                        string[] excludedRole = { "Supervisor" };

                        var loginWindow = Application.Current.MainWindow;
                        Application.Current.MainWindow = nextWindow;

                        if (!excludedRole.Contains(user.Role))
                        {
                            nextWindow.IsEnabled = false;
                            nextWindow.Opacity = .4;
                        }
                        nextWindow.Show();
                        loadingModal?.Close();
                        loginWindow?.Close();
                    }
                }
            }
            catch (CustomException ex)
            {
                TBX_USERNAME.Focus();
                loadingModal.Close();
                FileHelper.CatchError(ex);
            }
            catch (Exception ex)
            {
                TBX_USERNAME.Focus();
                loadingModal.Close();
                FileHelper.CatchSystemError(ex);
            }
        }

        private void TBX_PASSWORD_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                Login();
        }
    }
}
