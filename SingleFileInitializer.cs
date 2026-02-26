using System;
using System.Runtime.CompilerServices;

namespace DisplayBrightness
{
    internal static class SingleFileInitializer
    {
        [ModuleInitializer]
        internal static void Init()
        {
            Environment.SetEnvironmentVariable("MICROSOFT_WINDOWSAPPRUNTIME_BASE_DIRECTORY", AppContext.BaseDirectory);
        }
    }
}
