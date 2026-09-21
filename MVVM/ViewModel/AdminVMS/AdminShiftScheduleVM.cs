using ISCALE.MVVM.Model;
using ISCALE.MVVM.Model.ServiceModel;
using ISCALE.MVVM.View.ModalViews;
using ISCALE.Utilities;
using ISCALE.Utilities.Services;
using Microsoft.Data.Sqlite;
using MVVMCORE;
using System.ComponentModel;
using System.Globalization;
using System.Windows;

namespace ISCALE.MVVM.ViewModel.AdminVMS
{
    public class AdminShiftScheduleVM : ViewModelBase
    {
        private readonly SqliteConnection _connection;
        private readonly ScheduleServices _scheduleServices;

        private DateTime _dayStartDateTime;
        private DateTime _dayEndDateTime;
        private DateTime _nightStartDateTime;
        private DateTime _nightEndDateTime;

        private DateTime _currentDate;
        public DateTime CurrentDate
        {
            get { return _currentDate; }
            set
            {
                _currentDate = value;
                OnPropertyChanged();
            }
        }
        #region Reference Shift
        #region OriginalDayShift Bind
        private TimePickerModel _originalOriginalDayShiftStart;
        public TimePickerModel OriginalDayShiftStart
        {
            get { return _originalOriginalDayShiftStart; }
            set
            {
                _originalOriginalDayShiftStart = value;
                UpdateOriginalShiftPreview();
                OnPropertyChanged();
            }
        }

        private TimePickerModel _originalOriginalDayShiftEnd;
        public TimePickerModel OriginalDayShiftEnd
        {
            get { return _originalOriginalDayShiftEnd; }
            set
            {
                _originalOriginalDayShiftEnd = value;
                UpdateOriginalShiftPreview();
                OnPropertyChanged();
            }
        }
        #endregion
        #region OriginalNightShift Bind
        private TimePickerModel _originalOriginalNightShiftStart;
        public TimePickerModel OriginalNightShiftStart
        {
            get { return _originalOriginalNightShiftStart; }
            set
            {
                _originalOriginalNightShiftStart = value;
                UpdateOriginalShiftPreview();
                OnPropertyChanged();
            }
        }

        private TimePickerModel _originalOriginalNightShiftEnd;
        public TimePickerModel OriginalNightShiftEnd
        {
            get { return _originalOriginalNightShiftEnd; }
            set
            {
                _originalOriginalNightShiftEnd = value;
                UpdateOriginalShiftPreview();
                OnPropertyChanged();
            }
        }
        #endregion

        #endregion
        #region Database Preview Shift 
        private string _originalDayShiftStartPreview;
        public string OriginalDayShiftStartPreview
        {
            get { return _originalDayShiftStartPreview; }
            set { _originalDayShiftStartPreview = value; OnPropertyChanged(); }
        }

        private string _originalDayShiftEndPreview;
        public string OriginalDayShiftEndPreview
        {
            get { return _originalDayShiftEndPreview; }
            set { _originalDayShiftEndPreview = value; OnPropertyChanged(); }
        }

        private string _originalNightShiftStartPreview;
        public string OriginalNightShiftStartPreview
        {
            get { return _originalNightShiftStartPreview; }
            set { _originalNightShiftStartPreview = value; OnPropertyChanged(); }
        }

        private string _originalNightShiftEndPreview;
        public string OriginalNightShiftEndPreview
        {
            get { return _originalNightShiftEndPreview; }
            set { _originalNightShiftEndPreview = value; OnPropertyChanged(); }
        }
        #endregion

        #region Form Preview Shift
        private string _dayShiftStartPreview;
        public string DayShiftStartPreview
        {
            get { return _dayShiftStartPreview; }
            set { _dayShiftStartPreview = value; OnPropertyChanged(); }
        }

        private string _dayShiftEndPreview;
        public string DayShiftEndPreview
        {
            get { return _dayShiftEndPreview; }
            set { _dayShiftEndPreview = value; OnPropertyChanged(); }
        }

