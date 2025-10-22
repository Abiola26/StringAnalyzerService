using StringAnalyzerService.Entity;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace StringAnalyzerService.Services
{
    public class StringAnalyzerService
    {
        private readonly ConcurrentDictionary<string, StringRecord> _storage = new();

        public StringRecord AnalyzeString(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Value cannot be null or empty.");

            string hash = ComputeSha256Hash(value);

            if (_storage.ContainsKey(hash))
                throw new InvalidOperationException("String already exists.");

            var record = new StringRecord
            {
                Id = hash,
                Value = value,
                Properties = ComputeProperties(value, hash),
                CreatedAt = DateTime.UtcNow
            };

            _storage.TryAdd(hash, record);
            return record;
        }

        public StringRecord GetString(string value)
        {
            string hash = ComputeSha256Hash(value);
            return _storage.TryGetValue(hash, out var record) ? record : null;
        }

        public IEnumerable<StringRecord> GetAll() => _storage.Values;

        public bool DeleteString(string value)
        {
            string hash = ComputeSha256Hash(value);
            return _storage.TryRemove(hash, out _);
        }

        private static StringProperties ComputeProperties(string value, string hash)
        {
            return new StringProperties
            {
                Length = value.Length,
                IsPalindrome = IsPalindrome(value),
                UniqueCharacters = value.ToLower().Distinct().Count(),
                WordCount = value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length,
                Sha256Hash = hash,
                CharacterFrequencyMap = value
                    .ToLower()
                    .Where(c => !char.IsWhiteSpace(c))
                    .GroupBy(c => c)
                    .ToDictionary(g => g.Key, g => g.Count())
            };
        }

        private static bool IsPalindrome(string input)
        {
            var cleaned = new string(input.ToLower().Where(char.IsLetterOrDigit).ToArray());
            return cleaned.SequenceEqual(cleaned.Reverse());
        }

        private static string ComputeSha256Hash(string rawData)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        }
    }
}
