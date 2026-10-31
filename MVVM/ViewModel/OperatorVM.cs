using Formatter.Helper;
using ISCALE.Core.Abstract;
using ISCALE.Core.Enum;
using ISCALE.MVVM.Model;
using ISCALE.MVVM.Model.ServiceModel;
using ISCALE.MVVM.View.ModalViews;
using ISCALE.MVVM.View.OperatorViews.ModalView;
using ISCALE.MVVM.ViewModel.ModalVMS;
using ISCALE.MVVM.ViewModel.OperatorVMS.ComponentVM;
using ISCALE.MVVM.ViewModel.OperatorVMS.ModalVM;
using ISCALE.Utilities;
using ISCALE.Utilities.Services;
using Microsoft.Data.Sqlite;
using MVVMCORE;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Xml;
namespace ISCALE.MVVM.ViewModel
{
    public class OperatorVM : AOperatorVM
    {
        private readonly SqliteConnection _connection;
        private TransactionServices _transactionServices;

        private readonly User _currentUser;
        private readonly string _weigher;
        private readonly string _scaleNo;

        private string _avatar;
        public string Avatar
        {
            get { return _avatar; }
            set { _avatar = value; OnPropertyChanged(); }
        }

        #region init
        #region Serial Port Init
        private string _scaleComport;
        public string ScaleComport
        {
            get { return _scaleComport; }
            set { _scaleComport = value; OnPropertyChanged(); }
        }
        private string _serialData;
        public string SerialData
        {
            get { return _serialData; }
            set { _serialData = value; OnPropertyChanged(); }
        }
        #endregion

        public ObservableCollection<ProductItem> ItemList { get; set; } = new ObservableCollection<ProductItem>();

        public List<TransactionItem> _deletedTransactionItemList;

        private Transaction _currentTransaction;
        public Transaction CurrentTransaction
        {
            get { return _currentTransaction; }
            set
            {
                if (_currentTransaction != null)
                {
                    // Unhook previous
                    _currentTransaction.TransactionItems.CollectionChanged -= OnTransactionItemsChanged;
                    foreach (var item in _currentTransaction.TransactionItems)
                        item.PropertyChanged -= OnTransactionItemPropertyChanged;
                }

                _currentTransaction = value;

                if (_currentTransaction != null)
                {
                    _currentTransaction.TransactionItems.CollectionChanged += OnTransactionItemsChanged;
                    foreach (var item in _currentTransaction.TransactionItems)
                        item.PropertyChanged += OnTransactionItemPropertyChanged;
                }
            }
        }

        private bool _isScaleStarted = false;

        private string _scaleTimer;
        public string ScaleTimer
        {
            get { return _scaleTimer; }
            set { _scaleTimer = value; OnPropertyChanged(); }
        }

        private Style _scaleStyle;

        public Style ScaleStyle
        {
            get { return _scaleStyle; }
            set { _scaleStyle = value; OnPropertyChanged(); }
        }

        private bool _isStable;

        public bool IsStable
        {
            get { return _isStable; }
            set { _isStable = value; OnPropertyChanged(); }
        }

        private string _code;
        public string Code
        {
            get { return _code; }
            set
            {
                _code = value;
                if (!String.IsNullOrEmpty(_code))
                {
                    SearchItem();
                }
                OnPropertyChanged();
            }
        }

        public ObservableCollection<string> Sections { get; set; }
        private string _selectedSection;

        public string SelectedSection
        {
            get { return _selectedSection; }
            set { _selectedSection = value; OnPropertyChanged(); }
        }


        #region Visibilities
        private Visibility _demoButtonVisibility;

        public Visibility DemoButtonVisibility
        {
            get { return _demoButtonVisibility; }
            set { _demoButtonVisibility = value; OnPropertyChanged(); }
        }


        private Visibility _settingVisibility = Visibility.Visible;
        public Visibility SettingVisibility
        {
            get { return _settingVisibility; }
            set { _settingVisibility = value; OnPropertyChanged(); }
        }

        private Visibility _formVisibility;
        public Visibility FormVisibility
        {
            get { return _formVisibility; ; }
            set { _formVisibility = value; OnPropertyChanged(); }
        }

        private Visibility _scaleVisibility;
        public Visibility ScaleVisibility
        {
            get { return _scaleVisibility; }
            set { _scaleVisibility = value; OnPropertyChanged(); }
        }

