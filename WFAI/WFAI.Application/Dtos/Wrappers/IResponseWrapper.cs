namespace WFAI.Application.Dtos.Wrappers
{
    public interface IResponseWrapper
    {
        IReadOnlyList<string> Messages { get; }
        bool IsSuccessful { get; }
        int StatusCode { get; } // Added
    }


    public interface IResponseWrapper<out T> : IResponseWrapper
    {
        T Data { get; }
    }


}