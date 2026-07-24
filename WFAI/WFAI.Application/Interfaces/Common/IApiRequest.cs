namespace WFAI.Application.Interfaces.Common
{
    public interface IApiRequest
    {
        Task<T> GetAsync<T>(string url, object id, string token);
        Task<T> GetAllAsync<T>(string url, string token);
        Task<T> PostAsync<T>(string url, object objToCreate, string token);
        Task<T> UpdateAsync<T>(string url, object objToUpdate, string token);
        Task<T> DeleteAsync<T>(string url, object id, string token);
    }
}