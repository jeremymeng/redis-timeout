using StackExchange.Redis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace BasicTest
{
    static class Program
    {
        static void Main(string[] args)
        {
            var endpoint = args[0];
            Console.WriteLine($"Connecting to {endpoint}...");
            using (var conn = ConnectionMultiplexer.Connect($"{endpoint}:6379"))
            {
                Console.WriteLine($"Connected to {endpoint}");

                var db = conn.GetDatabase();

                RedisKey key = Me();
                db.KeyDelete(key);
                db.StringSet(key, "abc");

                var exceptions = new ConcurrentBag<Exception>();
                var numberConcurrency = int.Parse(args[1]);

                Parallel.For(1, numberConcurrency, (i) =>
                        {
                            try
                            {
                                var sw = new Stopwatch();
                                sw.Start();
                                conn.GetDatabase().StringGet(key);
                                sw.Stop();
                                System.Console.WriteLine($"{i}: {sw.ElapsedMilliseconds}  ms");
                                PrintThreadPoolStats(i);
                            }
                            catch (Exception ex)
                            {
                                exceptions.Add(ex);
                            }
                        });

                foreach (var ex in exceptions)
                {
                    //Console.WriteLine(ex.ToString());
                }
                Console.WriteLine($"# of exceptions: {exceptions.Count}");
            }
        }


        internal static void PrintThreadPoolStats(int i)
        {
            int maxIoThreads, maxWorkerThreads;
            ThreadPool.GetMaxThreads(out maxWorkerThreads, out maxIoThreads);

            int freeIoThreads, freeWorkerThreads;
            ThreadPool.GetAvailableThreads(out freeWorkerThreads, out freeIoThreads);

            int minIoThreads, minWorkerThreads;
            ThreadPool.GetMinThreads(out minWorkerThreads, out minIoThreads);

            int busyIoThreads = maxIoThreads - freeIoThreads;
            int busyWorkerThreads = maxWorkerThreads - freeWorkerThreads;

            var worker = $"{i}: (Busy={busyWorkerThreads},Min={minWorkerThreads})";
            System.Console.WriteLine(worker);
        }
        
        internal static string Me([CallerMemberName] string caller = null)
        {
            return caller;
        }
    }
}
