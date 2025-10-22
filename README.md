# StringAnalyzerService

A simple RESTful API built with ASP.NET Core (C#) that analyzes strings and returns computed properties like length, palindrome check, unique characters, word count, SHA-256 hash, and character frequency map.

🚀 How to Run Locally
git clone https://github.com/Abiola26/StringAnalyzerService.git
cd StringAnalyzerService
dotnet restore
dotnet run


API will run on:

HTTPS: https://localhost:7053

HTTP: http://localhost:5036

Open Swagger UI to test endpoints:
👉 https://localhost:7053/swagger

🧩 Endpoints
1️⃣ Create / Analyze String

POST /strings

{ "value": "Madam" }


✅ 201 Created → Returns analyzed properties
❌ 400 – Invalid request body or missing "value" field
❌ 409 – String already exists in the system
❌ 422 – Invalid data type for "value" (must be string)

2️⃣ Get Specific String

GET /strings/{string_value}
✅ 200 – Returns string record
❌ 404 – String does not exist in the system

3️⃣ Get All Strings (with Filters)

GET /strings?is_palindrome=true&min_length=5&max_length=20&word_count=1&contains_character=a
✅ 200 – Returns filtered data
❌ 400 – Invalid query parameter values or types

4️⃣ Filter by Natural Language

GET /strings/filter-by-natural-language?query=all%20single%20word%20palindromic%20strings
✅ 200 – Returns interpreted filters and results
❌ 400 - Unable to parse natural language query 
❌ 422 – Query parsed but resulted in conflicting filters

5️⃣ Delete String

DELETE /strings/{string_value}
✅ 204 – Deleted successfully
❌ 404 – String does not exist in the system

🧪 Quick Test Example (curl)
curl -X POST "https://localhost:7053/strings" ^
-H "Content-Type: application/json" ^
-d "{\"value\":\"Madam\"}" -k

👨🏽‍💻 Author

Name: Muheez
Stack: C# / ASP.NET Core
HNG Cohort: Backend Wizards