        private string _nightShiftStartPreview;
        public string NightShiftStartPreview
        {
            get { return _nightShiftStartPreview; }
            set { _nightShiftStartPreview = value; OnPropertyChanged(); }
        }

        private string _nightShiftEndPreview;
        public string NightShiftEndPreview
        {
            get { return _nightShiftEndPreview; }
            set { _nightShiftEndPreview = value; OnPropertyChanged(); }
        }
        #endregion

        #region ComboBox Binds
        #region DayShift Bind
        private TimePickerModel _dayShiftStart;
        public TimePickerModel DayShiftStart
        {
            get { return _dayShiftStart; }
            set
            {
                if (_dayShiftStart != null)
                    _dayShiftStart.PropertyChanged -= OnShiftChanged;

                _dayShiftStart = value;

                if (_dayShiftStart != null)
                    _dayShiftStart.PropertyChanged += OnShiftChanged;
                OnPropertyChanged();
                UpdateAllShiftPreviews();
            }
        }

        private TimePickerModel _dayShiftEnd;
        public TimePickerModel DayShiftEnd
        {
            get { return _dayShiftEnd; }
            set
            {
                if (_dayShiftEnd != null)
                    _dayShiftEnd.PropertyChanged -= OnShiftChanged;
                _dayShiftEnd = value;

                if (_dayShiftEnd != null)
                    _dayShiftEnd.PropertyChanged += OnShiftChanged;
                OnPropertyChanged();
                UpdateAllShiftPreviews();
            }
        }
        #endregion
        #region NightShift Bind
        private TimePickerModel _nightShiftStart;
        public TimePickerModel NightShiftStart
        {
            get { return _nightShiftStart; }
            set
            {
                if (_nightShiftStart != null)
                    _nightShiftStart.PropertyChanged -= OnShiftChanged;
                _nightShiftStart = value;

                if (_nightShiftStart != null)
                    _nightShiftStart.PropertyChanged += OnShiftChanged;
                OnPropertyChanged();
                UpdateAllShiftPreviews();
            }
        }

        private TimePickerModel _nightShiftEnd;
        public TimePickerModel NightShiftEnd
        {
            get { return _nightShiftEnd; }
            set
            {
                if (_nightShiftEnd != null)
                    _nightShiftEnd.PropertyChanged -= OnShiftChanged;
                _nightShiftEnd = value;
                if (_nightShiftEnd != null)
                    _nightShiftEnd.PropertyChanged += OnShiftChanged;
                OnPropertyChanged();
                UpdateAllShiftPreviews();
            }
        }
        #endregion
        #endregion

        public AdminShiftScheduleVM(SqliteConnection connection)
        {
            _connection = connection;
            _scheduleServices = new ScheduleServices(_connection);
            CurrentDate = DateTime.Now.Date;
            Load();
        }

        #region Relay
        public RelayCommand UpdateCommand => new RelayCommand(Update, CanUpdate);
        public RelayCommand CancelCommand => new RelayCommand(Cancel, CanCancel);
        #endregion

        #region Command
        private async void Update(object parameter)
        {
            FileHelper.DisableWindow();
            ModalResultView loadingModal = new ModalResultView(Application.Current.MainWindow, "loading");
            try
            {
                Schedule newSchedule = new()
                {
                    DayShiftStart = $"{DayShiftStart.Hour:D2}:{DayShiftStart.Minute:D2}",
                    DayShiftEnd = $"{DayShiftEnd.Hour:D2}:{DayShiftEnd.Minute:D2}",
                    NightShiftStart = $"{NightShiftStart.Hour:D2}:{NightShiftStart.Minute:D2}",
                    NightShiftEnd = $"{NightShiftEnd.Hour:D2}:{NightShiftEnd.Minute:D2}",
                };
                loadingModal.Show();
                await Task.Delay(500);

                // set reference for cancel
                OriginalDayShiftStart = DefaultValue(newSchedule.DayShiftStart);
                OriginalDayShiftEnd = DefaultValue(newSchedule.DayShiftEnd);
                OriginalNightShiftStart = DefaultValue(newSchedule.NightShiftStart);
                OriginalNightShiftEnd = DefaultValue(newSchedule.NightShiftEnd);

                ServiceResult updateResult = await _scheduleServices.Update(newSchedule);
                if (!updateResult.Success)
                    throw new Exception(updateResult.ErrorMessage);
                loadingModal.Close();
                ModalResultView successModal = new(Application.Current.MainWindow, "success", "Schedule has been updated successfully");
                successModal.ShowDialog();
                FileHelper.EnableWindow();
            }
            catch (Exception ex)
            {
                loadingModal.Close();
                FileHelper.CatchSystemError(ex);
            }
        }

