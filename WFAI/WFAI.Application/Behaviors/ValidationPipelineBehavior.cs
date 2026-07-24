namespace WFAI.Application.Behaviors
{
    internal class ValidationPipelineBehavior<TRequest, TResponse>
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>, IValidateMe
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;
        private readonly IValidationFailureFactory<TResponse> _failureFactory;

        public ValidationPipelineBehavior(
            IEnumerable<IValidator<TRequest>> validators,
            IValidationFailureFactory<TResponse> failureFactory)
        {
            _validators = validators;
            _failureFactory = failureFactory;
        }

        public async ValueTask<TResponse> Handle(
            TRequest request,
            MessageHandlerDelegate<TRequest, TResponse> next,
            CancellationToken cancellationToken)
        {
            if (_validators.Any())
            {
                var context = new ValidationContext<TRequest>(request);
                var validationResults = await Task
                    .WhenAll(_validators.Select(vr => vr.ValidateAsync(context, cancellationToken)));

                var failures = validationResults.SelectMany(vr => vr.Errors)
                    .Where(f => f != null)
                    .ToList();

                if (failures.Count > 0)
                {
                    var errorMessages = failures.Select(f => f.ErrorMessage).ToList();
                    return _failureFactory.CreateFailure(errorMessages, 400);
                }
            }

            return await next(request, cancellationToken);
        }
    }
}