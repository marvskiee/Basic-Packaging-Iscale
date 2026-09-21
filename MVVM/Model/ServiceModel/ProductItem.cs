using ISCALE.Core.Base;

namespace ISCALE.MVVM.Model
{
    public class ProductItem : BTableManagerModel
    {
		private string _code;
		public string Code
		{
			get { return _code; }
			set { _code = value; OnPropertyChanged(); }
		}

		private string _internalReference;

		public string InternalReference
		{
			get { return _internalReference; }
			set { _internalReference = value; OnPropertyChanged(); }
		}


		private string _description;

		public string Description
		{
			get { return _description; }
			set { _description = value; OnPropertyChanged(); }
		}


		private decimal _upperLimit;
		public decimal UpperLimit
		{
			get { return _upperLimit; }
			set { _upperLimit = value; OnPropertyChanged(); }
		}

		private decimal _lowerLimit;
		public decimal LowerLimit
		{
			get { return _lowerLimit; }
			set { _lowerLimit = value; OnPropertyChanged(); }
		}
	}
}
