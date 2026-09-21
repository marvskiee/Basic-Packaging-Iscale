using Formatter.Helper;
using ISCALE.Core.Abstract;
using ISCALE.MVVM.Model;
using ISCALE.MVVM.View.ModalViews;
using ISCALE.Utilities;
using ISCALE.Utilities.Services;
using Microsoft.Data.Sqlite;
using MVVMCORE;
using System.Collections.ObjectModel;
namespace ISCALE.MVVM.ViewModel.AdminVMS
{
    public class AdminUserVM : ATableManagerVM<User,UserServices>
    {
        #region init
        public ObservableCollection<string> Roles { get; set; } = new ObservableCollection<string>(Constants.ROLE_COLLECTION);

        private string? _selectedRole;
        public string? SelectedRole
        {
            get { return _selectedRole; }
            set {
                if (_selectedRole != value)
                {
                    _selectedRole = value;
                    if (TempTableItem != null)
                        TempTableItem.Role = _selectedRole;
                    OnPropertyChanged();
                }
            }
        }

        #endregion
        public AdminUserVM(SqliteConnection connection) : base(new UserServices(connection))
        { }

        #region Relay
        public RelayCommand SaveCommand => new RelayCommand(Save, CanSave);
        public RelayCommand ClearCommand => new RelayCommand(Clear, CanClear);
        public RelayCommand DeleteCommand => new RelayCommand(Delete);

        #endregion

        #region Override Command
        protected override async Task OverridePreSave(ModalResultView loadingModal)
        {
            User newUser = new()
            {
                Id = TempTableItem.Id,
                Username = TempTableItem.Username,
                Password = TempTableItem.Password != String.Empty ? CryptographyHelper.Encrypt(TempTableItem.Password, Constants.PASSWORD_HASH) : SelectedTableItem.Password,
                Role = SelectedRole?.ToString() ?? string.Empty,
            };

            if (string.IsNullOrEmpty(SelectedTableItem?.Id))
            {
                newUser.Id = Guid.NewGuid().ToString();
                ServiceResult postResult = await _service.AddItem(newUser);
                loadingModal.Close();

                if (!postResult.Success)
                    throw new CustomException(postResult.ErrorMessage, postResult.ErrorType);

                Items.Add(newUser);
            }
            else
            {
                //Update Item
                ServiceResult updateResult = await _service.UpdateItem(newUser);
                loadingModal.Close();

                if (!updateResult.Success)
                    throw new CustomException(updateResult.ErrorMessage, updateResult.ErrorType);

                int index = Items.IndexOf(SelectedTableItem);
                if (index != -1)
                {
                    Items[index] = newUser;

                    OnPropertyChanged(nameof(Items));
                    SelectedTableItem = new User();
                }
            }
        }
        protected override void OverrideSelectedTableItem()
        {
            TempTableItem = new User
            {
                Id = _selectedTableItem.Id,
                Username = _selectedTableItem.Username,
                Role = _selectedTableItem.Role,
            };
            SelectedRole = Roles?.FirstOrDefault((type)=>type.Equals(_selectedTableItem.Role));
        }

        protected override void Delete(object parameter)
        {
            if (parameter != null)
            {
                FileHelper.DisableWindow();

                string item_name;

                var _user = (User)parameter;
                item_name = _user?.Username ?? string.Empty;

                ProcessDeleteItem(item_name);
            }
        }

        protected override void Clear(object parameter)
        {
            SelectedTableItem = null;
            SelectedTableItem = new User();

            SelectedRole = null;

            DefaultFormButton();
        }
        #endregion

        #region CanCommand
        protected override bool CanSave(object parameter)
        {
            return !string.IsNullOrWhiteSpace(TempTableItem.Username) &&
                !string.IsNullOrWhiteSpace(TempTableItem.Role);
        }
        protected override bool CanClear(object parameter)
        {
            return !string.IsNullOrWhiteSpace(TempTableItem.Username) ||
                !string.IsNullOrWhiteSpace(TempTableItem.Role);
        }
        #endregion

    }
}
