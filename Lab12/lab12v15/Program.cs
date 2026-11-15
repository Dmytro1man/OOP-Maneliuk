using System;
using System.IO;
using System.Net.Http;
using System.Threading;

namespace lab12v15
{
    // --- 1. Клас для роботи з файлами (Варіант 15) ---
    public class FileProcessor
    {
        private int _loadAttempts = 0;

        public string LoadConfiguration(string path)
        {
            _loadAttempts++;
            // Імітувати IOException перші 2 рази, потім успіх
            if (_loadAttempts <= 2)
            {
                throw new IOException($"Помилка читання файлу '{path}' (Спроба {_loadAttempts}).");
            }
            return "{ \"LocalConfig\": \"Loaded Successfully\" }";
        }
    }

    // --- 2. Клас для роботи з мережею (Варіант 15) ---
    public class NetworkClient
    {
        private int _downloadAttempts = 0;

        public string DownloadConfiguration(string url)
        {
            _downloadAttempts++;
            // Імітувати HttpRequestException перші 3 рази, потім успіх
            if (_downloadAttempts <= 3)
            {
                throw new HttpRequestException($"Помилка підключення до '{url}' (Спроба {_downloadAttempts}).");
            }
            return "{ \"RemoteConfig\": \"Downloaded Successfully\" }";
        }
    }

    // --- 3. Допоміжний клас RetryHelper ---
    public static class RetryHelper
    {
        public static T ExecuteWithRetry<T>(
            Func<T> operation, 
            int retryCount = 3, 
            TimeSpan initialDelay = default, 
            Func<Exception, bool>? shouldRetry = null)
        {
            int currentAttempt = 0;
            
            while (true)
            {
                try
                {
                    return operation();
                }
                catch (Exception ex)
                {
                    currentAttempt++;
                    
                    // Перевіряємо, чи вичерпано ліміт спроб, або чи підходить виняток під умову
                    if (currentAttempt > retryCount || (shouldRetry != null && !shouldRetry(ex)))
                    {
                        Console.WriteLine($"[CRITICAL] Операцію скасовано після {currentAttempt - 1} повторних спроб або через непередбачений виняток.");
                        throw;
                    }

                    // Експоненційна затримка: initialDelay * 2^(currentAttempt - 1)
                    double delayMultiplier = Math.Pow(2, currentAttempt - 1);
                    TimeSpan delay = TimeSpan.FromMilliseconds(initialDelay.TotalMilliseconds * delayMultiplier);
                    
                    Console.WriteLine($"[Спроба {currentAttempt}/{retryCount}] Невдача: {ex.GetType().Name}. Повторення через {delay.TotalMilliseconds} мс...");
                    Thread.Sleep(delay);
                }
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            
            var fileProcessor = new FileProcessor();
            var networkClient = new NetworkClient();

            // Умова: Повторювати для IOException та HttpRequestException
            Func<Exception, bool> retryPolicy = ex => ex is IOException || ex is HttpRequestException;

            // --- Сценарій 1: Читання конфігурації з файлу ---
            Console.WriteLine("=== 1. Завантаження локальної конфігурації ===");
            try
            {
                string localConfig = RetryHelper.ExecuteWithRetry(
                    operation: () => fileProcessor.LoadConfiguration("config.json"),
                    retryCount: 3,
                    initialDelay: TimeSpan.FromMilliseconds(500),
                    shouldRetry: retryPolicy
                );
                Console.WriteLine($"[УСПІХ] Результат: {localConfig}\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ПОМИЛКА] {ex.Message}\n");
            }

            // --- Сценарій 2: Завантаження конфігурації з мережі ---
            Console.WriteLine("=== 2. Завантаження віддаленої конфігурації ===");
            try
            {
                // Ставимо 4 спроби, оскільки за умовою метод падає 3 рази
                string remoteConfig = RetryHelper.ExecuteWithRetry(
                    operation: () => networkClient.DownloadConfiguration("https://api.example.com/config"),
                    retryCount: 4, 
                    initialDelay: TimeSpan.FromMilliseconds(300),
                    shouldRetry: retryPolicy
                );
                Console.WriteLine($"[УСПІХ] Результат: {remoteConfig}\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ПОМИЛКА] {ex.Message}\n");
            }
        }
    }
}
