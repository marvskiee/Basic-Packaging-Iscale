using MVVMCORE;

namespace ISCALE.Core.Base
{
    public class BTableManagerModel : ViewModelBase
    {
		public string Id { get; set; }

		private bool _isDeleted;
		public bool IsDeleted
		{
			get { return _isDeleted; }
			set { _isDeleted = value; OnPropertyChanged(); }
		}

		public DateTime CreatedAt { get; set; }
	}
}
