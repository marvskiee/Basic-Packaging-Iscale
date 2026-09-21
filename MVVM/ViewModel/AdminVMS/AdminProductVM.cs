using ClosedXML.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using Formatter.Helper;
using ISCALE.Core.Enum;
using ISCALE.Core.Interface;
using ISCALE.MVVM.Model;
using ISCALE.MVVM.View.ModalViews;
using ISCALE.MVVM.ViewModel.ModalVMS;
using ISCALE.Utilities;
using ISCALE.Utilities.Services;
using Microsoft.Data.Sqlite;
using MVVMCORE;
using System.Windows;

namespace ISCALE.MVVM.ViewModel.AdminVMS
{
    public class AdminProductVM : ViewModelBase
    {
        private ProductServices _productServices;
        private ProductMetadata _currentProductMetadata;

        public ProductMetadata CurrentProductMetadata
        {
            get { return _currentProductMetadata; }
            set { _currentProductMetadata = value; OnPropertyChanged(); }
        }

        private readonly SqliteConnection _connection;
        #region init

        private string _upperLimit;
        public string UpperLimit
        {
            get { return _upperLimit; }
            set {
                _upperLimit = value;
                OnPropertyChanged();
                if (decimal.TryParse(_upperLimit, out decimal newUpperLimit))
                    _upperLimit = newUpperLimit.ToString();
                else
                    _upperLimit = "0";
            }
        }

        private string _lowerLimit;
        public string LowerLimit
        {
            get { return _lowerLimit; }
            set
            {
                _lowerLimit = value;
                OnPropertyChanged();
                if (decimal.TryParse(_lowerLimit, out decimal newUpperLimit))
                    _lowerLimit = newUpperLimit.ToString();
                else
                    _lowerLimit = "0";
            }
        }



        #endregion
        public AdminProductVM(SqliteConnection connection){
            _connection = connection;
            _productServices = new ProductServices(connection);
            CurrentProductMetadata = new ProductMetadata();
            LoadTable();
        }

        #region Relay
        public RelayCommand DeleteCommand => new RelayCommand(Delete);
        public RelayCommand UploadCommand => new RelayCommand(Upload);
        public RelayCommand ExportCommand => new RelayCommand(Export);

        #endregion

        #region Override Command
        private void Delete(object parameter)
        {
            if (parameter != null)
            {
                FileHelper.DisableWindow();

                string item_name;

                var _item = (ProductItem)parameter;

                ProcessDeleteItem(_item);
            }
        }

        private async void Upload(object parameter)
        {
            FileHelper.DisableWindow();

            ModalUploadBulkItemVM dialogVM = new ModalUploadBulkItemVM(new ProductServices(_connection));
            ModalUploadBulkItemView dialogView = new ModalUploadBulkItemView()
            {
                Owner = Application.Current.MainWindow,
                DataContext = dialogVM
            };

            dialogVM.CloseAction = (result) => dialogView.DialogResult = result;
            bool? result2 = dialogView.ShowDialog();
            if (result2 == true)
                await LoadTable();
            else 
                FileHelper.EnableWindow();
        }

        private void Export(object parameter)
        {
            FileHelper.DisableWindow();
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    DateTime timestamp = DateTime.UtcNow;

                    string fileName = $"Product({CurrentProductMetadata.DocumentReferenceNo} {CurrentProductMetadata.RevisionNo}) {DateTimeHelper.DateFileName(timestamp)}";

                    string filePath = FileHelper.getCurrentDirectory(RoleEnum.ADMIN) + $"\\Exported Products\\{fileName}.xlsx";

                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("Products");
                        // Header 
                        for (int h = 1; h <= Constants.BATCH_ITEM_HEADER.Count(); h++)
                        {
                            worksheet.Cell(1, h).Value = Constants.BATCH_ITEM_HEADER[h - 1];
                        }
                        int currentRow = 2;
                        // Body
                        for (int t = 0; t < CurrentProductMetadata.ProductItem.Count(); t++)
                        {
                            ProductItem PI = CurrentProductMetadata.ProductItem[t];

                            worksheet.Cell(currentRow, 1).Value = PI.Code;
                            worksheet.Cell(currentRow, 2).Value = PI.InternalReference;
                            worksheet.Cell(currentRow, 3).Value = PI.Description;
                            worksheet.Cell(currentRow, 4).Value = PI.LowerLimit;
                            worksheet.Cell(currentRow, 5).Value = PI.UpperLimit;

                            currentRow++;
                        }

                        workbook.SaveAs(filePath);
                    }
                });

                // Success State
                ModalResultView successModal = new(Application.Current.MainWindow, "success", $"File is saved at Desktop/AppData/Exported Products folder");
                successModal.ShowDialog();
                FileHelper.EnableWindow();
            }
            catch (Exception ex)
            {
                FileHelper.CatchSystemError(ex);
            }
        }
        #endregion

        #region Functions

        private async Task LoadTable()
        {
            FileHelper.DisableWindow();
            ModalResultView loadingModal = new ModalResultView(Application.Current.MainWindow, "loading");
            loadingModal.Show();
            await Task.Delay(500);
            try
            {
                ServiceResult saveResult = await _productServices.GetAllItem();
                if (!saveResult.Success)
                    throw new Exception(saveResult.ErrorMessage);

                if (saveResult.Data is ProductMetadata productMetadata)
                {
                    CurrentProductMetadata = productMetadata;
                }

                loadingModal.Close();
                FileHelper.EnableWindow();
            }
            catch (Exception ex)
            {
                await Task.Delay(1000);

                loadingModal.Close();
                FileHelper.CatchSystemError(ex);
            }
        }

        private async Task ProcessDeleteItem(ProductItem item)
        {
            ModalConfirmationVM dialogVM = new ModalConfirmationVM($"Delete {item?.Code}", "Are you sure you want to delete this?");
            ModalConfirmationView dialog = new ModalConfirmationView
            {
                DataContext = dialogVM,
                Owner = Application.Current.MainWindow
            };
            dialogVM.CloseAction = (result) => dialog.DialogResult = result;
            bool? result = dialog.ShowDialog();
            if (result == false)
            {
                FileHelper.EnableWindow();
                return;
            }
            ModalResultView loadingModal = new(Application.Current.MainWindow, "loading");
            loadingModal.Show();
            await Task.Delay(500);
            try
            {
                ServiceResult deleteResult = await _productServices.DeleteItem(item);

                if (!deleteResult.Success)
                    throw new CustomException(deleteResult.ErrorMessage, deleteResult.ErrorType);

                CurrentProductMetadata.ProductItem.Remove(item);
                loadingModal.Close();

                ModalResultView successModal = new(Application.Current.MainWindow, "success", $"Data has been deleted");
                successModal.ShowDialog();
                FileHelper.EnableWindow();
            }
            catch (CustomException ex)
            {
                loadingModal.Close();
                FileHelper.CatchError(ex);
            }
            catch (Exception ex)
            {
                loadingModal.Close();
                FileHelper.CatchSystemError(ex);
            }
        }
        #endregion
    }

}
