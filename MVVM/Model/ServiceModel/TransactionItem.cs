using MVVMCORE;
using System.Windows;
using System.Windows.Media;
namespace ISCALE.MVVM.Model
{
    public class TransactionItem : ViewModelBase
    {
		private int _id;
		public int Id
		{
			get { return _id; }
			set { _id = value; OnPropertyChanged(); }
		}
		public string TempId;
		private decimal _weight;
		public decimal Weight
		{
			get { return _weight; }
			set { _weight = value; OnPropertyChanged(); }
		}

		private bool _isDeleted;

		public bool IsDeleted
		{
			get { return _isDeleted; }
			set { _isDeleted = value; OnPropertyChanged(); }
		}

		private string _status;
		public string Status
		{
			get { return _status; }
			set { _status = value; OnPropertyChanged(); }
		}

        public DateTime DateCreated = DateTime.Now.ToLocalTime();

        public string DateCreatedString => DateCreated.ToString("hh:mm:ss:fff tt");

        private Brush _statusForeground;
		public Brush StatusForeground
        {
			get { return _statusForeground; }
			set { _statusForeground = value; OnPropertyChanged(); }
		}
	}
}
