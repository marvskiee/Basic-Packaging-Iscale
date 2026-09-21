using ISCALE.Core.Base;

namespace ISCALE.MVVM.Model
{
    public class User : BTableManagerModel
    {
		private string _username;
		public string Username
		{
			get { return _username; }
			set { 
				if(_username != value)
				{
					_username = value?.Trim();
					OnPropertyChanged(); }
				}
		}

		private string _password;

		public string Password
		{
			get { return _password; }
			set {
                if (_password != value)
                {
                    _password = value?.Trim();
                    OnPropertyChanged();
                }
            }
        }

        public DateTime DeletedAt { get; set; }


        private string _role;
		public string Role
		{
			get { return _role; }
			set { _role = value; OnPropertyChanged(); }
		}
	}
}
