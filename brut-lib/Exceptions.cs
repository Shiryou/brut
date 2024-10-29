namespace ResourceUtilityLib
{
    public class UnsupportedVersionException : Exception
    {
        protected static readonly string error = "%s is not a supported resource file version.";
        public UnsupportedVersionException()
        {
        }

        public UnsupportedVersionException(uint version)
            : base(String.Format(error, version))
        {
        }

        public UnsupportedVersionException(uint version, Exception inner)
            : base(String.Format(error, version), inner)
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
