using System;
using Microsoft.Diagnostics.Runtime;

static class Program
{
    static void Main(string[] args)
    {
        DataTarget dataTarget = DataTarget.LoadCoreDump(args[0]);
        ClrInfo version = dataTarget.ClrVersions[0];
        ClrRuntime runtime = version.CreateRuntime(version.LocalMatchingDac);
        foreach (ClrThread thread in runtime.Threads)
        {
            Console.WriteLine($"Thread {thread.OSThreadId:X}:");
            foreach (ClrStackFrame frame in thread.StackTrace)
                Console.WriteLine($"{frame.StackPointer,16:X} {frame.InstructionPointer,16:X} {frame}");
        }
    }
}
