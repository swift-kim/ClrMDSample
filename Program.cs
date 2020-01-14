using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.Diagnostics.Runtime;

namespace ClrMDSample
{
    public static class Program
    {
        private static void PrintStackTraces(string target = null)
        {
            if (target == null)
                target = Process.GetCurrentProcess().Id.ToString();

            using var dataTarget = int.TryParse(target, out var pid) ?
                DataTarget.PassiveAttachToProcess(pid) : DataTarget.LoadCoreDump(target);

            using var runtime = dataTarget.ClrVersions[0].CreateRuntime();

            Console.WriteLine("AppDomains:      {0:n0}", runtime.AppDomains.Length);
            Console.WriteLine("Managed Threads: {0:n0}", runtime.Threads.Length);

            foreach (var domain in runtime.AppDomains)
            {
                Console.WriteLine("ID:      {0}", domain.Id);
                Console.WriteLine("Name:    {0}", domain.Name);
                Console.WriteLine("Address: {0}", domain.Address);

                //foreach (ClrModule module in domain.Modules)
                //{
                //    Console.WriteLine("Module: {0}", module.Name);
                //}
            }

            foreach (var thread in runtime.Threads)
            {
                if (!thread.IsAlive)
                    continue;

                Console.WriteLine("Thread {0:X}:", thread.OSThreadId);
                foreach (ClrStackFrame frame in thread.EnumerateStackTrace())
                {
                    Console.WriteLine("{0,12:X} {1,12:X} {2}", frame.StackPointer, frame.InstructionPointer, frame);
                }
                Console.WriteLine();
            }

            //foreach (ClrHandle handle in runtime.EnumerateHandles())
            //{
            //    string objectType = runtime.Heap.GetObjectType(handle.Object).Name;
            //    Console.WriteLine("{0,12:X} {1,12} {2}", handle.Address, handle.HandleKind, objectType);
            //}

            Console.WriteLine("{0,12} {1,12} {2,12} {3,12} {4}", "Start", "End", "CommittedEnd", "ReservedEnd", "Type");
            foreach (var segment in runtime.Heap.Segments)
            {
                string type;
                if (segment.IsEphemeralSegment)
                    type = "Ephemeral";
                else if (segment.IsLargeObjectSegment)
                    type = "Large";
                else
                    type = "Gen2";

                Console.WriteLine("{0,12:X} {1,12:X} {2,12:X} {3,12:X} {4}", segment.Start, segment.End, segment.CommittedEnd, segment.ReservedEnd, type);
            }

            //if (!runtime.Heap.CanWalkHeap)
            //{
            //    Console.WriteLine("Cannot walk the heap!");
            //}
            //else
            //{
            //    foreach (ClrSegment seg in runtime.Heap.Segments)
            //    {
            //        foreach (ClrObject obj in seg.EnumerateObjects())
            //        {
            //            ClrType type = runtime.Heap.GetObjectType(obj);

            //            // If heap corruption, continue past this object.
            //            if (type == null)
            //                continue;

            //            Console.WriteLine("{0,12:X} {1,8:n0} {2,1:n0} {3}", obj.Address, obj.Size, seg.GetGeneration(obj), type.Name);
            //        }
            //    }
            //}

            var stringType = runtime.Heap.StringType;
            Console.WriteLine($"Inspecting {stringType}");
            foreach (var method in stringType.Methods)
            {
                var compileType = method.CompilationType;
                var startAddress = method.NativeCode;
                var endAddress = method.ILOffsetMap.Length > 0 ? method.ILOffsetMap.Select(entry => entry.EndAddress).Max() : 0;
                Console.WriteLine($"start={startAddress:X} end={endAddress:X} compileType={compileType} {method}");
            }
        }

        private static Assembly Resolve(object sender, ResolveEventArgs args)
        {
            Console.WriteLine($"Resolving {args.Name}...");
            var assemblyName = args.Name.Split(',')[0];
            var assemblyPath = Path.Combine("/home/owner/share/tmp/sdk_tools/clrmd", assemblyName + ".dll");
            return File.Exists(assemblyPath) ? Assembly.LoadFile(assemblyPath) : null;
        }

        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.AssemblyResolve += Resolve;

            var task = new Thread(() => PrintStackTraces(args.ElementAtOrDefault(0)));
            task.Start();
            task.Join();
        }
    }
}
