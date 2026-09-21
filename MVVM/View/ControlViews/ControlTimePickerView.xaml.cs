using ISCALE.MVVM.Model;
using System.Windows;
using System.Windows.Controls;

namespace ISCALE.MVVM.View.ControlViews
{
    public partial class ControlTimePickerView : UserControl
    {
        public ControlTimePickerView()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty SelectedTimeProperty =
            DependencyProperty.Register(
                "SelectedTime", 
                typeof(TimePickerModel), 
                typeof(ControlTimePickerView), 
                new PropertyMetadata(null));

        public TimePickerModel SelectedTime
        {
            get => (TimePickerModel)GetValue(SelectedTimeProperty);
            set =>  SetValue(SelectedTimeProperty, value); 
        }
    }
}
