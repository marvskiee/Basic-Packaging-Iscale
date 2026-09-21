using Formatter.Helper;
using MVVMCORE;
using System.Collections.ObjectModel;
namespace ISCALE.MVVM.Model
{
    public class Transaction : ViewModelBase
    {
		private string _jobId;

		public string JobId
		{
			get { return _jobId; }
			set { _jobId = value; OnPropertyChanged(); }
		}

		private string _transactionId;

		public string TransactionId
		{
			get { return _transactionId; }
			set { _transactionId = value; OnPropertyChanged(); }
		}

		private ProductItem _item;
        public ProductItem Item
        {
            get { return _item; }
            set { _item = value; OnPropertyChanged(); }
        }

        private string? _weigher;
		public string? Weigher
		{
			get { return _weigher; }
			set { _weigher = value; OnPropertyChanged(); }
		}

		private string? _scaleNo;
		public string? ScaleNo
		{
			get { return _scaleNo; }
			set { _scaleNo = value; OnPropertyChanged(); }
		}

		private string _section;
		public string Section
		{
			get { return _section; }
			set { _section = value; OnPropertyChanged(); }
		}

		private string _machineNo;
		public string MachineNo
		{
			get { return _machineNo; }
			set { _machineNo = value; OnPropertyChanged(); }
		}

		private string _qaNo;

		public string QaNo
		{
			get { return _qaNo; }
			set { _qaNo = value; OnPropertyChanged(); }
		}


		private string _operatorNo;

		public string OperatorNo
		{
			get { return _operatorNo; }
			set { _operatorNo = value; OnPropertyChanged(); }
		}

		private string _packerNo;
		public string PackerNo
		{
			get { return _packerNo; }
			set { _packerNo = value; OnPropertyChanged(); }
		}

		private User _supervisor;
		public User Supervisor
		{
			get { return _supervisor; }
			set { _supervisor = value; OnPropertyChanged(); }
		}

		public ObservableCollection<TransactionItem> TransactionItems { get; set; } = new ObservableCollection<TransactionItem>();

		private decimal _totalSuccessWeight;
		public decimal TotalSuccessWeight
        {
			get { return _totalSuccessWeight; }
			set { _totalSuccessWeight = value; OnPropertyChanged(); }
		}

		private decimal _totalFailedWeight;
		public decimal TotalFailedWeight
        {
			get { return _totalFailedWeight; }
			set { _totalFailedWeight = value; OnPropertyChanged(); }
		}

		private decimal _averageWeight;
		public decimal AverageWeight
		{
			get { return _averageWeight; }
			set { _averageWeight = value; OnPropertyChanged(); }
		}

		private int _failedCount;
		public int FailedCount
		{
			get { return _failedCount; }
			set { _failedCount = value; OnPropertyChanged(); }
		}

        private int _successCount;
		public int SuccessCount
		{
			get { return _successCount; }
			set { _successCount = value; OnPropertyChanged(); }
		}

		private int _deletedCount;
		public int DeletedCount
		{
			get { return _deletedCount; }
			set { _deletedCount = value; OnPropertyChanged(); }
		}

		private int _totalCount;
		public int TotalCount
		{
			get { return _totalCount; }
			set { _totalCount = value; OnPropertyChanged(); }
		}

		private string _scheduleShift;
		public string ScheduleShift
        {
			get { return _scheduleShift; }
			set { _scheduleShift = value; OnPropertyChanged(); }
		}

		private string _deleteNote;

		public string DeleteNote
		{
			get { return _deleteNote; }
			set { _deleteNote = value; OnPropertyChanged(); }
		}

		private DateTime _createdAt;
		public DateTime CreatedAt
		{
			get { return _createdAt; }
			set { _createdAt = value; OnPropertyChanged(); }
		}

		private DateTime _finishedAt;
		public DateTime FinishedAt
		{
			get { return _finishedAt; }
			set { _finishedAt = value; OnPropertyChanged(); }
		}

		public string Code { get; set; }

		private bool _isSelected;
		public bool IsSelected
		{
			get { return _isSelected; }
			set { _isSelected = value; OnPropertyChanged(); }
		}
        public string DateCreatedString => DateTimeHelper.DateUtcToLocal(CreatedAt);
		public string DateEndString => DateTimeHelper.DateUtcToLocal(FinishedAt);

    }
}
