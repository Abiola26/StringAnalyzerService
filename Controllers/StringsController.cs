using Microsoft.AspNetCore.Mvc;
using StringAnalyzerService.Models.Requests;
using StringAnalyzerService.Services;
using System.Text;
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

        // inside StringsController
        [HttpPost]
        public async Task<IActionResult> CreateString()
        {
            // Try binding the usual way first
            StringRequestModel? requestModel = null;
            try
            {
                // If model binding worked, MVC will populate Request.Body already;
                // we still try to read it safely if not.
                requestModel = await HttpContext.Request.ReadFromJsonAsync<StringRequestModel>();
            }
            catch
            {
                // ignore parse errors - we'll attempt fallback below
            }

            // Fallback: if requestModel is null attempt to parse raw body manually
            if (requestModel == null)
            {
                Request.EnableBuffering();
                using var sr = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
                var bodyText = await sr.ReadToEndAsync();
                Request.Body.Position = 0;

                if (string.IsNullOrWhiteSpace(bodyText))
                    return BadRequest(new { message = "Missing 'value' field" });

                try
                {
                    // parse as JSON and look for "value"
                    var doc = System.Text.Json.JsonDocument.Parse(bodyText);
                    if (!doc.RootElement.TryGetProperty("value", out var valElem))
                        return BadRequest(new { message = "Missing 'value' field" });

                    if (valElem.ValueKind != System.Text.Json.JsonValueKind.String)
                        return UnprocessableEntity(new { message = "'value' must be a string" });

                    requestModel = new StringRequestModel { Value = valElem.GetString()! };
                }
                catch (System.Text.Json.JsonException)
                {
                    return BadRequest(new { message = "Invalid request body" });
                }
            }

            // Now validate
            if (requestModel == null)
                return BadRequest(new { message = "Invalid request body" });

            if (requestModel.Value == null)
                return BadRequest(new { message = "Missing 'value' field" });

            // ensure the JSON type was string (defensive)
            // (If MVC binding gave us a string property already, this check is passed)
            if (requestModel.Value is not string)
                return UnprocessableEntity(new { message = "'value' must be a string" });

            if (string.IsNullOrWhiteSpace(requestModel.Value))
                return BadRequest(new { message = "'value' cannot be empty" });

            try
            {
                var record = _service.AnalyzeString(requestModel.Value);

                // Return 201 Created with location header pointing to GET route
                var location = $"/strings/{Uri.EscapeDataString(requestModel.Value)}";
                return Created(location, record);
            }
            catch (InvalidOperationException) // duplicate
            {
                return Conflict(new { message = "String already exists" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception)
            {
                // Generic fallback
                return BadRequest(new { message = "Invalid request body" });
            }
        }


        // ✅ 2️⃣ GET /strings/{string_value}
        [HttpGet("{string_value}")]
        public IActionResult GetString(string string_value)
        {
            var record = _service.GetString(string_value);
            if (record == null)
                return NotFound(new { message = "String does not exist in the system" });

            return Ok(record);
        }

        // ✅ 3️⃣ GET /strings (filters)
        [HttpGet]
        public IActionResult GetAllStrings(
            [FromQuery] bool? is_palindrome,
            [FromQuery] int? min_length,
            [FromQuery] int? max_length,
            [FromQuery] int? word_count,
            [FromQuery] string contains_character)
        {
            if (!string.IsNullOrEmpty(contains_character) && contains_character.Length != 1)
                return BadRequest(new { message = "contains_character must be a single character" });

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
                results = results.Where(r => r.Value.Contains(contains_character, StringComparison.OrdinalIgnoreCase));

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

        // ✅ 4️⃣ GET /strings/filter-by-natural-language
        [HttpGet("filter-by-natural-language")]
        public IActionResult FilterByNaturalLanguage([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest(new { message = "Query parameter 'query' is required" });

            var parsed = new Dictionary<string, object>();

            if (Regex.IsMatch(query, @"\bsingle\s+word\b", RegexOptions.IgnoreCase))
                parsed["word_count"] = 1;
            if (Regex.IsMatch(query, @"palindrom", RegexOptions.IgnoreCase))
                parsed["is_palindrome"] = true;
            var longerMatch = Regex.Match(query, @"longer\s+than\s+(\d+)", RegexOptions.IgnoreCase);
            if (longerMatch.Success)
                parsed["min_length"] = int.Parse(longerMatch.Groups[1].Value) + 1;
            var containsMatch = Regex.Match(query, @"containing\s+(?:the\s+letter\s+)?([a-zA-Z])", RegexOptions.IgnoreCase);
            if (containsMatch.Success)
                parsed["contains_character"] = containsMatch.Groups[1].Value.ToLower();

            if (Regex.IsMatch(query, @"first\s+vowel", RegexOptions.IgnoreCase))
                parsed["contains_character"] = "a";

            if (!parsed.Any())
                return BadRequest(new { message = "Unable to parse natural language query" });

            var results = _service.GetAll();

            if (parsed.ContainsKey("is_palindrome"))
                results = results.Where(r => r.Properties.IsPalindrome);
            if (parsed.ContainsKey("min_length"))
                results = results.Where(r => r.Properties.Length >= Convert.ToInt32(parsed["min_length"]));
            if (parsed.ContainsKey("word_count"))
                results = results.Where(r => r.Properties.WordCount == Convert.ToInt32(parsed["word_count"]));
            if (parsed.ContainsKey("contains_character"))
            {
                string c = parsed["contains_character"].ToString();
                results = results.Where(r => r.Value.Contains(c, StringComparison.OrdinalIgnoreCase));
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

        // ✅ 5️⃣ DELETE /strings/{string_value}
        [HttpDelete("{string_value}")]
        public IActionResult DeleteString(string string_value)
        {
            bool deleted = _service.DeleteString(string_value);
            if (!deleted)
                return NotFound(new { message = "String does not exist in the system" });

            return NoContent();
        }
    }
}
