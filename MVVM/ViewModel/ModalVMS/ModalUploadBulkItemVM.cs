using ClosedXML.Excel;
using ISCALE.MVVM.Model;
using ISCALE.Utilities;
using ISCALE.Utilities.Services;
using Microsoft.Win32;
using MVVMCORE;
using System.Collections.ObjectModel;
using System.Windows;

namespace ISCALE.MVVM.ViewModel.ModalVMS
{
    public class ModalUploadBulkItemVM : ViewModelBase
    {
        private bool IsUploaded;
        private ProductMetadata _currentProductMetadata;

        public ProductMetadata CurrentProductMetadata
        {
            get { return _currentProductMetadata; }
            set { _currentProductMetadata = value; OnPropertyChanged(); }
        }

        public Action<bool?> CloseAction { get; set; }
        private ProductServices _itemServices;
        public ObservableCollection<ProductItem> Items { get; set; } = new ObservableCollection<ProductItem>();
        #region Init
        private string _message;
        public string Message
        {
            get { return _message; }
            set { _message = value; OnPropertyChanged(); }
        }

        private Style _messageStyle;
        public Style MessageStyle
        {
            get { return _messageStyle; }
            set { _messageStyle = value; OnPropertyChanged(); }
        }

        private string _filePath;
        public string FilePath
        {
            get { return _filePath; }
            set { _filePath = value; OnPropertyChanged(); }
        }

        private bool _isFormEnabled;

        public bool IsFormEnabled
        {
            get { return _isFormEnabled; }
            set { _isFormEnabled = value; OnPropertyChanged(); }
        }

        #endregion


        public ModalUploadBulkItemVM(ProductServices itemServices)
        {
            _itemServices = itemServices;
            CurrentProductMetadata = new ProductMetadata();
            CurrentProductMetadata.EffectiveDate = DateTime.Now.ToLocalTime().Date;
            Message = "Please choose excel file";
            MessageStyle = (Style)Application.Current.FindResource("NormalAlert_patched");
            IsFormEnabled = true;
        }

        #region Relay
        public RelayCommand ChooseFileCommand => new RelayCommand(ChooseFile);
        public RelayCommand CloseCommand => new RelayCommand(Close);
        public RelayCommand SubmitCommand => new RelayCommand(Submit, CanSubmit);
        #endregion


        #region Command
        private async void ChooseFile(object parameter)
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Filter = "Excel files (*.xlsx)|*.xlsx|All files (*.*)|*.*"
                };


                if (openFileDialog.ShowDialog() == true)
                {
                    FilePath = openFileDialog.FileName;
                    Message = "Validating data. Please wait...";
                    MessageStyle = (Style)Application.Current.FindResource("NormalAlert_patched");
                
                    IsFormEnabled = false;
                    await ReadExcelFile();
                }
            }
            catch (Exception ex)
            {
                Items.Clear();
                Message = ex.Message;

                if (ex.Message.Contains("used by another process"))
                    Message = "The file is already opened pleased close it and try again!";
                MessageStyle = (Style)Application.Current.FindResource("ErrorAlert_patched");
            }
            finally
            {
                IsFormEnabled = true;
            }
        }
        private async void Submit(object parameter)
        {
            IsFormEnabled = false;
            await UploadFile();
            IsFormEnabled = true;
        }
        private void Close(object parameter)
        {
            CloseAction.Invoke(IsUploaded);
        }
        #endregion

        #region CanCommand
        private bool CanSubmit(object parameter)
        {
            return IsFormEnabled && Items.Count > 0 && !string.IsNullOrEmpty(CurrentProductMetadata.RevisionNo) && !string.IsNullOrEmpty(CurrentProductMetadata.DocumentReferenceNo);
        }
        #endregion

        #region Functions 
        private async Task UploadFile()
        {
            Message = "Uploading... Please wait.";
            MessageStyle = (Style)Application.Current.FindResource("NormalAlert_patched");
            await Task.Delay(500);
            try
            {
                CurrentProductMetadata.ProductItem = new ObservableCollection<ProductItem>(Items);
                ServiceResult submitResult = await _itemServices.UploadCSV(CurrentProductMetadata);
                if (!submitResult.Success)
                    throw new CustomException(submitResult.ErrorMessage, submitResult.ErrorType);

                Message = "Data has been uploaded successfully.";
                MessageStyle = (Style)Application.Current.FindResource("SuccessAlert_patched");
                FilePath = string.Empty;
                Items.Clear();
                IsUploaded = true;
            }
            catch (CustomException ex)
            {
                if (ex.Message.Contains("UNIQUE"))
                    Message = "Data has already been added!";
                else
                    Message = ex.Message;
                MessageStyle = (Style)Application.Current.FindResource("SuccessAlert_patched");
            }
            catch (Exception ex)
            {
                Message = "Something went wrong!";
                MessageStyle = (Style)Application.Current.FindResource("ErrorAlert_patched");
            }
        }
        private async Task ReadExcelFile()
        {
            await Task.Delay(500);
            try
            {
                using (var workbook = new XLWorkbook(FilePath))
                {
                    Items.Clear();
                    var worksheet = workbook.Worksheet(1);
                    var rows = worksheet.RangeUsed().RowsUsed().Skip(1);
                    int countRow = 2;
                    foreach (var row in rows)
                    {
                        string code = row.Cell(1).GetFormattedString();
                        string internalReference = row.Cell(2).GetString();
                        string description = row.Cell(3).GetString();
                        decimal lowerLimit = decimal.TryParse(row.Cell(4).GetString(), out decimal lower) ? lower : 0;
                        decimal upperLimit = decimal.TryParse(row.Cell(5).GetString(), out decimal upper) ? upper : 0;
                        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(description))
                            throw new Exception($"Line {countRow} Error: Missing Display Name or Code# detected. Please correct and retry.");

                        if (upperLimit < lowerLimit)
                        {
                            throw new Exception($"Line {countRow} Error: Upper Limit is lower than Lower Limit detected. Please correct and retry.");
                        }
                        Items.Add(new ProductItem
                        {
                            Id = Guid.NewGuid().ToString(),
                            Code = code,
                            InternalReference = internalReference,
                            Description = description,
                            UpperLimit = upperLimit,
                            LowerLimit = lowerLimit 
                        });
                        countRow++;
                    }
                    if (Items.Count > 0)
                    {
                        Message = "The data is ready to upload.";
                        MessageStyle = (Style)Application.Current.FindResource("SuccessAlert_patched");
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        #endregion
    }
}
