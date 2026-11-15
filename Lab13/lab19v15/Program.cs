using System;

namespace lab19v15
{
    // --- 1. Власний виняток (Варіант 15) ---
    public class MessageProcessingException : Exception
    {
        public string MessageId { get; }
        public string Details { get; }

        public MessageProcessingException(string messageId, string details) 
            : base($"Помилка обробки повідомлення [{messageId}]: {details}")
        {
            MessageId = messageId;
            Details = details;
        }
    }

    // --- 2. Узагальнений клас Result<T> ---
    public class Result<T>
    {
        public bool IsSuccess { get; }
        public T? Value { get; }
        public string? Error { get; }

        private Result(T? value, bool isSuccess, string? error)
        {
            Value = value;
            IsSuccess = isSuccess;
            Error = error;
        }

        public static Result<T> Success(T value)
        {
            return new Result<T>(value, true, null);
        }

        public static Result<T> Failure(string error)
        {
            return new Result<T>(default, false, error);
        }

        public static Result<T> Failure(Exception ex)
        {
            return new Result<T>(default, false, ex.Message);
        }
    }

    // --- 3. Клас Service (Варіант 15) ---
    public class MessageProcessor
    {
        // Метод, що викидає виняток у разі пошкодження повідомлення
        public void ProcessMessage(string message)
        {
            if (message.Contains("corrupted"))
            {
                throw new MessageProcessingException("MSG-999", "Повідомлення містить пошкоджені дані.");
            }
            Console.WriteLine($"[ProcessMessage] Повідомлення успішно оброблено: {message}");
        }

        // Метод, що повертає Result<T> для очікуваних сценаріїв невдачі
        public Result<bool> SendMessage(string message)
        {
            if (!message.Contains("@")) // Імітація перевірки на валідного одержувача
            {
                return Result<bool>.Failure("Одержувач невалідний (відсутній символ '@').");
            }
            
            Console.WriteLine($"[SendMessage] Повідомлення успішно надіслано: {message}");
            return Result<bool>.Success(true);
        }
    }

    // --- 4. Демонстрація роботи ---
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            var processor = new MessageProcessor();

            Console.WriteLine("=== Демонстрація обробки через Exception ===");
            try
            {
                // Успішний сценарій
                processor.ProcessMessage("Звичайний текст повідомлення.");
                
                // Сценарій з помилкою (викидає виняток)
                processor.ProcessMessage("Цей текст містить corrupted дані.");
            }
            catch (MessageProcessingException ex)
            {
                Console.WriteLine($"[ПЕРЕХОПЛЕНО ВИНЯТОК] ID: {ex.MessageId}. Деталі: {ex.Details}");
                Console.WriteLine($"[ПОВІДОМЛЕННЯ ВИНЯТКУ] {ex.Message}");
            }

            Console.WriteLine("\n=== Демонстрація обробки через патерн Result<T> ===");
            
            // Успішний сценарій
            var successResult = processor.SendMessage("user@example.com: Привіт!");
            if (successResult.IsSuccess)
            {
                Console.WriteLine($"[RESULT SUCCESS] Операція успішна. Значення: {successResult.Value}");
            }

            // Сценарій з очікуваною помилкою
            var failureResult = processor.SendMessage("invalid_user: Привіт!");
            if (!failureResult.IsSuccess)
            {
                Console.WriteLine($"[RESULT FAILURE] Операція не вдалася. Причина: {failureResult.Error}");
            }
        }
    }
}