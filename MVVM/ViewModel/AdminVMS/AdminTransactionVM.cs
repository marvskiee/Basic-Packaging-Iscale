using ClosedXML.Excel;
using Formatter.Helper;
using ISCALE.Core.Enum;
using ISCALE.MVVM.Model;
using ISCALE.MVVM.View.ModalViews;
using ISCALE.MVVM.ViewModel.ModalVMS;
using ISCALE.Utilities;
using ISCALE.Utilities.Services;
using Microsoft.Data.Sqlite;
using MVVMCORE;
using System.Collections.ObjectModel;
using System.Windows;

namespace ISCALE.MVVM.ViewModel.AdminVMS
{
    public class AdminTransactionVM : ViewModelBase
    {
        private TransactionServices _transactionServices;
        public ObservableCollection<Transaction> Transactions { get; private set; } = new ObservableCollection<Transaction>();
        #region Init
        // TextBox Field
        private string _weigher;
        public string Weigher
        {
            get { return _weigher; }
            set
            {
                _weigher = value;
                if (string.IsNullOrEmpty(_weigher))
                {
                    IsWeigherCheckEnabled = false;
                    IsWeigherSpecific = false;
                }
                else
                    IsWeigherCheckEnabled = true;
                OnPropertyChanged();
            }
        }
     
        // CheckBox Enabled
        private bool _isWeigherCheckEnabled;
        public bool IsWeigherCheckEnabled
        {
            get { return _isWeigherCheckEnabled; }
            set { _isWeigherCheckEnabled = value; OnPropertyChanged(); }
        }
       
        // Checkbox IsChecked
        private bool _isWeigherSpecific;
        public bool IsWeigherSpecific
        {
            get { return _isWeigherSpecific; }
            set { _isWeigherSpecific = value; OnPropertyChanged(); }
        }
       
        // DateTime
        private DateTime _startDate;
        public DateTime StartDate
        {
            get { return _startDate; }
            set { _startDate = value; OnPropertyChanged(); }
        }
        private DateTime _endDate;

        public DateTime EndDate
        {
            get { return _endDate; }
            set { _endDate = value; OnPropertyChanged(); }
        }

        #endregion

        #region Table 
        private bool _isSelectedAll;

        public bool IsSelectedAll
        {
            get { return  _isSelectedAll; }
            set { 
                _isSelectedAll = value;
                foreach (Transaction transaction in Transactions)
                    transaction.IsSelected = _isSelectedAll;

                OnPropertyChanged(); 
            }
        }

        #endregion
        public AdminTransactionVM(SqliteConnection connection)
        {
            _transactionServices = new TransactionServices(connection);
            StartDate = DateTime.Now.ToLocalTime().Date;
            EndDate = DateTime.Now.ToLocalTime().Date;
            IsSelectedAll = false;
        }

        #region Relay
        public RelayCommand SearchCommand => new RelayCommand(Search);
        public RelayCommand ClearCommand => new RelayCommand(Clear);
        public RelayCommand ViewCommand => new RelayCommand(View);
        public RelayCommand ExportReportCommand => new RelayCommand(Export, CanExport);
        #endregion

