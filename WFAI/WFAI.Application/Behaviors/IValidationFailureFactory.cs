namespace WFAI.Application.Behaviors
{
    public interface IValidationFailureFactory<TResponse>
    {
        TResponse CreateFailure(IReadOnlyList<string> errors, int statusCode);
    }
}