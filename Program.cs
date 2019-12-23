using System;
using System.Linq;
using Microsoft.Diagnostics.Runtime;

static class Program
{
    static void Main(string[] args)
    {
        using DataTarget dataTarget =
            int.TryParse(args[0], out int pid) ?
            DataTarget.PassiveAttachToProcess(pid) :
            DataTarget.LoadCoreDump(args[0]);

        using ClrRuntime runtime = dataTarget.ClrVersions.Single().CreateRuntime();

        Console.WriteLine("AppDomains:      {0:n0}", runtime.AppDomains.Length);
        Console.WriteLine("Managed Threads: {0:n0}", runtime.Threads.Length);

        foreach (ClrAppDomain domain in runtime.AppDomains)
        {
            Console.WriteLine("ID:      {0}", domain.Id);
            Console.WriteLine("Name:    {0}", domain.Name);
            Console.WriteLine("Address: {0}", domain.Address);

            foreach (ClrModule module in domain.Modules)
            {
                Console.WriteLine("Module: {0}", module.Name);
            }
        }

        foreach (ClrThread thread in runtime.Threads)
        {
            if (!thread.IsAlive)
                continue;

            Console.WriteLine("Thread {0:X}:", thread.OSThreadId);

            foreach (ClrStackFrame frame in thread.EnumerateStackTrace())
                Console.WriteLine("{0,12:X} {1,12:X} {2}", frame.StackPointer, frame.InstructionPointer, frame);

            Console.WriteLine();
        }
    }
}
