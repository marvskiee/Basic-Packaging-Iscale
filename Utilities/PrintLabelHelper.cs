using ISCALE.MVVM.Model;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows;
using System.Windows.Media;


namespace ISCALE.Utilities
{
    public class PrintLabelHelper
    {
        private FlowDocument GeneratePrintLabel(Transaction SelectedTransaction, PrintDialog printDialog)
        {
            // Create a FlowDocument
            FlowDocument flowDoc = new FlowDocument();
            FontFamily customFont = new FontFamily("pack://application:,,,/JIMS;Font/Poppins-Regular.ttf#Poppins");

            flowDoc.PageWidth = 400;
            flowDoc.PageHeight = 400;

            flowDoc.FontFamily = customFont;
            flowDoc.FontSize = 13;
            flowDoc.ColumnWidth = printDialog.PrintableAreaWidth; // Set to fit printable area width


            // Content
            Grid grid = new Grid();
            grid.Width = flowDoc.PageWidth - 38;
            grid.HorizontalAlignment = HorizontalAlignment.Stretch;
            grid.Height = flowDoc.PageHeight - 38;

            grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

            Border productPlanBox = TableBodyBox($"Item Code: {SelectedTransaction.Item.Code}", new Thickness(1));
            Grid.SetRow(productPlanBox, 0);
            grid.Children.Add(productPlanBox);

            Border dateCreatedBox = TableBodyBox($"Weighted Date:\n{SelectedTransaction.CreatedAt.ToString("MMM dd, yyyy h:mm:ss tt")}", new Thickness(1, 0, 1, 1));
            Grid.SetRow(dateCreatedBox, 1);
            grid.Children.Add(dateCreatedBox);

            Border weightBox = TableBodyBox($"Weight: {SelectedTransaction.TransactionItems[0]?.Weight.ToString()} KG" ?? "", new Thickness(1, 0, 1, 1));
            Grid.SetRow(weightBox, 2);
            grid.Children.Add(weightBox);

            BlockUIContainer block = new BlockUIContainer(grid);

            flowDoc.Blocks.Add(block);
            return flowDoc;
        }
        public static Border TableBodyBox(string title, Thickness borderThickness, string alignment = "", int fontSize = 25)
        {
            Border newBoxText = new Border()
            {
                BorderBrush = Brushes.Black,
                BorderThickness = borderThickness,
                Padding = new Thickness(5),
                Child = NormalTextBlock(title, alignment, fontSize),
            };
            return newBoxText;
        }
        public static TextBlock NormalTextBlock(string title, string alignment = "", int fontSize = 25)
        {
            TextBlock textBlock = new TextBlock(new Run(title))
            {
                FontSize = fontSize,
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = alignment == "R" ? HorizontalAlignment.Right : alignment == "C" ? HorizontalAlignment.Center : HorizontalAlignment.Left
            };
            return textBlock;
        }
        public void Print(Transaction SelectedTransaction)
        {
            /*PrintDialog printDialog = new PrintDialog();
            FlowDocument flowDoc = GeneratePrintLabel(SelectedTransaction, printDialog);
            if (printDialog.ShowDialog() == true)
            {
                IDocumentPaginatorSource idpSource = flowDoc;
                printDialog.PrintDocument(idpSource.DocumentPaginator, $"{SelectedTransaction.JobId} {SelectedTransaction.DateCreated.ToString("Mddyyyyhhmmss")}");
            }*/
        }
    }
}
