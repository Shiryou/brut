namespace ResourceUtilityLib
{
    public class UnsupportedVersionException : Exception
    {
        public UnsupportedVersionException()
        {
        }

        public UnsupportedVersionException(string message)
            : base(message)
        {
        }

        public UnsupportedVersionException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

    public class InvalidResourceException : Exception
    {
        public InvalidResourceException()
        {
        }

        public InvalidResourceException(string message)
            : base(message)
        {
        }

        public InvalidResourceException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
