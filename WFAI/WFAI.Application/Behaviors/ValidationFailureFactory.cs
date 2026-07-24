using System.Reflection;
using WFAI.Application.Dtos.Wrappers;

namespace WFAI.Application.Behaviors
{
    internal sealed class ValidationFailureFactory<TResponse> : IValidationFailureFactory<TResponse>
    {
        // Reflection runs exactly once per TResponse type (at class initialization), not per request.
        private static readonly Func<IReadOnlyList<string>, int, TResponse> _create = BuildFactory();

        public TResponse CreateFailure(IReadOnlyList<string> errors, int statusCode)
            => _create(errors, statusCode);

        private static Func<IReadOnlyList<string>, int, TResponse> BuildFactory()
        {
            if (typeof(TResponse).IsGenericType &&
                typeof(TResponse).GetGenericTypeDefinition() == typeof(IResponseWrapper<>))
            {
                var dataType = typeof(TResponse).GetGenericArguments()[0];
                var wrapperType = typeof(ResponseWrapper<>).MakeGenericType(dataType);
                var failMethod = wrapperType.GetMethod(
                    nameof(ResponseWrapper<object>.Fail),
                    BindingFlags.Public | BindingFlags.Static,
                    [typeof(IReadOnlyList<string>), typeof(int)])!;

                return (errors, statusCode) => (TResponse)failMethod.Invoke(null, [errors, statusCode])!;
            }

            return (errors, statusCode) => (TResponse)ResponseWrapper.Fail(errors, statusCode);
        }
    }
}