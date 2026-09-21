using ISCALE.MVVM.Model;
using ISCALE.Utilities;
using ISCALE.Utilities.Services;
using LiveWeightScale;
using Microsoft.Data.Sqlite;
using NAudio.Wave;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
namespace ISCALE.MVVM.ViewModel.OperatorVMS.ComponentVM
{
    public class ScaleVM : AStableWeightScale
    {
        private TransactionServices _transactionServices;

        #region OperatorReference
        private readonly Func<string> _getStatus;
        private readonly Action<string> _setStatus;

        private readonly Func<Style> _getWeightStyle;
        private readonly Action<Style> _setWeightStyle;

        private readonly Func<Transaction> _getCurrentTransaction;
        private readonly Action<Transaction> _setCurrentTransaction;

        private readonly Func<Visibility> _getFailedIconVisibility;
        private readonly Action<Visibility> _setFailedIconVisibility;

        private readonly Func<Visibility> _getSuccessIconVisibility;
        private readonly Action<Visibility> _setSuccessIconVisibility;
        #endregion

        #region Form Bind
        private bool _isReset;
        public bool IsReset
        {
            get { return _isReset; }
            set { _isReset = value; OnPropertyChanged(); }
        }

        private ProductItem _selectedItem;
        public ProductItem SelectedItem
        {
            get { return _selectedItem; }
            set { _selectedItem = value; }
        }

        private Visibility FailedIconVisibility
        {
            get => _getFailedIconVisibility();
            set { _setFailedIconVisibility(value); }
        }

        private Visibility SuccessIconVisibility
        {
            get => _getSuccessIconVisibility();
            set { _setSuccessIconVisibility(value); }
        }

        private Transaction CurrentTransaction
        {
            get => _getCurrentTransaction();
            set { _setCurrentTransaction(value); OnPropertyChanged(); }
        }

        private string Status
        {
            get => _getStatus();
            set { _setStatus(value); OnPropertyChanged(); }
        }

        private Style ScaleStyle
        {
            get => _getWeightStyle();
            set { _setWeightStyle(value); OnPropertyChanged(); }
        }
        #endregion

        private readonly Style _successStyle = (Style)Application.Current.FindResource("SuccessAlert_patched");
        private readonly Style _normalStyle = (Style)Application.Current.FindResource("NormalAlert_patched");
        private readonly Style _failedStyle = (Style)Application.Current.FindResource("ErrorAlert_patched");

        public ScaleVM(
            SqliteConnection connection,
            Func<string> getScaleComport, Action<string> setScaleComport,
            Func<string> getSerialData, Action<string> setSerialData,
            Func<string> getStatus, Action<string> setStatus,
            Func<Style> getWeightStyle, Action<Style> setWeightStyle,
            Func<Transaction> getCurrentTransaction, Action<Transaction> setCurrentTransaction,
            Func<Visibility> getFailedIconVisibility, Action<Visibility> setFailedIconVisibility,
            Func<Visibility> getSuccessIconVisibility, Action<Visibility> setSuccessIconVisibility,
            short maxSize,
            short threadSleep
            ) : base(getScaleComport, setScaleComport, getSerialData, setSerialData, maxSize, threadSleep)
        {
            _transactionServices = new TransactionServices(connection);
            _getStatus = getStatus;
            _setStatus = setStatus;

            _getWeightStyle = getWeightStyle;
            _setWeightStyle = setWeightStyle;

            _getCurrentTransaction = getCurrentTransaction;
            _setCurrentTransaction = setCurrentTransaction;

            _getFailedIconVisibility = getFailedIconVisibility;
            _setFailedIconVisibility = setFailedIconVisibility;

            _getSuccessIconVisibility = getSuccessIconVisibility;
            _setSuccessIconVisibility = setSuccessIconVisibility;

            IsReset = true;

            FailedIconVisibility = Visibility.Collapsed;
            SuccessIconVisibility = Visibility.Collapsed;
        }

