namespace AsusWakeOnLan;

class WolException : Exception
{
    public WolException(string text) : base(text) { }
}