        private Visibility _tableVisibility;
        public Visibility TableVisibility
        {
            get { return _tableVisibility; }
            set { _tableVisibility = value; OnPropertyChanged(); }
        }
        #endregion


        private Visibility _failedIconVisibility;
        public Visibility FailedIconVisibility
        {
            get { return _failedIconVisibility; }
            set { _failedIconVisibility = value; OnPropertyChanged(); }
        }

        private Visibility _successIconVisibility;
        public Visibility SuccessIconVisibility
        {
            get { return _successIconVisibility; }
            set { _successIconVisibility = value; OnPropertyChanged(); }
        }

        private string _shiftMessageResult;

        public string ShiftMessageResult
        {
            get { return _shiftMessageResult; }
            set { _shiftMessageResult = value; OnPropertyChanged(); }
        }

        private ProductServices _itemServices;
        private ScheduleServices _scheduleServices;
        private ScaleVM _scaleVM;
        private Transaction _recoverTransaction;
        private bool isRecoveryMode;
        #endregion

        public OperatorVM(User currentUser, string scaleNo, string weigher, SqliteConnection connection, Transaction recoverTransaction = null) : base(currentUser)
        {
            _connection = connection;
            _itemServices = new ProductServices(_connection);
            _scheduleServices = new ScheduleServices(_connection);
            _transactionServices = new TransactionServices(_connection);
            _currentUser = currentUser;
            _avatar = Constants.OPERATOR_AVATAR;
            Sections = Constants.SECTION_COLLECTION;
            _weigher = weigher;
            _scaleNo = scaleNo;

            isRecoveryMode = recoverTransaction != null;
            _recoverTransaction = recoverTransaction;
            _demoButtonVisibility = Constants.DEMO ? Visibility.Visible : Visibility.Hidden;
            _deletedTransactionItemList = new List<TransactionItem>();
            InitTransaction();
            LoadSetting();

            _scaleVM = new ScaleVM(
                _connection,
                () => ScaleComport, (value) => ScaleComport = value,
                () => SerialData, (value) => SerialData = value,
                () => ScaleTimer, (value) => ScaleTimer = value,
                () => ScaleStyle, (value) => ScaleStyle = value,
                () => CurrentTransaction, (value) => CurrentTransaction = value,
                () => FailedIconVisibility, (value) => FailedIconVisibility = value,
                () => SuccessIconVisibility, (value) => SuccessIconVisibility = value,
                1,
                4
                );
        }

        #region Relay
        public RelayCommand FinishCommand => new RelayCommand(Finish, CanFinish);
        public RelayCommand DeleteCommand => new RelayCommand(Delete);
        public RelayCommand AddCommand => new RelayCommand(Add);
        public RelayCommand LogoutCommand => new RelayCommand(Logout);
        public RelayCommand SettingCommand => new RelayCommand(OpenSetting, CanOpenSetting);
        public RelayCommand StartScaleCommand => new RelayCommand(StartScale, CanStartScale);
        public RelayCommand DemoCaptureCommand => new RelayCommand(DemoCapture, CanDemoCapture);
        #endregion

        #region Command

        private void DemoCapture(object parameter)
        {
            // do some append here to have item in the table
            Random random = new Random();
            decimal max = _scaleVM.SelectedItem.UpperLimit + 50m;
            decimal min = 0m;
            decimal value = min + ((decimal)random.NextDouble() * (max - min));
            _scaleVM.SerialData = Math.Round(value, 2).ToString();
            _scaleVM.LiveAppend();
        }

        private void Add(object parameter)
        {
            TransactionItem newItem = new TransactionItem
            {
                Id = CurrentTransaction.TransactionItems.Count + 1,
                Weight = 90,
                Status = "Failed",
                StatusForeground = Brushes.Red,
                IsDeleted = false
            };

            CurrentTransaction.TransactionItems.Insert(0, newItem);
        }

