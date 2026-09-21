using ISCALE.MVVM.View.ModalViews;
using ISCALE.MVVM.ViewModel.ModalVMS;
using ISCALE.Utilities;
using System.Windows;
using System.Windows.Controls;

namespace ISCALE.MVVM.View.ControlViews
{
    public partial class ControlWindowHeaderView : UserControl
    {
        public ControlWindowHeaderView()
        {
            InitializeComponent();
            TB_VERSION.Text = Constants.APP_VERSION;
        }

        private void BTN_CLOSE_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            string sessionValue = this.Tag?.ToString();

            if (sessionValue != "false")
            {
                try
                {
                    FileHelper.DisableWindow();

                    ModalConfirmationVM dialogVM = new("Exit", "Are you sure you want to exit?");
                    ModalConfirmationView dialog = new ModalConfirmationView
                    {
                        DataContext = dialogVM,
                        Owner = Application.Current.MainWindow
                    };
                    dialogVM.CloseAction = (result) => dialog.DialogResult = result;
                    bool? result = dialog.ShowDialog();
                    if (result == true)
                        this.CloseWindow();
                    else FileHelper.EnableWindow();
                }
                catch (Exception ex)
                {

                }
            }
            else
            {
                CloseWindow();
            }
        }

        private void CloseWindow()
        {
            Window window = Window.GetWindow(this);
            if (window == Application.Current.MainWindow)
                Application.Current.Shutdown();
            else
                window.Close();
        }

        private void BTN_RESIZE_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Window window = Window.GetWindow(this);
            if (window != null)
            {
                if (window.WindowState == WindowState.Maximized)
                    window.WindowState = WindowState.Normal;
                else
                    window.WindowState = WindowState.Maximized;
            }
        }

        private void BTN_MINIMIZE_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Window window = Window.GetWindow(this);
            if (window != null)
            {
                window.WindowState = WindowState.Minimized;
            }
        }

        public static readonly DependencyProperty HasNoResizeProperty =
        DependencyProperty.Register(nameof(HasNoResize), typeof(bool),
            typeof(ControlWindowHeaderView),
            new PropertyMetadata(false, OnHasNoResize));

        public bool HasNoResize
        {
            get => (bool)GetValue(HasNoResizeProperty);
            set => SetValue(HasNoResizeProperty, value);
        }

        private static void OnHasNoResize(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (ControlWindowHeaderView)d;
            bool status = (bool)e.NewValue;

            control.BTN_RESIZE.Visibility = !status ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