        public override void SerialPortInitError(Exception ex)
        {
            ErrorHandler(ex);
        }

        public override void SerialPortRuntimeError(Exception ex)
        {
            ErrorHandler(ex);
        }

        private void ErrorHandler(Exception ex)
        {
            FileHelper.DisableWindow();
            FileHelper.CatchSystemError(ex);
        }
        public override async void LiveAppend()
        {
            if (string.IsNullOrEmpty(CurrentTransaction.TransactionId)) return;
            decimal weight = 0;
            if (decimal.TryParse(SerialData, out decimal w)) weight = w;
            else return;
            if (weight == 0) return;

            bool withinRange = IsWithinRange(weight);


            int idCount = CurrentTransaction.TransactionItems.Count;

            TransactionItem transactionItem = new TransactionItem
            {
                Id = idCount + 1,
                TempId = Guid.NewGuid().ToString(),
                Weight = weight,
                IsDeleted = false,
                Status = withinRange ? "Success" : "Failed",
                StatusForeground = withinRange ? Brushes.LimeGreen : Brushes.Red,
            };
            // Create new Transaction
            Transaction newTransaction = new Transaction
            {
                Code = CurrentTransaction.Code,
                JobId = CurrentTransaction.JobId,
            };

            // Insert items
            foreach (var item in CurrentTransaction.TransactionItems)
                newTransaction.TransactionItems.Add(item);
            
            // Add new item
            newTransaction.TransactionItems.Insert(0, transactionItem);

            var success_item = newTransaction.TransactionItems.Where(x => x.IsDeleted == false && x.Status == "Success");
            var failed_item = newTransaction.TransactionItems.Where(x => x.IsDeleted == false && x.Status == "Failed");
            // Assign future values
            newTransaction.SuccessCount = success_item.Count();
            newTransaction.FailedCount = failed_item.Count();

            newTransaction.TotalFailedWeight = failed_item.Sum(y => y.Weight);
            newTransaction.TotalSuccessWeight = success_item.Sum(y => y.Weight);

            newTransaction.TotalCount = newTransaction.TransactionItems.Count();

            if (newTransaction.SuccessCount > 0)
            {
                decimal avg = newTransaction.TotalSuccessWeight / newTransaction.SuccessCount;
                newTransaction.AverageWeight = Math.Truncate(avg * 100) / 100;
            }
            else
                newTransaction.AverageWeight = 0;
            
            // Submit  
            ServiceResult addResult = await _transactionServices.AddTransactionItem(transactionItem, newTransaction);
            if (!addResult.Success)
                throw new CustomException(addResult.ErrorMessage, addResult.ErrorType);
            
            // Apply when success
            await Application.Current.Dispatcher.BeginInvoke(() =>
            {
                CurrentTransaction.TransactionItems.Insert(0, transactionItem);
            });
        }
        public override void LiveUpdate()
        {
            decimal weight = 0;
            if (decimal.TryParse(SerialData, out decimal w)) weight = w;
            else return;

            bool withinRange = IsWithinRange(weight);

            ScaleStyle = withinRange ? _successStyle : _failedStyle;
            FailedIconVisibility = withinRange ? Visibility.Collapsed : Visibility.Visible;
            SuccessIconVisibility = withinRange ? Visibility.Visible : Visibility.Collapsed;

            if (weight == 0)
            {
                IsReset = true;
                Status = string.Empty;
                ScaleStyle = _normalStyle;
                FailedIconVisibility = Visibility.Collapsed;
                SuccessIconVisibility = Visibility.Collapsed;
            }
        }
        private bool IsWithinRange(decimal weight)
        {
            return weight <= SelectedItem.UpperLimit && weight >= SelectedItem.LowerLimit;
        }
        public override string StringFormat(string data)
        {
            string weightNumber = "";
            foreach (var item in data)
            {
                if (byte.TryParse(item.ToString(), out byte number) || item == '.')
                {
                    weightNumber += item;
                }
            }
            return weightNumber;
        }
        public override void CleanUp()
        { }
    }
}
