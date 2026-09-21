using Formatter.Helper;
using MVVMCORE;
using System.Collections.ObjectModel;

namespace ISCALE.MVVM.Model
{
    public class ProductMetadata : ViewModelBase
    {
		private int _id;
		public int Id
		{
			get { return _id; }
			set { _id = value; }
		}

		private DateTime? _effectiveDate;
		public DateTime? EffectiveDate
		{
			get { return _effectiveDate; }
			set { _effectiveDate = value; OnPropertyChanged(); }
		}

		private string _documentReferenceNo;
		public string DocumentReferenceNo
		{
			get { return _documentReferenceNo; }
			set { _documentReferenceNo = value; OnPropertyChanged(); }
		}

		private string _revisionNo;
		public string RevisionNo
		{
			get { return _revisionNo; }
			set { _revisionNo = value; OnPropertyChanged(); }
		}

        public ObservableCollection<ProductItem> ProductItem { get; set; } = new ObservableCollection<ProductItem>();

        private DateTime _createdAt;
		public DateTime CreatedAt
		{
			get { return _createdAt; }
			set { _createdAt = value; }
		}

		public string CreatedAtString => Id > 0 ? DateTimeHelper.DateUtcToLocal(CreatedAt) : string.Empty;
    }
}
