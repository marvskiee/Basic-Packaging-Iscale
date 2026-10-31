using Formatter.Helper;
using ISCALE.MVVM.Model;
using System.Collections.ObjectModel;
using System.IO.Ports;

namespace ISCALE.Utilities
{
    public class Constants
    {
        
        public static readonly bool DEMO = false;
        public static readonly string APP_VERSION = "1.3.1";
        
        // APPDATA DIRECTORIES
        public static readonly string[] ADMIN_APPDATA = { "\\Logs", "\\Reports"};
        public static readonly string[] SUPERVISOR_APPDATA = { "\\Logs", "\\Config"};

        // ENCRYPT HASH
        public static readonly string TABLE_HASH = Env.TABLE_HASH;
        
        // IMAGE
        public const string SUCCESS_IMAGE = "/Images/Icons/check.png";
        public const string ERROR_IMAGE = "/Images/Icons/x.png";
        public const string LOADING_IMAGE = "/Images/Icons/timer.png";

        public const string ADMIN_AVATAR = "/Images/Avatars/admin.png";
        public const string OPERATOR_AVATAR = "/Images/Avatars/operator.png";

        public static readonly string APP_DATA = $"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}\\AppData";
        
        // FILE
        public static readonly string SUPERVISOR_SETTING_FILE= "\\Config\\settings.xml";
        public static readonly string LOG_FILE = "\\Logs\\index.txt";

        public static readonly string[] ROLES = { "Supervisor" };
        public static readonly ObservableCollection<string> ROLE_COLLECTION = new ObservableCollection<string>(ROLES);

        public static readonly string[] BATCH_ITEM_HEADER = { "Code #", "Internal Reference","Display Name","Lower Limit" ,"Upper Limit"};
        public static readonly string[] TRANSACTION_HEADER = { 
            "Time Start","Time End", "Shift", "Section", "Packer No", "Machine No","Qa No", "Operator No", "Supervisor", "Weigher","Weighing Scale #", "TransactionID",
            "Item Code #", "Item Internal Reference", "Item Display Name", "Lower Limit", "Upper Limit", "Average Weight",
            "Total Success Weight", "Total Success Count" ,"Total Failed Weight", "Total Failed Count", "Deleted Count", "Total Overall Count", "Delete Notes" };

        // HASHES
        public static readonly string PASSWORD_HASH = Env.PASSWORD_HASH;

        public static readonly string[] COM_PORTS = SerialPort.GetPortNames();
        private static readonly string[] SECTIONS = { "Trash Bag", "Sheeted", "Rollo" };

        public static readonly ObservableCollection<string> SECTION_COLLECTION = new ObservableCollection<string>(SECTIONS);
        public static readonly ObservableCollection<string> COMPORT_COLLECTION = new ObservableCollection<string>(COM_PORTS);

        // ERROR KEYWORD
        public static readonly string WRONG_PASSWORD = "WRONG_PASSWORD";
        public static readonly string NO_DATA_ERROR_TYPE = "NO_DATA_ERROR_TYPE";
        public static readonly string CAST_DATA_ERROR_TYPE = "CAST_DATA_ERROR_TYPE";
        public static readonly string CONNECTION_ERROR_TYPE = "CONNECTION_ERROR_TYPE";
        public static readonly string USER_NOT_EXISTED = "USER_NOT_EXISTED";
        public static readonly string DUPLICATE_ERROR_TYPE = "DUPLICATE_ERROR_TYPE";
        public static readonly string INVALID_SCHEDULE = "INVALID_SCHEDULE";

        // ERROR MESSAGE
        public static readonly string CAST_DATA_ERROR_MESSAGE = "Invalid cast data!";
        public static readonly string INVALID_SCHEDULE_MESSAGE = "Invalid schedule! Please contact supervisor";

        public static readonly User ADMIN_CREDENTIALS = new User { Id = Guid.NewGuid().ToString(), Username = "Admin", Password = CryptographyHelper.Encrypt("Admin!", Constants.PASSWORD_HASH), Role = "Admin" };

        // TIME PICKER ITEMS
        public static readonly int[] DAYSHIFT_RANGE = Enumerable.Range(0, 24).ToArray();
        private static readonly int[] EXTRASHIFT = Enumerable.Range(0, 7).ToArray();
        public static readonly int[] NIGHTSHIFT_RANGE = (Enumerable.Range(18, 6).ToArray()).Concat(EXTRASHIFT).ToArray();
        public static readonly int[] MINUTE_RANGE = Enumerable.Range(0, 60).ToArray();

    }
}
