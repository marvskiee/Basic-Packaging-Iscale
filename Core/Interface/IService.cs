using ISCALE.MVVM.Model;

namespace ISCALE.Core.Interface
{
    public interface IService<T> where T : class
    {
        public Task CreateTable();
        public Task<ServiceResult> GetAllItem();
        public Task<ServiceResult> AddItem(T t);
        public Task<ServiceResult> UpdateItem(T t);
        public Task<ServiceResult> DeleteItem(T t);

    }
}
