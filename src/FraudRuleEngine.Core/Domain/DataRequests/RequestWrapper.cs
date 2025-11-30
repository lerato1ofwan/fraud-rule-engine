namespace FraudRuleEngine.Core.Domain.DataRequests;

/// <summary>
/// Wrapper class to convert strongly-typed requests to IRequest<object>;
/// This is needed because IRequest<T>; cannot be directly cast to IRequest<object>; at runtime,
/// even though the interface is covariant.
/// </summary>
internal class RequestWrapper : IRequest<object>
{
    private readonly IRequest<object> _wrapped;

    public string RequestId => _wrapped.RequestId;

    public RequestWrapper(IRequest<object> request)
    {
        _wrapped = request;
    }

    /// <summary>
    /// Creates a wrapper for any IRequest<T>; by using the RequestId.
    /// </summary>
    public static IRequest<object> Wrap<T>(IRequest<T> request)
    {
        return new RequestWrapperImpl<T>(request);
    }

    private class RequestWrapperImpl<T> : IRequest<object>
    {
        private readonly IRequest<T> _request;

        public RequestWrapperImpl(IRequest<T> request)
        {
            _request = request;
        }

        public string RequestId => _request.RequestId;
    }
}