        private async void Delete(object parameter)
        {
            if (!(parameter is TransactionItem item)) return;
            ModalResultView loadingModal = new ModalResultView(Application.Current.MainWindow, "loading", "Auto Save...");
            FileHelper.DisableWindow();
            loadingModal.Show();
            await Task.Delay(800);
            try
            {
                Transaction newTransaction = new Transaction
                {
                    JobId = CurrentTransaction.JobId,
                };

                // re-assign again without the selected item
                foreach (var newItem in CurrentTransaction.TransactionItems)
                    newTransaction.TransactionItems.Add(newItem);

                // update the computation
                UpdateSuccessCount(newTransaction);
                ServiceResult deleteItem = await _transactionServices.DeleteTransactionItem(item, newTransaction);
                if (!deleteItem.Success)
                    throw new CustomException(deleteItem.ErrorMessage, deleteItem.ErrorType);
                loadingModal.Close();
                FileHelper.EnableWindow();
            }
            catch (CustomException ex)
            {
                item.IsDeleted = !item.IsDeleted;
                loadingModal.Close();
                FileHelper.CatchError(ex);
            }
            catch (Exception ex)
            {
                item.IsDeleted = !item.IsDeleted;
                loadingModal.Close();
                FileHelper.CatchSystemError(ex);
            }
        }
        private async void ResetToDefault()
        {
            SelectedSection = null;
            _recoverTransaction = null;
            isRecoveryMode = false;
            await ReInitTransaction();
            ScaleTimer = String.Empty;
            Application.Current.Dispatcher.Invoke(() =>
            {
                SerialData = "0";
            });
            _isScaleStarted = false;

            if (Constants.DEMO) return;
            _scaleVM.CloseSerialPort();
        }
        private async void Finish(object parameter)
        {
            // Stop scale here
            try
            {
                if (!Constants.DEMO)
                    _scaleVM.CloseSerialPort();

                if (CurrentTransaction.TransactionItems.Where(x => x.IsDeleted == true).Count() > 0)
                    await ProcessDelete();
                else
                    await FinishTransaction();

            }
            catch (Exception ex)
            {
                FileHelper.CatchSystemError(ex);
            }
          

        }


        private void OpenSetting(object parameter)
        {
            bool _isFormNotEnabled = !IsFormEnabled;
            FileHelper.DisableWindow();
            OperatorSettingModalVM dialogVM = new(ScaleComport);
            OperatorSettingModalView dialog = new()
            {
                DataContext = dialogVM,
                Owner = Application.Current.MainWindow
            };
            
            // close port first
            if (_isScaleStarted && !Constants.DEMO)
                _scaleVM.CloseSerialPort();

            // get next action
            dialogVM.CloseAction = (result) => dialog.DialogResult = result;
            bool? result = dialog.ShowDialog();

            // set up again the port if started
            // start port if scale started
            _scaleComport = dialogVM.ScaleComport;
            if(_isScaleStarted && !Constants.DEMO) 
                _scaleVM.InitializedPort();

            FileHelper.EnableWindow();
        }
        private async void StartScale(object parameter)
        {
            await LoadShift();
        }

        #endregion

        #region CanCommand

        private bool CanOpenSetting(object parameter) {
            return !Constants.DEMO;
        }

        private bool CanDemoCapture(object parameter)
        {
            return Constants.DEMO;
        }
        private bool CanStartScale(object parameter)
        {
            return !string.IsNullOrEmpty(CurrentTransaction?.Weigher) &&
                !string.IsNullOrEmpty(CurrentTransaction?.OperatorNo) &&
                !string.IsNullOrEmpty(CurrentTransaction?.PackerNo) &&
                !string.IsNullOrEmpty(CurrentTransaction?.MachineNo) &&
                !string.IsNullOrEmpty(SelectedSection) &&
                CurrentTransaction?.Item != null;
        }

        private bool CanFinish(object parameter)
        {
            return CurrentTransaction?.TransactionItems.Where(x => x.IsDeleted == false).Count() > 0;
        }
        #endregion

        #region Functions
        private async Task InitTransaction()
        {
            try
            {
                IsFormEnabled = false;
                var transaction = new Transaction
                {
                    Weigher = _weigher,
                    ScaleNo = _scaleNo,
                    Supervisor = CurrentUser,
                };
                
                CurrentTransaction = isRecoveryMode ? _recoverTransaction : transaction;
                SelectedSection = CurrentTransaction.Section;
                
                CurrentTransaction.TransactionItems.CollectionChanged += OnTransactionItemsChanged;
                await ReInitTransaction();
                Code = isRecoveryMode ? CurrentTransaction.Item.Code : null;
                InitVisibility();
                ScaleStyle = (Style)Application.Current.FindResource("NormalAlert_patched");
            }
            catch (Exception ex)
            {

            }
        }