        private void Cancel(object parameter)
        {
            DayShiftStart.Minute = OriginalDayShiftStart.Minute;
            DayShiftStart.Hour = OriginalDayShiftStart.Hour;

            DayShiftEnd.Minute = OriginalDayShiftEnd.Minute;
            DayShiftEnd.Hour = OriginalDayShiftEnd.Hour;

            NightShiftStart.Minute = OriginalNightShiftStart.Minute;
            NightShiftStart.Hour = OriginalNightShiftStart.Hour;

            NightShiftEnd.Minute = OriginalNightShiftEnd.Minute;
            NightShiftEnd.Hour = OriginalNightShiftEnd.Hour;
        }
        #endregion

        #region CanCommand
        private bool CanUpdate(object parameter)
        {
            if (_dayStartDateTime == default ||
                _dayEndDateTime == default ||
                _nightStartDateTime == default ||
                _nightEndDateTime == default)
                return false;

            bool valid =
                _dayStartDateTime < _dayEndDateTime &&
                _dayEndDateTime <= _nightStartDateTime &&
                _nightStartDateTime < _nightEndDateTime &&
                _nightEndDateTime <= _dayStartDateTime.AddDays(1);

            bool allShiftsReasonable =
                (_dayEndDateTime - _dayStartDateTime).TotalHours <= 24 &&
                (_nightEndDateTime - _nightStartDateTime).TotalHours <= 24;

            return valid && allShiftsReasonable;
        }
        private bool CanCancel(object parameter)
        {
            return true;
        }
        #endregion
        #region Functions
        private async Task Load()
        {
            ModalResultView loadingModal = new ModalResultView(Application.Current.MainWindow, "loading");
            try
            {
                FileHelper.DisableWindow();
                loadingModal.Show();
                await Task.Delay(500);
                ServiceResult loadResult = await _scheduleServices.Load();
                if (!loadResult.Success)
                    throw new CustomException(loadResult.ErrorMessage, loadResult.ErrorType);
                if (loadResult.Data is Schedule schedule)
                {
                    // assign init values from db
                    DayShiftStart = DefaultValue(schedule.DayShiftStart.ToString());
                    DayShiftEnd = DefaultValue(schedule.DayShiftEnd.ToString());
                    NightShiftStart = DefaultValue(schedule.NightShiftStart.ToString());
                    NightShiftEnd = DefaultValue(schedule.NightShiftEnd.ToString());

                    // set reference for cancel
                    OriginalDayShiftStart = DefaultValue(schedule.DayShiftStart.ToString());
                    OriginalDayShiftEnd = DefaultValue(schedule.DayShiftEnd.ToString());
                    OriginalNightShiftStart = DefaultValue(schedule.NightShiftStart.ToString());
                    OriginalNightShiftEnd = DefaultValue(schedule.NightShiftEnd.ToString());

                }
                loadingModal.Close();
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
        private TimePickerModel DefaultValue(string time)
        {
            TimePickerModel chunks = new();
            string[] timeSplit = time.Split(":");
            if (timeSplit.Length == 2)
            {
                int hour = int.TryParse(timeSplit[0], out int hr) ? hr : 0;
                int minute = int.TryParse(timeSplit[1], out int mn) ? mn : 0;
                chunks.Hour = hour;
                chunks.Minute = minute;
            }
            return chunks;
        }
        private void OnShiftChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TimePickerModel.Hour) || e.PropertyName == nameof(TimePickerModel.Minute))
            {
                if (sender is TimePickerModel newTPM)
                    UpdateAllShiftPreviews();
            }
        }

