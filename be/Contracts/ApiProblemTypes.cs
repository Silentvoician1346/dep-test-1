namespace be.Contracts;

public static class ApiProblemTypes
{
    public const string AuthenticationRequired = "urn:dep-test-1:problem:authentication-required";
    public const string AccessDenied = "urn:dep-test-1:problem:access-denied";
    public const string Conflict = "urn:dep-test-1:problem:conflict";
    public const string InvalidCredentials = "urn:dep-test-1:problem:invalid-credentials";
    public const string NotFound = "urn:dep-test-1:problem:not-found";
    public const string UnexpectedError = "urn:dep-test-1:problem:unexpected-error";
    public const string ValidationFailed = "urn:dep-test-1:problem:validation-failed";
}