        private async Task ReInitTransaction()
        {
            try
            {
                IsFormEnabled = !isRecoveryMode;
                string uniqueId = Guid.NewGuid().ToString();
                DateTime createdAt = isRecoveryMode && _recoverTransaction?.CreatedAt != null ? DateTime.Parse(DateTimeHelper.DateUtcToLocal(_recoverTransaction.CreatedAt)) : DateTime.Now.ToLocalTime();
                string dateCode = createdAt.Date.ToString("yyMMdd");
                ServiceResult result = await _transactionServices.GetLatestTransactionId(dateCode);
                if (!result.Success)
                    throw new CustomException(result.ErrorMessage, result.ErrorType);
                string transactionId = result.Data.ToString() ?? string.Empty;

                // Populate Transaction whether new or recover
                CurrentTransaction.JobId = isRecoveryMode && !string.IsNullOrEmpty(_recoverTransaction?.JobId) ? _recoverTransaction.JobId : uniqueId;
                CurrentTransaction.Code = dateCode;

                CurrentTransaction.CreatedAt = createdAt;
                CurrentTransaction.Item = isRecoveryMode ? _recoverTransaction.Item : null;
                CurrentTransaction.TransactionId = transactionId;

                if (!isRecoveryMode) CurrentTransaction.TransactionItems.Clear();
                CurrentTransaction.TransactionItems.CollectionChanged -= OnTransactionItemsChanged;
                CurrentTransaction.TransactionItems.CollectionChanged += OnTransactionItemsChanged;

                Code = null;
                InitVisibility();
                ScaleStyle = (Style)Application.Current.FindResource("NormalAlert_patched");
            }
            catch (CustomException ex)
            {

            }
            catch (Exception ex)
            {

            }
        }


