using System;
using System.IO;
using System.Net.Http;
using System.Threading;

namespace lab12v15
{
    // Клас з методом для повторення операцій (Retry Pattern)
    public static class RetryHelper
    {
        public static T ExecuteWithRetry<T>(
            Func<T> operation,
            int retryCount,
            TimeSpan initialDelay,
            Func<Exception, bool> shouldRetry)
        {
            int attempt = 0;
            TimeSpan delay = initialDelay;

            while (true)
            {
                try
                {
                    attempt++;
                    return operation();
                }
                catch (Exception ex)
                {
                    // Якщо вичерпано спроби АБО помилка не підпадає під умову повторення - прокидаємо виняток далі
                    if (attempt > retryCount || !shouldRetry(ex))
                    {
                        throw; 
                    }

                    Console.WriteLine($"[Спроба {attempt}/{retryCount}] Сталася помилка: {ex.GetType().Name}. Повторна спроба через {delay.TotalMilliseconds} мс...");
                    Thread.Sleep(delay);
                    delay = delay * 2; // Експоненційне збільшення затримки
                }
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            
            // Політика: повторювати тільки для помилок вводу-виводу (IOException) або мережевих помилок (HttpRequestException)
            Func<Exception, bool> retryPolicy = ex => ex is IOException || ex is HttpRequestException;

            Console.WriteLine("=== 1. Успішне виконання після кількох тимчасових помилок ===");
            int counter1 = 0;
            try
            {
                var result1 = RetryHelper.ExecuteWithRetry(
                    operation: () =>
                    {
                        counter1++;
                        if (counter1 < 3)
                        {
                            throw new IOException("Імітація тимчасової помилки доступу до файлу.");
                        }
                        return "Файл успішно прочитано!";
                    },
                    retryCount: 3,
                    initialDelay: TimeSpan.FromMilliseconds(500),
                    shouldRetry: retryPolicy
                );
                Console.WriteLine($"Результат: {result1}\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Кінцева помилка: {ex.Message}\n");
            }

            Console.WriteLine("=== 2. Перевищення ліміту спроб (всі спроби невдалі) ===");
            try
            {
                RetryHelper.ExecuteWithRetry<bool>(
                    operation: () =>
                    {
                        throw new HttpRequestException("Імітація відсутності з'єднання з сервером.");
                    },
                    retryCount: 2,
                    initialDelay: TimeSpan.FromMilliseconds(500),
                    shouldRetry: retryPolicy
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ПЕРЕХОПЛЕНО] Операція скасована після всіх спроб. Остання помилка: {ex.Message}\n");
            }

            // ТОЙ САМИЙ ДОДАНИЙ СЦЕНАРІЙ ДЛЯ ВИКЛАДАЧА:
            Console.WriteLine("=== 3. Демонстрація критичної помилки (БЕЗ ПОВТОРЕНЬ) ===");
            try
            {
                RetryHelper.ExecuteWithRetry<bool>(
                    operation: () => 
                    {
                        // ArgumentException НЕ входить до retryPolicy, тому програма впаде одразу без повторень
                        throw new ArgumentException("Невірні вхідні дані! (Цю помилку не треба повторювати)");
                    },
                    retryCount: 3,
                    initialDelay: TimeSpan.FromMilliseconds(500),
                    shouldRetry: retryPolicy
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ПЕРЕХОПЛЕНО КРИТИЧНУ ПОМИЛКУ] {ex.GetType().Name}: {ex.Message}");
                Console.WriteLine("-> Операцію скасовано ОДРАЗУ без повторних спроб, оскільки тип помилки не дозволено в shouldRetry.\n");
            }
        }
    }
}