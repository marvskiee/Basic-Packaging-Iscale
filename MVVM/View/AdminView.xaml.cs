using System.Windows;
using System.Windows.Input;

namespace ISCALE.MVVM.View
{
    public partial class AdminView : Window
    {
        public AdminView()
        {
            InitializeComponent();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }
    }
}
