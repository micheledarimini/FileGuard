﻿using System;
using System.Threading.Tasks;

namespace FileGuard.Tests
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("FileGuard Logger Test Suite");
            Console.WriteLine("=========================\n");

            var runner = new LoggerTestRunner();
            
            try
            {
                await runner.RunBasicTests();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nTest fallito con errore:");
                Console.WriteLine($"- Tipo: {ex.GetType().Name}");
                Console.WriteLine($"- Messaggio: {ex.Message}");
                Environment.Exit(1);
            }
            finally
            {
                runner.Cleanup();
            }
        }
    }
}
