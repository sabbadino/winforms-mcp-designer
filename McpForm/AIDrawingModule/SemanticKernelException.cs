

namespace AIDrawingModule
{

    [Serializable]
    public class SemanticKernelConfigurationException : SemanticKernelException
    {
        public SemanticKernelConfigurationException()
        {
        }

        public SemanticKernelConfigurationException(string? message) : base(message)
        {
        }

        public SemanticKernelConfigurationException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }

    [Serializable]
    public class SemanticKernelException : Exception
    {
        public SemanticKernelException()
        {
        }

        public SemanticKernelException(string? message) : base(message)
        {
        }

        public SemanticKernelException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}