        private void LoadSetting()
        {
            // Load Setting xml
            if (!File.Exists(FileHelper.getCurrentDirectory(RoleEnum.SUPERVISOR) + Constants.SUPERVISOR_SETTING_FILE))
                FileHelper.initSetting();

            // Load XML
            try
            {
                XmlDocument scaleDoc = new XmlDocument();
                scaleDoc.Load(FileHelper.getCurrentDirectory(RoleEnum.SUPERVISOR) + Constants.SUPERVISOR_SETTING_FILE);
                XmlNodeList scaleNodes = scaleDoc.SelectNodes("default_settings");
                foreach (XmlNode node in scaleNodes)
                {
                    if (node.ChildNodes.Count == 1)
                    {
                        ScaleComport = node.ChildNodes[0].InnerText;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                FileHelper.CatchSystemModalError(ex);
            }
        }

        private async Task SaveTransaction()
        {
            FileHelper.DisableWindow();
            ModalResultView loadingModal = new ModalResultView(Application.Current.MainWindow, "loading", "Saving progress... Please wait.");
            loadingModal.Show();
            await Task.Delay(500);
            try
            {
                CurrentTransaction.Section = SelectedSection;
                if (!isRecoveryMode)
                {
                    ServiceResult addResult = await _transactionServices.AddTransaction(CurrentTransaction);
                    if (!addResult.Success)
                        throw new CustomException(addResult.ErrorMessage, addResult.ErrorType);
                }

                loadingModal.Close();
                FileHelper.EnableWindow();
                FormVisibility = Visibility.Collapsed;
                ScaleVisibility = Visibility.Visible;
                TableVisibility = Visibility.Visible;
                IsFormEnabled = false;
                _isScaleStarted = true;
                _scaleVM.SelectedItem = CurrentTransaction.Item;

                if (Constants.DEMO) return;
                _scaleVM.InitializedPort();
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
        private async Task ProcessDelete()
        {
            FileHelper.DisableWindow();
            List<TransactionItem> deletedItems = CurrentTransaction.TransactionItems.Where(x => x.IsDeleted == true).ToList();
            ModalSupervisorAuthVM dialogVM = new ModalSupervisorAuthVM(_currentUser, deletedItems, _connection);
            ModalSupervisorAuthView dialogView = new ModalSupervisorAuthView()
            {
                DataContext = dialogVM,
                Owner = Application.Current.MainWindow
            };
            dialogVM.CloseAction = (result) => dialogView.DialogResult = result;
            bool? result1 = dialogView.ShowDialog();
            if (result1 == true)
            {
                CurrentTransaction.DeleteNote = dialogVM.Note;
                await FinishTransaction();
            }
            else
            {
                FileHelper.EnableWindow();
                if (!Constants.DEMO)
                    _scaleVM.InitializedPort();
            }
        }
        private async Task FinishTransaction()
        {
            FileHelper.DisableWindow();
            ModalResultView loadingModal = new ModalResultView(Application.Current.MainWindow, "loading");
            loadingModal.Show();
            try
            {
                CurrentTransaction.FinishedAt = DateTime.UtcNow;
                ServiceResult addResult = await _transactionServices.UpdateTransaction(CurrentTransaction);
                if (!addResult.Success)
                    throw new CustomException(addResult.ErrorMessage, addResult.ErrorType);
                loadingModal.Close();
                ModalResultView successModal = new ModalResultView(Application.Current.MainWindow, "success", "Transaction Saved!");
                successModal.ShowDialog();
                ResetToDefault();
                FileHelper.EnableWindow();
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

        private async Task LoadShift()
        {
            ModalResultView loadingModal = new ModalResultView(Application.Current.MainWindow, "loading", "Checking today shift...");
            try
            {
                FileHelper.DisableWindow();
                loadingModal.Show();
                await Task.Delay(500);
                ServiceResult loadResult = await _scheduleServices.Load();
                if (!loadResult.Success)
                    throw new CustomException(loadResult.ErrorMessage, loadResult.ErrorType);
                bool hasError = false;

                DateTime CurrentDate = CurrentTransaction.CreatedAt;
                if (loadResult.Data is Schedule schedule)
                {
                    // assign init values from db
                    TimePickerModel DayShiftStart = GenerateCbxValue(schedule.DayShiftStart.ToString());
                    TimePickerModel DayShiftEnd = GenerateCbxValue(schedule.DayShiftEnd.ToString());
                    TimePickerModel NightShiftStart = GenerateCbxValue(schedule.NightShiftStart.ToString());
                    TimePickerModel NightShiftEnd = GenerateCbxValue(schedule.NightShiftEnd.ToString());

                    // Day Shift Start
                    TimeSpan dayStartSpan = new(DayShiftStart.Hour, DayShiftStart.Minute, 0);
                    DateTime dayStart = CurrentDate.Date + dayStartSpan;
                    string DayShiftStartPreview = dayStart.ToString("hh:mm tt");

                    // Day Shift End
                    TimeSpan dayEndSpan = new(DayShiftEnd.Hour, DayShiftEnd.Minute, 0);
                    DateTime dayEnd = CurrentDate.Date + dayEndSpan;
                    if (dayEnd <= dayStart)
                        dayEnd = dayEnd.AddDays(1);
                    string DayShiftEndPreview = dayEnd.ToString("hh:mm tt");

                    // Night Shift Start
                    TimeSpan nightStartSpan = new(NightShiftStart.Hour, NightShiftStart.Minute, 0);
                    DateTime nightStart = CurrentDate.Date + nightStartSpan;
                    if (nightStart < dayEnd)
                        nightStart = nightStart.AddDays(1);
                    string NightShiftStartPreview = nightStart.ToString("hh:mm tt");

                    // Night Shift End
                    TimeSpan nightEndSpan = new(NightShiftEnd.Hour, NightShiftEnd.Minute, 0);
                    DateTime nightEnd = CurrentDate.Date + nightEndSpan;
                    if (nightEnd <= nightStart)
                        nightEnd = nightEnd.AddDays(1);
                    string NightShiftEndPreview = nightEnd.ToString("hh:mm tt");

                    // Constraint

                    bool valid =
                        dayStart < dayEnd &&
                        dayEnd <= nightStart &&
                        nightStart < nightEnd &&
                        nightEnd <= dayStart.AddDays(1) &&
                        (dayEnd - dayStart).TotalMinutes > 0 &&
                        (nightEnd - nightStart).TotalMinutes > 0;

                    bool allShiftsReasonable =
                        (dayEnd - dayStart).TotalHours <= 24 &&
                        (nightEnd - nightStart).TotalHours <= 24;

                    if (!(valid && allShiftsReasonable))
                        throw new CustomException(Constants.INVALID_SCHEDULE_MESSAGE, Constants.INVALID_SCHEDULE);

                    loadingModal.Close();
                    string dayshiftRange = $"{DayShiftStartPreview} - {DayShiftEndPreview}";
                    string nightshiftRange = $"{NightShiftStartPreview} - {NightShiftEndPreview}";

                    // Previous Date 
                    DateTime prevNightStart = nightStart.AddDays(-1);
                    DateTime prevNightEnd = nightEnd.AddDays(-1);

                    if (
                        (CurrentTransaction.CreatedAt >= dayStart && CurrentTransaction.CreatedAt < dayEnd)
                        )
                    {
                        CurrentTransaction.ScheduleShift = dayshiftRange;
                        await SaveTransaction();
                    }
                    else if (
                        (CurrentTransaction.CreatedAt >= nightStart && CurrentTransaction.CreatedAt < nightEnd) ||
                        (CurrentTransaction.CreatedAt >= prevNightStart && CurrentTransaction.CreatedAt < prevNightEnd)
                    )
                    {
                        CurrentTransaction.ScheduleShift = nightshiftRange;
                        await SaveTransaction();
                    }
                    else
                    {
                        // no shift at the moment
                        ModalResultView noShiftModal = new ModalResultView(Application.Current.MainWindow, "error", $"Today shift is {dayshiftRange} and {nightshiftRange}");
                        noShiftModal.ShowDialog();
                        FileHelper.EnableWindow();
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
        private TimePickerModel GenerateCbxValue(string time)
        {
            string[] timeSplit = time.Split(":");
            if (timeSplit.Length != 2)
                throw new Exception("Invalid time format");

            if (!int.TryParse(timeSplit[0], out int hour) || hour < 0 || hour >= 24)
                throw new Exception("Invalid hour value.");

            if (!int.TryParse(timeSplit[1], out int minute) || minute < 0 || minute >= 60)
                throw new Exception("Invalid minute value.");

            return new TimePickerModel
            {
                Hour = hour,
                Minute = minute
            };
        }

        protected override void OnFormClose()
        {
            if (_isScaleStarted && !Constants.DEMO)
                _scaleVM.CloseSerialPort();
        }

        protected override void OnLogoutCancel() {
            if (_isScaleStarted && !Constants.DEMO)
                _scaleVM.InitializedPort();
        }

        private void InitVisibility()
        {
            FormVisibility = Visibility.Visible;
            ScaleVisibility = Visibility.Collapsed;
            TableVisibility = Visibility.Collapsed;
        }

        private async Task SearchItem()
        {
            ProductItem item = new()
            {
                Code = Code
            };
            ServiceResult findResult = await _itemServices.FindItem(item);
            if (findResult.Success)
            {
                if (findResult.Data is List<ProductItem> castItemList)
                {
                    if (castItemList.Count > 0)
                        CurrentTransaction.Item = castItemList[0];
                    else
                        CurrentTransaction.Item = null;
                }
            }

        }
        private void OnTransactionItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TransactionItem.IsDeleted))
            {
                UpdateSuccessCount(CurrentTransaction);
            }
        }
        private void OnTransactionItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (TransactionItem newItem in e.NewItems)
                    newItem.PropertyChanged += OnTransactionItemPropertyChanged;
            }

            if (e.OldItems != null)
            {
                foreach (TransactionItem oldItem in e.OldItems)
                    oldItem.PropertyChanged -= OnTransactionItemPropertyChanged;
            }

            UpdateSuccessCount(CurrentTransaction);
        }

        private void UpdateSuccessCount(Transaction _transaction)
        {
            _transaction.SuccessCount = _transaction.TransactionItems.Where(x => x.IsDeleted == false && x.Status == "Success").Count();
            _transaction.FailedCount = _transaction.TransactionItems.Where(x => x.IsDeleted == false && x.Status == "Failed").Count();
            _transaction.DeletedCount = _transaction.TransactionItems.Where(x => x.IsDeleted == true).Count();
            _transaction.TotalCount = _transaction.TransactionItems.Count();
            _transaction.TotalFailedWeight = _transaction.TransactionItems.Where(x => x.IsDeleted == false && x.Status == "Failed").Sum(y => y.Weight);
            _transaction.TotalSuccessWeight = _transaction.TransactionItems.Where(x => x.IsDeleted == false && x.Status == "Success").Sum(y => y.Weight);

            if (_transaction.SuccessCount > 0)
            {
                decimal avg = _transaction.TotalSuccessWeight / _transaction.SuccessCount;
                _transaction.AverageWeight = avg;
            }
            else
                _transaction.AverageWeight = 0;
        }

        #endregion
    }
}