namespace ISCALE.MVVM.Model.ServiceModel
{
    public class Schedule
    {
		private string _dayShiftStart;
		public string DayShiftStart
        {
			get { return _dayShiftStart; }
			set { _dayShiftStart = value; }
		}

        private string _dayShiftEnd;
        public string DayShiftEnd
        {
            get { return _dayShiftEnd; }
            set { _dayShiftEnd = value; }
        }

        private string _nightShiftStart;
        public string NightShiftStart
        {
            get { return _nightShiftStart; }
            set { _nightShiftStart = value; }
        }

        private string _nightShiftEnd;
        public string NightShiftEnd
        {
            get { return _nightShiftEnd; }
            set { _nightShiftEnd = value; }
        }
    }
}