        #region Command
        private async void Search(object parameter)
        {
            Transactions.Clear();
            FileHelper.DisableWindow();
            ModalResultView loadingModal = new(Application.Current.MainWindow, "loading");
            loadingModal.Show();
            await Task.Delay(500);
            try
            {
                TransactionSearchPayload payload = new()
                {
                    Weigher = Weigher,
                    IsWeigherSpecific = IsWeigherSpecific,
                    StartDate = StartDate.ToUniversalTime(),
                    EndDate = EndDate.ToUniversalTime()
                };
                ServiceResult searchResult = await _transactionServices.SearchTransactionReport(payload);
                if (!searchResult.Success)
                    throw new CustomException(searchResult.ErrorMessage, searchResult.ErrorType);
                if (searchResult.Data is List<Transaction> transactions)
                {
                    loadingModal.Close();
                    FileHelper.EnableWindow();
                    foreach (Transaction transaction in transactions)
                    {
                        transaction.IsSelected = false;
                        Transactions.Add(transaction);
                    }
                }
                else
                    throw new CustomException(Constants.CAST_DATA_ERROR_MESSAGE, Constants.CAST_DATA_ERROR_TYPE);
            }
            catch (CustomException ex)
            {
                loadingModal.Close();
                FileHelper.CatchError(ex);
            }
            catch (Exception ex)
            {
                loadingModal.Close();
                FileHelper.CatchSystemError(ex);
            }
        }
        private async void View(object parameter)
        {
            if (parameter is Transaction transaction)
            {
                FileHelper.DisableWindow();
                ModalResultView loadingModal = new(Application.Current.MainWindow, "loading");
                loadingModal.Show();
                await Task.Delay(500);
                try
                {
                    ServiceResult itemResult = await _transactionServices.SearchTransactionReportItem(transaction.JobId);
                    if (!itemResult.Success)
                        throw new CustomException(itemResult.ErrorMessage, itemResult.ErrorType);
                    loadingModal.Close();

                    if (itemResult.Data is List<TransactionItem> transactionItems)
                    {
                        transaction.TransactionItems = new ObservableCollection<TransactionItem>(transactionItems);
                        ModalTransactionVM dialogVM = new ModalTransactionVM(transaction);
                        ModalTransactionView dialogView = new ModalTransactionView()
                        {
                            Owner = Application.Current.MainWindow,
                            DataContext = dialogVM
                        };

                        dialogVM.CloseAction = (result) => dialogView.DialogResult = result;
                        bool? result2 = dialogView.ShowDialog();
                        FileHelper.EnableWindow();
                    }
                    else
                        throw new CustomException(itemResult.ErrorMessage, itemResult.ErrorType);
                }
                catch (CustomException ex)
                {
                    loadingModal.Close();
                    FileHelper.CatchError(ex);
                }
                catch (Exception ex)
                {
                    loadingModal.Close();
                    FileHelper.CatchSystemError(ex);
                }
            }
        }
        private void Clear(object parameter)
        {
            Weigher = string.Empty;
            IsWeigherCheckEnabled = false;

            StartDate = DateTime.Now.ToLocalTime().Date;
            EndDate = DateTime.Now.ToLocalTime().Date;
        }

        private void Export(object parameter)
        {
            FileHelper.DisableWindow();
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    DateTime timestamp = DateTime.UtcNow;

                    string fileName = $"Report({StartDate.ToString("yy-MM-dd")} - {EndDate.ToString("yy-MM-dd")}) {DateTimeHelper.DateFileName(timestamp)}";
                    string filePath = FileHelper.getCurrentDirectory(RoleEnum.ADMIN) + $"\\Reports\\{fileName}.xlsx";

                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("Report");
                        // Header 
                        for (int h = 1; h <= Constants.TRANSACTION_HEADER.Count(); h++)
                        {
                            worksheet.Cell(1, h).Value = Constants.TRANSACTION_HEADER[h - 1];
                        }
                        int currentRow = 2;
                        // Body
                        List<Transaction> selectedTransactions = Transactions.Where(x => x.IsSelected == true).ToList();
                        for (int t = 0; t < selectedTransactions.Count(); t++)
                        {
                            Transaction TS = selectedTransactions[t];

                            List<string> cells = new List<string>
                            {
                            DateTimeHelper.DateUtcToLocal(TS.CreatedAt),
                            DateTimeHelper.DateUtcToLocal(TS.FinishedAt),
                            TS.ScheduleShift,
                            TS.Section,
                            TS.PackerNo,
                            TS.MachineNo,
                            TS.QaNo,
                            TS.OperatorNo,
                            TS.Supervisor.Username,
                            TS.Weigher ?? "",
                            TS.ScaleNo ?? "",
                            TS.TransactionId,
                            TS.Item.Code,
                            TS.Item.InternalReference,
                            TS.Item.Description,
                            TS.Item.LowerLimit.ToString(),
                            TS.Item.UpperLimit.ToString(),
                            TS.AverageWeight.ToString(),
                            TS.TotalSuccessWeight.ToString(),
                            TS.SuccessCount.ToString(),
                            TS.TotalFailedWeight.ToString(),
                            TS.FailedCount.ToString(),
                            TS.DeletedCount.ToString(),
                            TS.TotalCount.ToString(),
                            TS.DeleteNote
                            };

                            for (int column_count = 0; column_count < cells.Count; column_count++)
                            {
                                worksheet.Cell(currentRow, column_count+1).Value = cells[column_count];
                            }


                            currentRow++;
                        }
                        worksheet.Range("A1:X1").SetAutoFilter();
                        workbook.SaveAs(filePath);
                    }
                });

                // Success State
                ModalResultView successModal = new(Application.Current.MainWindow, "success", $"File is saved at Desktop/AppData/Reports folder");
                successModal.ShowDialog();
                FileHelper.EnableWindow();
            }
            catch (Exception ex)
            {
                FileHelper.CatchSystemError(ex);
            }
        }

        #endregion

        private bool CanExport(object parameter)
        {
            return Transactions.Where(x => x.IsSelected == true).Count() > 0;
        }
    }
}
