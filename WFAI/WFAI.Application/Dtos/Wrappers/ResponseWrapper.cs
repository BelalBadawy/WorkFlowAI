namespace WFAI.Application.Dtos.Wrappers
{
    public class ResponseWrapper : IResponseWrapper
    {
        public IReadOnlyList<string> Messages { get; init; } = [];
        public bool IsSuccessful { get; init; }
        public int StatusCode { get; init; } = 500;

        #region Fail Synchronously
        public static IResponseWrapper Fail(int statusCode = 500)
        {
            return new ResponseWrapper { IsSuccessful = false, StatusCode = statusCode };
        }

        public static IResponseWrapper Fail(string message, int statusCode = 500)
        {
            return new ResponseWrapper { IsSuccessful = false, Messages = [message], StatusCode = statusCode };
        }

        public static IResponseWrapper Fail(IReadOnlyList<string> messages, int statusCode = 500)
        {
            return new ResponseWrapper { IsSuccessful = false, Messages = messages, StatusCode = statusCode };
        }
        #endregion

        #region Fail Asynchronously
        public static Task<IResponseWrapper> FailAsync(int statusCode = 500)
        {
            return Task.FromResult(Fail(statusCode));
        }

        public static Task<IResponseWrapper> FailAsync(string message, int statusCode = 500)
        {
            return Task.FromResult(Fail(message, statusCode));
        }

        public static Task<IResponseWrapper> FailAsync(IReadOnlyList<string> messages, int statusCode = 500)
        {
            return Task.FromResult(Fail(messages, statusCode));
        }
        #endregion

        #region Success Synchronously
        public static IResponseWrapper Success(int statusCode = 200)
        {
            return new ResponseWrapper { IsSuccessful = true, StatusCode = statusCode };
        }

        public static IResponseWrapper Success(string message, int statusCode = 200)
        {
            return new ResponseWrapper { IsSuccessful = true, Messages = [message], StatusCode = statusCode };
        }

        public static IResponseWrapper Success(IReadOnlyList<string> messages, int statusCode = 200)
        {
            return new ResponseWrapper { IsSuccessful = true, Messages = messages, StatusCode = statusCode };
        }
        #endregion

        #region Success Asynchronously
        public static Task<IResponseWrapper> SuccessAsync(int statusCode = 200)
        {
            return Task.FromResult(Success(statusCode));
        }

        public static Task<IResponseWrapper> SuccessAsync(string message, int statusCode = 200)
        {
            return Task.FromResult(Success(message, statusCode));
        }

        public static Task<IResponseWrapper> SuccessAsync(IReadOnlyList<string> messages, int statusCode = 200)
        {
            return Task.FromResult(Success(messages, statusCode));
        }
        #endregion
    }

    public class ResponseWrapper<T> : IResponseWrapper<T>
    {
        public IReadOnlyList<string> Messages { get; init; } = [];
        public bool IsSuccessful { get; init; }
        public int StatusCode { get; init; } = 500;
        public T Data { get; init; } = default!;

        public ResponseWrapper() { }

        #region Fail Synchronously
        public static IResponseWrapper<T> Fail(int statusCode = 500)
        {
            return new ResponseWrapper<T> { IsSuccessful = false, StatusCode = statusCode };
        }

        public static IResponseWrapper<T> Fail(string message, int statusCode = 500)
        {
            return new ResponseWrapper<T> { IsSuccessful = false, Messages = [message], StatusCode = statusCode };
        }

        public static IResponseWrapper<T> Fail(IReadOnlyList<string> messages, int statusCode = 500)
        {
            return new ResponseWrapper<T> { IsSuccessful = false, Messages = messages, StatusCode = statusCode };
        }
        #endregion

        #region Fail Asynchronously
        public static Task<IResponseWrapper<T>> FailAsync(int statusCode = 500)
        {
            return Task.FromResult(Fail(statusCode));
        }

        public static Task<IResponseWrapper<T>> FailAsync(string message, int statusCode = 500)
        {
            return Task.FromResult(Fail(message, statusCode));
        }

        public static Task<IResponseWrapper<T>> FailAsync(IReadOnlyList<string> messages, int statusCode = 500)
        {
            return Task.FromResult(Fail(messages, statusCode));
        }
        #endregion

        #region Success Synchronously
        public static IResponseWrapper<T> Success(int statusCode = 200)
        {
            return new ResponseWrapper<T> { IsSuccessful = true, StatusCode = statusCode };
        }

        public static IResponseWrapper<T> Success(string message, int statusCode = 200)
        {
            return new ResponseWrapper<T> { IsSuccessful = true, Messages = [message], StatusCode = statusCode };
        }

        public static IResponseWrapper<T> Success(IReadOnlyList<string> messages, int statusCode = 200)
        {
            return new ResponseWrapper<T> { IsSuccessful = true, Messages = messages, StatusCode = statusCode };
        }

        public static IResponseWrapper<T> Success(T data, int statusCode = 200)
        {
            return new ResponseWrapper<T> { Data = data, IsSuccessful = true, StatusCode = statusCode };
        }

        public static IResponseWrapper<T> Success(T data, string message, int statusCode = 200)
        {
            return new ResponseWrapper<T> { Data = data, IsSuccessful = true, Messages = [message], StatusCode = statusCode };
        }

        public static IResponseWrapper<T> Success(T data, IReadOnlyList<string> messages, int statusCode = 200)
        {
            return new ResponseWrapper<T> { Data = data, IsSuccessful = true, Messages = messages, StatusCode = statusCode };
        }
        #endregion

        #region Success Asynchronously
        public static Task<IResponseWrapper<T>> SuccessAsync(int statusCode = 200)
        {
            return Task.FromResult(Success(statusCode));
        }

        public static Task<IResponseWrapper<T>> SuccessAsync(string message, int statusCode = 200)
        {
            return Task.FromResult(Success(message, statusCode));
        }

        public static Task<IResponseWrapper<T>> SuccessAsync(IReadOnlyList<string> messages, int statusCode = 200)
        {
            return Task.FromResult(Success(messages, statusCode));
        }

        public static Task<IResponseWrapper<T>> SuccessAsync(T data, int statusCode = 200)
        {
            return Task.FromResult(Success(data, statusCode));
        }

        public static Task<IResponseWrapper<T>> SuccessAsync(T data, string message, int statusCode = 200)
        {
            return Task.FromResult(Success(data, message, statusCode));
        }

        public static Task<IResponseWrapper<T>> SuccessAsync(T data, IReadOnlyList<string> messages, int statusCode = 200)
        {
            return Task.FromResult(Success(data, messages, statusCode));
        }
        #endregion
    }


}