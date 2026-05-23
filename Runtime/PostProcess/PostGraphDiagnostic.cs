using HoUrp.Extensions.Core;

namespace HoUrp.Extensions.PostProcess
{
    public readonly struct PostGraphDiagnostic
    {
        public PostGraphDiagnostic(
            PostGraphDiagnosticSeverity severity,
            HoUrpIdentifier ownerId,
            string code,
            string message)
        {
            Severity = severity;
            OwnerId = ownerId;
            Code = code ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public PostGraphDiagnosticSeverity Severity { get; }
        public HoUrpIdentifier OwnerId { get; }
        public string Code { get; }
        public string Message { get; }
    }
}
