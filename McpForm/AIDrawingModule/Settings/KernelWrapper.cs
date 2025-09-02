using Microsoft.SemanticKernel;
namespace AIDrawingModule.Settings
{
    public class KernelWrapper
    {
        public required KernelSettings KernelSettings { get; init; }
        public required Kernel Kernel { get; init; }

    }
}
