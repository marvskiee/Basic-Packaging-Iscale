using ISCALE.Utilities;
using System.Collections.ObjectModel;
using System.Windows;
using System.Xml;
using MVVMCORE;
using System.IO.Ports;
using ISCALE.Core.Enum;

namespace ISCALE.MVVM.ViewModel.OperatorVMS.ModalVM
{
    public class OperatorSettingModalVM : ViewModelBase
    {

        private string scaleComportRef;
        #region Scale Settings
        private Style _settingResultStyle;
        public Style SettingResultStyle
        {
            get { return _settingResultStyle; }
            set { _settingResultStyle = value; OnPropertyChanged(); }
        }

        private string _settingResult;
        public string SettingResult
        {
            get { return _settingResult; }
            set { _settingResult = value; OnPropertyChanged(); }
        }

        public ObservableCollection<string> ComPortList { get; private set; }
        public Action<bool?> CloseAction { get; set; }
        public bool? DialogResult { get; private set; }

        private string _scaleComport;
        public string ScaleComport
        {
            get { return _scaleComport; }
            set
            {
                _scaleComport = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Init
        public OperatorSettingModalVM(string scaleComport)
        {
            scaleComportRef = scaleComport;
            LoadSetting();
            DialogResult = false;
        }
        #endregion

        #region Relay
        public RelayCommand SaveCommand => new RelayCommand(Save);
        public RelayCommand CancelCommand => new RelayCommand(Cancel);
        public RelayCommand CloseCommand => new RelayCommand(Close);
        #endregion

        #region Command
        private void Save(object parameter)
        {
            try
            {
                using (XmlTextWriter writer = new XmlTextWriter(FileHelper.getCurrentDirectory(RoleEnum.SUPERVISOR) + Constants.SUPERVISOR_SETTING_FILE, System.Text.Encoding.UTF8))
                {
                    writer.Formatting = Formatting.Indented;
                    writer.Indentation = 2;
                    writer.WriteStartElement("default_settings");
                    writer.WriteElementString("com_port", ScaleComport);
                    writer.WriteEndElement();
                    writer.Flush();
                }
                SettingResult = "Changes has been applied.";
                SettingResultStyle = (Style)Application.Current.FindResource("SuccessAlert_patched");
                DialogResult = true;
                scaleComportRef = ScaleComport;
            }
            catch (Exception ex)
            {
               FileHelper.LogHandler($"Operator Settings Save {ex.Message}");
                SettingResult = "Something went wrong.";
                SettingResultStyle = (Style)Application.Current.FindResource("ErrorAlert_patched");
            }
        }
        private void Cancel(object parameter)
        {
            ScaleComport = ComPortList?.FirstOrDefault(x => x.Equals(scaleComportRef));
        }
        private void Close(object parameter)
        {
            CloseAction?.Invoke(DialogResult);
        }
        #endregion

        #region Functions
        private void LoadSetting()
        {
            try
            {
                ComPortList?.Clear();

                SettingResult = "Please select Com Port";
                SettingResultStyle = (Style)Application.Current.FindResource("NormalAlert_patched");

                // Populate ComboBox 
                ComPortList = new ObservableCollection<string>(SerialPort.GetPortNames());
                ScaleComport = ComPortList?.FirstOrDefault(x => x.Equals(scaleComportRef));
            }
            catch (Exception ex)
            {
                FileHelper.LogHandler($"Operator Setting Load{ex.Message}");
                SettingResult = "Something went wrong.";
                SettingResultStyle = (Style)Application.Current.FindResource("ErrorAlert_patched");
            }
        }
        #endregion
    }
}
