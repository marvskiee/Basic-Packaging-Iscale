using ISCALE.Utilities;
using System.Windows;
using System.Windows.Input;

namespace ISCALE.MVVM.View.ModalViews
{
    public partial class ModalResultView : Window
    {
        public string HeaderTitle { get; set; }
        public string Body { get; set; }
        public string ImagePath { get; set; }
        public ModalResultView(Window parentWindow, string status, string body = "")
        {
            Owner = parentWindow;
            DataContext = this;
            InitializeComponent();

            Body = body;

            // Set ImagePath based on Status
            if (status == "loading")
            {
                if (string.IsNullOrEmpty(body))
                    Body = "Please wait while we process.";
                HeaderTitle = "Loading..";
                ImagePath = Constants.LOADING_IMAGE;
                BtnClose.Visibility = Visibility.Collapsed;
            }
            else if (status == "error")
            {
                HeaderTitle = "Error";
                ImagePath = Constants.ERROR_IMAGE;
            }
            else
            {
                HeaderTitle = "Success";
                ImagePath = Constants.SUCCESS_IMAGE;
            }
        }

        private void BTN_CLOSE_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                Close();
        }
    }
}
