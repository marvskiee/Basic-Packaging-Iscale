using MVVMCORE;
using ISCALE.Core.Base;
using ISCALE.Core.Interface;
using ISCALE.MVVM.Model;
using ISCALE.MVVM.View.ModalViews;
using ISCALE.Utilities;
using System.Collections.ObjectModel;
using System.Windows;
using ISCALE.MVVM.ViewModel.ModalVMS;

namespace ISCALE.Core.Abstract
{
    public abstract class ATableManagerVM<Model, Service> : ViewModelBase where Model : BTableManagerModel, new() where Service : IService<Model>
    {
        protected Service _service;
        public ObservableCollection<Model> Items { get; set; }

        #region Button Labels
        private string _addLabel;
        public string AddLabel
        {
            get { return _addLabel; }
            set { _addLabel = value; OnPropertyChanged(); }
        }

        private string _clearLabel;

        public string ClearLabel
        {
            get { return _clearLabel; }
            set { _clearLabel = value; OnPropertyChanged(); }
        }
        #endregion

        #region SelectedTableItem Bind
        private Model _tempTableItem;
        public Model TempTableItem
        {
            get { return _tempTableItem; }
            set { _tempTableItem = value; OnPropertyChanged(); }
        }

        public Model _selectedTableItem;
        public Model SelectedTableItem
        {
            get => _selectedTableItem;
            set
            {
                if (!Equals(_selectedTableItem, value))
                {
                    _selectedTableItem = value;
                    if (_selectedTableItem != null)
                    {
                        OverrideSelectedTableItem();
                        AddLabel = "Update";
                        ClearLabel = "Cancel";
                    }
                    OnPropertyChanged();
                }
            }
        }
        #endregion

        #region CRUD
        public async Task LoadTable()
        {
            FileHelper.DisableWindow();
            ModalResultView loadingModal = new ModalResultView(Application.Current.MainWindow, "loading");
            loadingModal.Show();
            await Task.Delay(500);
            Items.Clear();
            try
            {
                ServiceResult saveResult = await _service.GetAllItem();
                if (!saveResult.Success)
                    throw new Exception(saveResult.ErrorMessage);

                if (saveResult.Data is List<Model> items)
                {
                    foreach (var item in items)
                    {
                        Items.Add(item);
                    }
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

        protected async void Save(object param)
        {
            FileHelper.DisableWindow();
            ModalResultView loadingModal = new ModalResultView(Application.Current.MainWindow, "loading");
            loadingModal.Show();
            await Task.Delay(500);
            try
            {
                string action = String.IsNullOrEmpty(SelectedTableItem?.Id) ? "added" : "updated";
                await OverridePreSave(loadingModal);

                Clear(null);

                loadingModal.Close();
                ModalResultView successModal = new ModalResultView(Application.Current.MainWindow, "success", $"Data has been {action}");
                successModal.ShowDialog();
                FileHelper.EnableWindow();
            }
            catch(CustomException ex)
            {
                loadingModal.Close();
                if (ex.ErrorType == Constants.DUPLICATE_ERROR_TYPE)
                    FileHelper.CatchError(ex);
                else
                    throw new Exception(ex.ErrorType);
            }
            catch (Exception ex)
            {
                FileHelper.CatchSystemError(ex);
            }
        }

        protected async void ProcessDeleteItem(string item_name)
        {
            ModalConfirmationVM dialogVM = new ModalConfirmationVM($"Delete {item_name}", "Are you sure you want to delete this?");
            ModalConfirmationView dialog = new ModalConfirmationView
            {
                DataContext = dialogVM,
                Owner = Application.Current.MainWindow
            };
            dialogVM.CloseAction = (result) => dialog.DialogResult = result;
            bool? result = dialog.ShowDialog();
            if (result == false)
            {
                Clear(null);
                FileHelper.EnableWindow();
                return;
            }
            ModalResultView loadingModal = new(Application.Current.MainWindow, "loading");
            loadingModal.Show();
            await Task.Delay(500);
            try
            {
                ServiceResult deleteResult = await _service.DeleteItem(SelectedTableItem);

                if (!deleteResult.Success)
                    throw new CustomException(deleteResult.ErrorMessage, deleteResult.ErrorType);

                Items.Remove(SelectedTableItem);

                _selectedTableItem = new();
                DefaultFormButton();

                loadingModal.Close();

                ModalResultView successModal = new (Application.Current.MainWindow, "success", $"Data has been deleted");
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
            finally
            {
                SelectedTableItem = null;
                SelectedTableItem = new();
                TempTableItem = new();
                DefaultFormButton();
            }
        }

        #endregion

        protected void DefaultFormButton()
        {
            AddLabel = "Add";
            ClearLabel = "Clear";
        }

        public ATableManagerVM(Service service)
        {
            Clear(null);
            _service = service;
            Items = new ObservableCollection<Model>();
            LoadTable();
        }

        protected abstract void OverrideSelectedTableItem();
        protected abstract Task OverridePreSave(ModalResultView loadingModal);
        protected abstract bool CanSave(object param);
        protected abstract void Clear(object param);
        protected abstract void Delete(object param);
        protected abstract bool CanClear(object param);
    }
}
