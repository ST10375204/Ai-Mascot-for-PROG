using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace PROG7312.Services
{
    internal class GPTServices
    {
        private readonly HttpClient _client;

        public GPTServices()
        {
            _client = new HttpClient();
            _client.BaseAddress = new Uri("https://openrouter.ai/api/v1/");
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", Environment.GetEnvironmentVariable("OPENROUTER_API_KEY"));
        }

        /// <summary>
        /// High-level convenience: given a clicked FrameworkElement it will gather event info,
        /// assemble the prompt, call the model and return the assistant text.
        /// Call this from MainWindow instead of doing reflection there.
        /// </summary>
        public async Task<string> ExplainClickedElementAsync(FrameworkElement element)
        {
            if (element == null) return "(no element)";

            string elementName = string.IsNullOrEmpty(element.Name) ? "(unnamed)" : element.Name;
            string elementType = element.GetType().Name;
            string xamlSnippet = $"<{elementType} Name=\"{element.Name}\" />";

            var eventsInfo = GatherEventInfo(element);

            return await ExplainComponentAsync(elementType, elementName, xamlSnippet, eventsInfo);
        }

        /// <summary>
        /// Existing prompt builder: keep this if you want to call with pre-collected info.
        /// </summary>
        public async Task<string> ExplainComponentAsync(string elementType, string elementName, string xamlSnippet, List<string> eventsInfo)
        {
            string eventsDetails = eventsInfo != null && eventsInfo.Count > 0
                ? string.Join("\n\n", eventsInfo)
                : "(No event handlers detected; this component does not perform any actions)";

            string prompt = $"The user clicked on a {elementType} named '{elementName}'.\n\n" +
                            $"XAML:\n{xamlSnippet}\n\n" +
                            $"Event Handlers with code:\n{eventsDetails}\n\n" +
                            "Explain in simple terms what this component does and how it behaves.";

            return await GetResponseAsync(prompt);
        }

        #region Reflection + source scanning (moved here from MainWindow)

        /// <summary>
        /// Gather events + code for a FrameworkElement. Returns a list of readable strings.
        /// This tries CLR reflection first, then falls back to common XAML pattern handler names.
        /// </summary>
        private List<string> GatherEventInfo(FrameworkElement element)
        {
            var list = new List<string>();
            if (element == null) return list;

            // 1) Try CLR events via reflection
            try
            {
                var events = element.GetType().GetEvents(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (var ev in events)
                {
                    try
                    {
                        // Attempt to find backing field (works for many patterns; not guaranteed for routed events)
                        var field = element.GetType().GetField(ev.Name,
                            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

                        if (field != null)
                        {
                            var del = field.GetValue(element) as Delegate;
                            if (del != null)
                            {
                                foreach (var handler in del.GetInvocationList())
                                {
                                    string body = GetMethodBodyFromFile(handler.Method.Name) ??
                                                  $"(Method {handler.Method.Name} exists but no code found; probably updates UI)";
                                    list.Add($"{ev.Name} → {handler.Method.Name}\n{body}");
                                }
                            }
                        }
                    }
                    catch
                    {
                        // ignore per-event failures
                    }
                }
            }
            catch
            {
                // ignore reflection failures
            }

            // 2) XAML naming convention fallback
            if (element is Button btn && !string.IsNullOrEmpty(btn.Name))
                AddHandlerFromXamlStyle(list, btn.Name, "Click");
            else if (element is ComboBox cb && !string.IsNullOrEmpty(cb.Name))
                AddHandlerFromXamlStyle(list, cb.Name, "SelectionChanged");
            else if (element is TextBox tb && !string.IsNullOrEmpty(tb.Name))
                AddHandlerFromXamlStyle(list, tb.Name, "TextChanged");

            return list;
        }

        private void AddHandlerFromXamlStyle(List<string> list, string elementName, string eventName)
        {
            string handlerName = $"{elementName}_{eventName}";
            string body = GetMethodBodyFromFile(handlerName) ??
                          $"(Method {handlerName} exists but no code found; default action could be showing a message or updating a control)";
            list.Add($"{eventName} → {handlerName}\n{body}");
        }

        /// <summary>
        /// Search project .cs files for method text by name. Uses simple text scanning + brace matching.
        /// Returns the method signature + body or null if not found.
        /// </summary>
        private string GetMethodBodyFromFile(string methodName)
        {
            try
            {
                // locate project root by walking up until a .csproj exists
                string dir = AppDomain.CurrentDomain.BaseDirectory;
                DirectoryInfo di = new DirectoryInfo(dir);
                while (di != null && !di.GetFiles("*.csproj").Any())
                    di = di.Parent;

                if (di == null) return null;
                string projectRoot = di.FullName;

                // get .cs files excluding bin/obj
                var csFiles = Directory.GetFiles(projectRoot, "*.cs", SearchOption.AllDirectories)
                                       .Where(p => !p.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar) &&
                                                   !p.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar));

                foreach (var file in csFiles)
                {
                    string code = File.ReadAllText(file);

                    // quick prefilter
                    if (!code.Contains(methodName + "(", StringComparison.Ordinal)) continue;

                    // find candidate occurrences
                    int idx = 0;
                    while ((idx = code.IndexOf(methodName + "(", idx, StringComparison.Ordinal)) >= 0)
                    {
                        // Walk back to try to capture the signature start
                        int sigStart = idx;
                        int back = sigStart - 1;
                        while (back > 0 && code[back] != '\n' && code[back] != ';' && code[back] != '{' && code[back] != '}')
                            back--;
                        sigStart = Math.Max(0, back + 1);

                        // find matching ')' for parameters
                        int parenStart = code.IndexOf('(', idx);
                        if (parenStart < 0) { idx += methodName.Length; continue; }
                        int pos = parenStart + 1;
                        int parenDepth = 1;
                        while (pos < code.Length && parenDepth > 0)
                        {
                            if (code[pos] == '(') parenDepth++;
                            else if (code[pos] == ')') parenDepth--;
                            pos++;
                        }
                        if (parenDepth != 0) { idx += methodName.Length; continue; }

                        // find first '{' after params
                        int braceOpen = code.IndexOf('{', pos);
                        if (braceOpen < 0) { idx += methodName.Length; continue; }

                        // match braces to find method end
                        int bracePos = braceOpen + 1;
                        int depth = 1;
                        while (bracePos < code.Length && depth > 0)
                        {
                            if (code[bracePos] == '{') depth++;
                            else if (code[bracePos] == '}') depth--;
                            bracePos++;
                        }
                        if (depth != 0) { idx += methodName.Length; continue; }

                        int extractStart = sigStart;
                        int extractLength = bracePos - extractStart;
                        string methodText = code.Substring(extractStart, extractLength).Trim();

                        // collapse multiple blank lines for readability
                        methodText = Regex.Replace(methodText, @"\r\n\s*\r\n", "\r\n\r\n");

                        return methodText;
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region LLM call

        private async Task<string> GetResponseAsync(string prompt)
        {
            var payload = new
            {
                model = "moonshotai/kimi-k2:free",
                messages = new[]
                {
                    new {
                        role = "system",
                        content = "You are Oom Vrikkie, a stereotypical Afrikaner/boer uncle acting as a friendly guide in a municipal app. " +
                        "Speak mostly in simple English, but sprinkle in a little Zulu, Afrikaans, and South African slang for flavour — not " +
                        "too much, just enough to feel local. Explain app features at a high level in plain, everyday terms. Keep responses short," +
                        " casual, and helpful, like you’re talking to a neighbour. Avoid overexplaining, avoid switching fully into another " +
                        "language, and do not use formatting like bold text, quotation marks, or lists. Just give natural plain text responses." +
                        "\r\n\r\nFor example, if the user clicks the Report button, you might say: Ja my bru, this button is for making a report" +
                        " about things like potholes or water leaks. Nice and easy."
                    },
                    new { role = "user", content = prompt }
                }
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await _client.PostAsync("chat/completions", content);
                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    return "System is busy, try again later.";

                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(result);
                if (!doc.RootElement.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
                    return $"Error: No choices returned. Raw: {result}";

                var message = choices[0].GetProperty("message");
                if (!message.TryGetProperty("content", out var contentProp))
                    return $"Error: No content returned. Raw: {result}";

                return contentProp.GetString();
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        #endregion
    }
}
