using Microsoft.AspNetCore.Mvc;
using StringAnalyzerService.Models.Requests;
using StringAnalyzerService.Services;
using System.Text.RegularExpressions;

namespace StringAnalyzerService.Controllers
{
    [ApiController]
    [Route("strings")]
    public class StringsController : ControllerBase
    {
        private readonly StringAnalyzerService.Services.StringAnalyzerService _service;

        public StringsController(StringAnalyzerService.Services.StringAnalyzerService service)
        {
            _service = service;
        }

        // 1️⃣ POST /strings
        [HttpPost]
        public IActionResult CreateString([FromBody] StringRequestModel request)
        {
            if (request == null || request.Value == null)
                return BadRequest(new { message = "Missing 'value' field" });

            if (request.Value is not string)
                return UnprocessableEntity(new { message = "'value' must be a string" });

            if (string.IsNullOrWhiteSpace(request.Value))
                return BadRequest(new { message = "'value' cannot be empty" });

            try
            {
                var record = _service.AnalyzeString(request.Value);
                return CreatedAtAction(nameof(GetString), new { string_value = request.Value }, record);
            }
            catch (InvalidOperationException)
            {
                return Conflict(new { message = "String already exists" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // 2️⃣ GET /strings/{string_value}
        [HttpGet("{string_value}")]
        public IActionResult GetString(string string_value)
        {
            var record = _service.GetString(string_value);
            if (record == null)
                return NotFound(new { message = "String does not exist in the system" });

            return Ok(record);
        }

        // 3️⃣ DELETE /strings/{string_value}
        [HttpDelete("{string_value}")]
        public IActionResult DeleteString(string string_value)
        {
            bool deleted = _service.DeleteString(string_value);
            if (!deleted)
                return NotFound(new { message = "String does not exist in the system" });

            return NoContent();
        }

        // 4️⃣ GET /strings (with filters)
        [HttpGet]
        public IActionResult GetAllStrings(
            [FromQuery] bool? is_palindrome,
            [FromQuery] int? min_length,
            [FromQuery] int? max_length,
            [FromQuery] int? word_count,
            [FromQuery] string contains_character)
        {
            // --- Validation ---
            if (!string.IsNullOrEmpty(contains_character) && contains_character.Length != 1)
                return BadRequest(new { message = "Invalid query parameter values or types" });

            if (min_length.HasValue && min_length < 0)
                return BadRequest(new { message = "'min_length' must be non-negative" });

            if (max_length.HasValue && max_length < 0)
                return BadRequest(new { message = "'max_length' must be non-negative" });

            if (min_length.HasValue && max_length.HasValue && min_length > max_length)
                return BadRequest(new { message = "'min_length' cannot be greater than 'max_length'" });

            if (word_count.HasValue && word_count < 0)
                return BadRequest(new { message = "'word_count' must be non-negative" });

            // --- Filter data ---
            var results = _service.GetAll();

            if (is_palindrome.HasValue)
                results = results.Where(r => r.Properties.IsPalindrome == is_palindrome.Value);

            if (min_length.HasValue)
                results = results.Where(r => r.Properties.Length >= min_length.Value);

            if (max_length.HasValue)
                results = results.Where(r => r.Properties.Length <= max_length.Value);

            if (word_count.HasValue)
                results = results.Where(r => r.Properties.WordCount == word_count.Value);

            if (!string.IsNullOrEmpty(contains_character))
            {
                char c = contains_character[0];
                results = results.Where(r => r.Value.IndexOf(c, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            var list = results.ToList();

            return Ok(new
            {
                data = list,
                count = list.Count,
                filters_applied = new
                {
                    is_palindrome,
                    min_length,
                    max_length,
                    word_count,
                    contains_character
                }
            });
        }

        // 5️⃣ GET /strings/filter-by-natural-language?query=...
        [HttpGet("filter-by-natural-language")]
        public IActionResult FilterByNaturalLanguage([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest(new { message = "Query parameter 'query' is required" });

            var parsed = new Dictionary<string, object>();

            // --- Simple heuristic parsing ---
            if (Regex.IsMatch(query, @"\bsingle\s+word\b", RegexOptions.IgnoreCase))
                parsed["word_count"] = 1;

            if (Regex.IsMatch(query, @"palindrom", RegexOptions.IgnoreCase))
                parsed["is_palindrome"] = true;

            var longerMatch = Regex.Match(query, @"longer\s+than\s+(\d+)", RegexOptions.IgnoreCase);
            if (longerMatch.Success && int.TryParse(longerMatch.Groups[1].Value, out var longerN))
                parsed["min_length"] = longerN + 1;

            var containsLetterMatch = Regex.Match(query, @"(?:containing|contains|contain)\s+(?:the\s+letter\s+)?([a-zA-Z])\b", RegexOptions.IgnoreCase);
            if (containsLetterMatch.Success)
                parsed["contains_character"] = containsLetterMatch.Groups[1].Value.ToLower();

            if (Regex.IsMatch(query, @"first\s+vowel", RegexOptions.IgnoreCase) && !parsed.ContainsKey("contains_character"))
                parsed["contains_character"] = "a";

            if (parsed.Count == 0)
                return BadRequest(new { message = "Unable to parse natural language query" });

            // --- Conflict validation ---
            if (parsed.ContainsKey("min_length") && parsed.ContainsKey("max_length"))
            {
                if (Convert.ToInt32(parsed["min_length"]) > Convert.ToInt32(parsed["max_length"]))
                    return UnprocessableEntity(new { message = "Query parsed but resulted in conflicting filters" });
            }

            // --- Apply filters ---
            bool? isPalindrome = parsed.ContainsKey("is_palindrome") ? (bool?)Convert.ToBoolean(parsed["is_palindrome"]) : null;
            int? minLength = parsed.ContainsKey("min_length") ? (int?)Convert.ToInt32(parsed["min_length"]) : null;
            int? wordCount = parsed.ContainsKey("word_count") ? (int?)Convert.ToInt32(parsed["word_count"]) : null;
            string containsChar = parsed.ContainsKey("contains_character") ? parsed["contains_character"].ToString() : null;

            var results = _service.GetAll();

            if (isPalindrome.HasValue)
                results = results.Where(r => r.Properties.IsPalindrome == isPalindrome.Value);

            if (minLength.HasValue)
                results = results.Where(r => r.Properties.Length >= minLength.Value);

            if (wordCount.HasValue)
                results = results.Where(r => r.Properties.WordCount == wordCount.Value);

            if (!string.IsNullOrEmpty(containsChar))
            {
                char c = containsChar[0];
                results = results.Where(r => r.Value.IndexOf(c, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            var list = results.ToList();

            return Ok(new
            {
                data = list,
                count = list.Count,
                interpreted_query = new
                {
                    original = query,
                    parsed_filters = parsed
                }
            });
        }
    }
}
