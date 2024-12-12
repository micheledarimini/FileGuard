using System;
using System.Threading.Tasks;
using FileGuard.Core.Tests.LoggerTests;

namespace FileGuard.Core.Tests.LoggerTests
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Avvio test logger...");
            
            var testRunner = new LoggerTestRunner();
            
            try
            {
                await testRunner.RunTests();
                testRunner.PrintResults();
                
                Console.WriteLine("\nTest completati con successo!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nErrore durante i test: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }

            Console.WriteLine("\nPremi un tasto per uscire...");
            Console.ReadKey();
        }
    }
}
