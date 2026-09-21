using ISCALE.Core.Enum;
using ISCALE.MVVM.View.ModalViews;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Xml;

namespace ISCALE.Utilities
{
    public class FileHelper
    {
        public static string getCurrentDirectory(RoleEnum role)
        {
            string file_path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string final_path = file_path + "\\AppData";
            // create folders directory
            if (!Directory.Exists(final_path)) Directory.CreateDirectory(final_path);
            string[] folders = { };
            switch (role)
            {
                case RoleEnum.SUPERVISOR:
                    folders = Constants.SUPERVISOR_APPDATA;
                    break;
                case RoleEnum.ADMIN:
                    folders = Constants.ADMIN_APPDATA;
                    break;
            }

            foreach (string folder in folders)
            {
                if (!Directory.Exists(final_path + folder)) Directory.CreateDirectory(final_path + folder);
            }
            return final_path;
        }


        public static void LogHandler(string content)
        {
            string path = getCurrentDirectory(RoleEnum.ADMIN) + "\\Logs\\index.txt";
            if (!File.Exists(path))
            {
                using (StreamWriter sw = File.CreateText(path)) ;
            }
            string[] lines = File.ReadAllLines(path);
            List<string> error_content = new List<string>();
            error_content = lines.ToList();
            error_content.Add($"[{DateTime.Now.ToString()}]: {content}");
            File.WriteAllLines(path, error_content);
        }

        public static void initSetting()
        {
            using (XmlTextWriter writer = new XmlTextWriter(getCurrentDirectory(RoleEnum.SUPERVISOR) + Constants.SUPERVISOR_SETTING_FILE, System.Text.Encoding.UTF8))
            {
                string default_port_name = "";
                if (Constants.COM_PORTS.Length > 0) default_port_name = Constants.COM_PORTS[0];

                writer.Formatting = Formatting.Indented;
                writer.Indentation = 2;
                writer.WriteStartElement("default_settings");
                writer.WriteElementString("com_port", default_port_name);
                writer.WriteEndElement();
                writer.Flush();
            }
        }

        public static void DisableWindow()
        {
            Application.Current.MainWindow.Opacity = 0.4;
            Application.Current.MainWindow.IsEnabled = false;
        }

        public static void EnableWindow()
        {
            Application.Current.MainWindow.Opacity = 1;
            Application.Current.MainWindow.IsEnabled = true;
            Application.Current.MainWindow.Activate();
        }

        public static void CatchError(CustomException ex, [CallerFilePath] string filePath = "", [CallerMemberName] string methodName = "", [CallerLineNumber] int lineNumber = 0)
        {
            string errorMessage = "";
            string logMessage = $"{Path.GetFileName(filePath)} => {methodName} at ({lineNumber}) : {ex.Message}";
            LogHandler(logMessage);
            // ui error types
            switch (ex.ErrorType)
            {
                case "USER_NOT_EXISTED":
                    errorMessage = "Wrong Credentials!";
                    break;
                case "INVALID_SCHEDULE":
                    errorMessage = "Invalid schedule! Please contact supervisor.";
                    break;
                case "NO_DATA_ERROR_TYPE":
                    errorMessage = "No data found!";
                    break;
                case "DUPLICATE_ERROR_TYPE":
                    errorMessage = "Data already existed!";
                    break;
                case "RESTRICTED_ERROR_TYPE":
                case "CAST_DATA_ERROR_TYPE":
                    errorMessage = "Something went wrong!";
                    break;
                case "INVALID_FILE_ERROR_TYPE":
                    errorMessage = "Invalid File!";
                    break;
                default:
                    errorMessage = "Something went wrong!";
                    break;
            }
            ModalResultView errorModal = new(Application.Current.MainWindow, "error", errorMessage);
            errorModal.ShowDialog();
            EnableWindow();
        }

        public static void CatchSystemError(Exception ex, [CallerFilePath] string filePath = "", [CallerMemberName] string methodName = "", [CallerLineNumber] int lineNumber = 0)
        {
            string errorMessage = "Something went wrong!";
            string logMessage = $"{Path.GetFileName(filePath)} => {methodName} at ({lineNumber}) : {ex.Message}";
            LogHandler(logMessage);

            if (ex.Message.ToLower().Contains("device") || ex.Message.ToLower().Contains("could not"))
                errorMessage = ex.Message;
            ModalResultView errorModal = new(Application.Current.MainWindow, "error", errorMessage);
            errorModal.ShowDialog();
            EnableWindow();
        }

        public static void CatchModalError(CustomException ex, [CallerFilePath] string filePath = "", [CallerMemberName] string methodName = "", [CallerLineNumber] int lineNumber = 0)
        {
            string logMessage = $"{Path.GetFileName(filePath)} => {methodName} at ({lineNumber}) : {ex.Message}";
            // do switch here
            LogHandler(logMessage);
        }

        public static void CatchSystemModalError(Exception ex, [CallerFilePath] string filePath = "", [CallerMemberName] string methodName = "", [CallerLineNumber] int lineNumber = 0)
        {
            string logMessage = $"{Path.GetFileName(filePath)} => {methodName} at ({lineNumber}) : {ex.Message}";
            LogHandler(logMessage);
        }

        public static void CreateFile(string path)
        {
            if (!File.Exists(path))
                File.Create(path).Dispose();
        }
    }
}
