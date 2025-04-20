using System;
using System.Diagnostics;
using System.Threading.Tasks;
namespace SmartCache.NET
{
    public class Program
    {
        static int executionCounter = 0;

        static async Task Main(string[] args)
        {
            string testParam = "TEST-KEY";
            List<int> testList = new List<int>();
            int totalCalls = 10;

            var tasks = new Task<string>[totalCalls];

            Console.WriteLine("---- Starting parallel calls ----");

            Stopwatch sw = Stopwatch.StartNew();

            for (int i = 0; i < totalCalls; i++)
            {
                //testList.Add(i);
                int localIndex = i; // Capture the current value of i
                tasks[i] = Task.Run(async () =>
                {
                    var result = await Executor.Instance.GetDataAsync<string>(() =>
                    {
                        Interlocked.Increment(ref executionCounter);
                        Console.WriteLine($"[CodeBlock] Executed by Thread: {Task.CurrentId}");
                        Task.Delay(500).Wait(); // Simulate delay
                        return "Hello World";
                    }, testList, 10, 5); // 10 secs cache, 5 secs async refresh
                    return result;
                });
            }

            await Task.WhenAll(tasks);

            sw.Stop();

            Console.WriteLine("\n---- Results ----");
            foreach (var t in tasks)
            {
                Console.WriteLine($"Returned: {t.Result}");
            }
            Console.WriteLine($"\n[Total CodeBlock Executions]: {executionCounter}");
            Console.WriteLine($"[Total Time]: {sw.ElapsedMilliseconds} ms");

            // Insert the async refresh test here
            await Task.Delay(6000); // Allow async refresh period to elapse

            Console.WriteLine("\n---- Triggering Async Refresh ----");

            var refreshedResult = await Executor.Instance.GetDataAsync<string>(() =>
            {
                Interlocked.Increment(ref executionCounter);
                Console.WriteLine($"[CodeBlock - Refresh] Executed by Thread: {Task.CurrentId}");
                return "Refreshed Hello World";
            }, testList, 10, 5); // 10 secs cache duration, 5 secs async refresh interval

            Console.WriteLine($"[Result After Async Refresh]: {refreshedResult}");
            Console.WriteLine($"[Total CodeBlock Executions]: {executionCounter}");
        }

    }
}