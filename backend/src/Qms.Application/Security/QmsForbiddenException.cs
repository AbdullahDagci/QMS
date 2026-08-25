namespace Qms.Application.Security;

public sealed class QmsForbiddenException(string message) : Exception(message);
