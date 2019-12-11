using System;
using System.IO;
using Microsoft.Diagnostics.Runtime;

static class Program
{
    static void Main(string[] args)
    {
        // Create the DataTarget.  This can be done through a crash dump, a live process pid, or
        // via an IDebugClient pointer.
        DataTarget dataTarget = DataTarget.LoadCoreDump(args[0]); // Do not use LoadCrashDump().
        //DataTarget dataTarget = DataTarget.AttachToProcess(int.Parse(args[0]), 5000, AttachFlag.Passive);

        // DataTarget.ClrVersions lists the versions of CLR loaded in the process (this may be
        // v2 and v4 in the Side-By-Side case.
        ClrInfo version = dataTarget.ClrVersions[0];
        Console.WriteLine("Found CLR Version: " + version.Version);

        // CLRVersionInfo contains information on the correct Dac dll to load.  This includes
        // the long named dac, the version of clr, etc.  This is enough information to request
        // the dac from the symbol server (though we do not provide an API to do this).  Also,
        // if the version you are debugging is actually installed on your machine, DacLocation
        // will contain the full path to the dac.
        // string dac = dataTarget.SymbolLocator.FindBinary(version.DacInfo);
        string dac = version.LocalMatchingDac;
        // string dac = "/usr/share/dotnet/shared/Microsoft.NETCore.App/3.0.0/libmscordaccore.so";
        Console.WriteLine("DAC location: " + dac);
        if (dac == null || !File.Exists(dac))
        {
            throw new FileNotFoundException("Could not find the specified dac.", dac);
        }

        // Now create a CLRRuntime instance.  This is the "root" object of the API. It allows
        // you to do things like Enumerate memory regions in CLR, enumerate managed threads,
        // enumerate AppDomains, etc.
        ClrRuntime runtime = version.CreateRuntime(dac);

        // Print out some basic information like:  Number of managed threads, number of AppDomains,
        // number of objects on the heap, and so on.  Note you can walk the AppDomains in the process
        // with:  foreach (CLRAppDomain domain in runtime.AppDomains)
        // Same for runtime.Threads to walk the threads in the process.  You can walk callstacks
        // (similar to !clrstack) with:
        //    foreach (CLRStackFrame frame in runtime.Threads[i].StackTrace)
        Console.WriteLine("AppDomains:      {0:n0}", runtime.AppDomains.Count);
        Console.WriteLine("Managed Threads: {0:n0}", runtime.Threads.Count);

        // Print out AppDomains.
        foreach (ClrAppDomain domain in runtime.AppDomains)
        {
            Console.WriteLine("ID:      {0}", domain.Id);
            Console.WriteLine("Name:    {0}", domain.Name);
            Console.WriteLine("Address: {0:X}", domain.Address);

            // Print out modules loaded for each ClrAppDomain.
            foreach (ClrModule module in domain.Modules)
            {
                Console.WriteLine($"Module:  {module.ImageBase:X} {module.Name}");
            }
        }

        // Print out the call stack for each thread in the process.
        foreach (ClrThread thread in runtime.Threads)
        {
            if (!thread.IsAlive)
                continue;

            Console.WriteLine("Thread {0:X}:", thread.OSThreadId);

            foreach (ClrStackFrame frame in thread.StackTrace)
                Console.WriteLine("{0,12:X} {1,12:X} {2}", frame.StackPointer, frame.InstructionPointer, frame);

            Console.WriteLine();
        }
    }
}
