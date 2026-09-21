using ISCALE.Core.Abstract;
using ISCALE.MVVM.Model;
using ISCALE.MVVM.View.ModalViews;
using ISCALE.MVVM.ViewModel;
using ISCALE.MVVM.ViewModel.ModalVMS;
using ISCALE.Utilities;
using ISCALE.Utilities.Services;
using Microsoft.Data.Sqlite;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ISCALE.MVVM.View.OperatorViews
{
    public partial class WeigherView : Window
    {
        private readonly SqliteConnection _connection;
        private readonly User _supervisor;
        private Window nextWindow;
        private readonly TransactionServices _transactionServices;
        private TransactionRecoverPayload _transactionRecoverPayload;
        private Transaction _recoverTransaction;
        public WeigherView(User supervisor, SqliteConnection connection)
        {
            InitializeComponent();
            _connection = connection;
            _supervisor = supervisor;
            _transactionServices = new TransactionServices(connection);
            _transactionRecoverPayload = new()
            {
                Supervisor = supervisor,
                Weigher = ""
            };

            nextWindow = new OperatorView();
            TBX_SCALE_NO.Focus();
            BTN_CONFIRM.IsEnabled = false;
        }

        private void BTN_CONFIRM_Click(object sender, RoutedEventArgs e)
        {
            Confirm();
        }
        private async void Confirm()
        {

            if (!string.IsNullOrEmpty(TBX_WEIGHER_NAME.Text))
            {
                FileHelper.DisableWindow();
                ModalResultView loadingModal = new ModalResultView(Application.Current.MainWindow, "loading", "Preparing at the moment...");

                loadingModal.Show();
                await Task.Delay(100);
                var vm = new OperatorVM(_supervisor, TBX_SCALE_NO.Text, TBX_WEIGHER_NAME.Text, _connection, _recoverTransaction);

                loadingModal.Close();
                nextWindow.DataContext = vm;
                nextWindow.Show();
                Application.Current.MainWindow?.Close();
                Application.Current.MainWindow = nextWindow;

            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void ButtonStatusEvent()
        {
            BTN_CONFIRM.IsEnabled = !string.IsNullOrEmpty(TBX_WEIGHER_NAME.Text) && !string.IsNullOrEmpty(TBX_SCALE_NO.Text);
        }
        private void TBX_SCALE_NO_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            ButtonStatusEvent();
        }

        private void TBX_WEIGHER_NAME_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            ButtonStatusEvent();

        }

        private void TBX_WEIGHER_NAME_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                Confirm();
        }

        private void TBX_SCALE_NO_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                Confirm();
        }

        private async void BTN_RECOVER_Click(object sender, RoutedEventArgs e)
        {
            try {
                FORM_SETUP.Visibility = Visibility.Collapsed;
                FORM_RECOVER.Visibility = Visibility.Visible;

                ServiceResult result = await _transactionServices.SearchTransactionRecover(_transactionRecoverPayload);
                if (!result.Success)
                    throw new CustomException(result.ErrorMessage, result.ErrorType);

                if (result.Data is List<Transaction> transactions) {
                    DG_TRANSACTION_ITEM.ItemsSource = transactions; 
                }
            }
            catch (CustomException ex)
            {
                FileHelper.CatchError(ex);
            }
            catch (Exception ex) { 
                FileHelper.CatchSystemError(ex);
            }


        }

        private void BTN_SETUP_Click(object sender, RoutedEventArgs e)
        {
            FORM_RECOVER.Visibility = Visibility.Collapsed;
            FORM_SETUP.Visibility = Visibility.Visible;
        }

        private async void BTN_SELECT_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Button button = (Button)sender;
                var row = (Transaction) button.DataContext;
                _recoverTransaction = row;
                FileHelper.DisableWindow();
                ModalConfirmationVM dialogVM = new ModalConfirmationVM("Recover", "Are you sure you want to continue this transaction?");
                ModalConfirmationView dialog = new ModalConfirmationView
                {
                    DataContext = dialogVM,
                    Owner = Application.Current.MainWindow
                };
                dialogVM.CloseAction = (result) => dialog.DialogResult = result;

                bool? result = dialog.ShowDialog();

                if (result == true)
                {
                    ServiceResult itemResult = await _transactionServices.SearchTransactionReportItem(_recoverTransaction.JobId, true);
                    if (!itemResult.Success)
                        throw new CustomException(itemResult.ErrorMessage, itemResult.ErrorType);

                    if (itemResult.Data is List<TransactionItem> transactionItems)
                        _recoverTransaction.TransactionItems = new ObservableCollection<TransactionItem>(transactionItems.AsEnumerable().Reverse());

                    TBX_SCALE_NO.Text = row.ScaleNo;
                    TBX_WEIGHER_NAME.Text = row.Weigher;
                    Confirm();
                }
                else
                    FileHelper.EnableWindow();

            }
            catch (Exception ex)
            {
                FileHelper.CatchSystemError(ex);
            }
        }
    }
}
