using ISCALE.Utilities;
using MVVMCORE;
using System.Collections.ObjectModel;
using System.Windows;

namespace ISCALE.MVVM.Model
{
    public class TimePickerModel : ViewModelBase
    {
        private int _hour;
        public int Hour
        {
            get { return _hour; }
            set {
                if (_hour != value)
                {
                    _hour = value;
                    OnPropertyChanged(); 
                }
            }
        }

        private int _minute;
        public int Minute
        {
            get { return _minute; }
            set { 
                if (_minute != value)
                {
                    _minute = value;
                    OnPropertyChanged(); 
                }
            }
        }

        public ObservableCollection<int> HoursCollection { get; } = new ObservableCollection<int>(Constants.DAYSHIFT_RANGE);
        public ObservableCollection<int> MinutesCollection { get; } = new ObservableCollection<int>(Constants.MINUTE_RANGE);

    }
}
