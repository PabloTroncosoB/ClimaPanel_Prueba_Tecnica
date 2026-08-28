namespace ClimaPanel.Web.Common;

public sealed class UserMessageException : Exception
{
    public UserMessageException(string message) : base(message)
    {
    }
}