        private void UpdateAllShiftPreviews()
        {
            if (DayShiftStart == null || DayShiftEnd == null || NightShiftStart == null || NightShiftEnd == null)
                return;

            _dayStartDateTime = CurrentDate.Date + new TimeSpan(DayShiftStart.Hour, DayShiftStart.Minute, 0);
            _dayEndDateTime = CurrentDate.Date + new TimeSpan(DayShiftEnd.Hour, DayShiftEnd.Minute, 0);
            _nightStartDateTime = CurrentDate.Date + new TimeSpan(NightShiftStart.Hour, NightShiftStart.Minute, 0);
            _nightEndDateTime = CurrentDate.Date + new TimeSpan(NightShiftEnd.Hour, NightShiftEnd.Minute, 0);


            if (_dayEndDateTime <= _dayStartDateTime)
                _dayEndDateTime = _dayEndDateTime.AddDays(1);

            if (_nightStartDateTime < _dayEndDateTime)
                _nightStartDateTime = _nightStartDateTime.AddDays(1);

            if (_nightEndDateTime <= _nightStartDateTime)
                _nightEndDateTime = _nightEndDateTime.AddDays(1);

            // UI ONLY (safe to format)
            DayShiftStartPreview = _dayStartDateTime.ToString("MM-dd-yyyy hh:mm tt", CultureInfo.InvariantCulture);
            DayShiftEndPreview = _dayEndDateTime.ToString("MM-dd-yyyy hh:mm tt", CultureInfo.InvariantCulture);
            NightShiftStartPreview = _nightStartDateTime.ToString("MM-dd-yyyy hh:mm tt", CultureInfo.InvariantCulture);
            NightShiftEndPreview = _nightEndDateTime.ToString("MM-dd-yyyy hh:mm tt", CultureInfo.InvariantCulture);
        }

        private void UpdateOriginalShiftPreview()
        {
            if (OriginalDayShiftStart == null || OriginalDayShiftEnd == null ||
                OriginalNightShiftStart == null || OriginalNightShiftEnd == null)
                return;

            DateTime dayStart = CurrentDate.Date + new TimeSpan(OriginalDayShiftStart.Hour, OriginalDayShiftStart.Minute, 0);
            DateTime dayEnd = CurrentDate.Date + new TimeSpan(OriginalDayShiftEnd.Hour, OriginalDayShiftEnd.Minute, 0);

            if (dayEnd <= dayStart)
                dayEnd = dayEnd.AddDays(1);

            DateTime nightStart = CurrentDate.Date + new TimeSpan(OriginalNightShiftStart.Hour, OriginalNightShiftStart.Minute, 0);
            if (nightStart < dayEnd)
                nightStart = nightStart.AddDays(1);

            DateTime nightEnd = CurrentDate.Date + new TimeSpan(OriginalNightShiftEnd.Hour, OriginalNightShiftEnd.Minute, 0);
            if (nightEnd <= nightStart)
                nightEnd = nightEnd.AddDays(1);

            OriginalDayShiftStartPreview = dayStart.ToString("MM-dd-yyyy hh:mm tt", CultureInfo.InvariantCulture);
            OriginalDayShiftEndPreview = dayEnd.ToString("MM-dd-yyyy hh:mm tt", CultureInfo.InvariantCulture);
            OriginalNightShiftStartPreview = nightStart.ToString("MM-dd-yyyy hh:mm tt", CultureInfo.InvariantCulture);
            OriginalNightShiftEndPreview = nightEnd.ToString("MM-dd-yyyy hh:mm tt", CultureInfo.InvariantCulture);
        }
        #endregion
    }
}
