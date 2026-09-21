using ISCALE.MVVM.ViewModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ISCALE.MVVM.View
{
    public partial class OperatorView : Window
    {
        public OperatorView()
        {
            InitializeComponent();
            TBX_OPERATORNO.Focus();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }
    }
}
