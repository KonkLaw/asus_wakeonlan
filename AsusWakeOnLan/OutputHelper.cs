namespace AsusWakeOnLan;

class OutputHelper
{
    private const string DefaultOneOneIndent = " ";
    private string indent = string.Empty;

    public void Indent()
    {
        indent = DefaultOneOneIndent + indent;
    }

    public void Unindent()
    {
        if (indent.Length >= DefaultOneOneIndent.Length)
            indent = indent[DefaultOneOneIndent.Length..];
        else
            indent = string.Empty;
    }

    public void WriteLine(string message)
    {
        Console.WriteLine(indent + message);
    }
}