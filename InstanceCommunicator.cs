using System;

namespace DisplayBrightness
{
    public static class InstanceCommunicator
    {
        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern bool AllowSetForegroundWindow(int dwProcessId);

        private const int ASFW_ANY = -1;

        public static void SignalExistingInstance()
        {
            try
            {
                AllowSetForegroundWindow(ASFW_ANY);

                using var client = new System.IO.Pipes.NamedPipeClientStream(".", "DisplayBrightness_Pipe", System.IO.Pipes.PipeDirection.Out);
                
                client.Connect(1000);
                client.WriteByte(1);
            }
            catch 
            {
            }
        }

        public static async void StartNamedPipeServer(Action onSignalReceived)
        {
            await System.Threading.Tasks.Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        using var server = new System.IO.Pipes.NamedPipeServerStream("DisplayBrightness_Pipe", System.IO.Pipes.PipeDirection.In);
                        await server.WaitForConnectionAsync();
                        server.ReadByte();
                        
                        onSignalReceived?.Invoke();
                    }
                    catch 
                    {
                        await System.Threading.Tasks.Task.Delay(1000);
                    }
                }
            });
        }
    }
}
