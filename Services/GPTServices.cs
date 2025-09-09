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
using System.Windows.Media;
using System.Diagnostics;
using System.Windows.Media.Media3D;

namespace PROG7312.Services
{
    internal class GPTServices
    {
        private readonly HttpClient _client;

        public GPTServices()
        {
            _client = new HttpClient
            {
                BaseAddress = new Uri("https://openrouter.ai/api/v1/")
            };
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", Environment.GetEnvironmentVariable("OPENROUTER_API_KEY"));
        }

        public async Task<string> ExplainClickedAsync(object originalSource)
        {
            var element = ResolveControl(originalSource);
            if (element == null)
                return "(No valid element found)";

            return await ExplainClickedElementAsync(element);
        }
        public FrameworkElement ResolveControl(object source)
        {
            if (source == null) return null;

            var current = source as DependencyObject;

            while (current != null)
            {
                // Return only meaningful interactive controls
                if (current is Button || current is TextBox || current is ComboBox || current is ListBox || current is RichTextBox)
                    return current as FrameworkElement;

                // Handle FlowDocument text content
                if (current is System.Windows.Documents.TextElement textElement)
                {
                    current = textElement.Parent as DependencyObject;
                    continue;
                }

                // Handle FlowDocument itself 
                if (current is System.Windows.Documents.FlowDocument doc)
                {
                    var parent = LogicalTreeHelper.GetParent(doc);
                    if (parent is FrameworkElement feParent)
                        return feParent;

                    // if no FrameworkElement parent found, bail out
                    return null;
                }

                // Safe guard: only call VisualTreeHelper if it's a Visual/Visual3D
                if (current is Visual || current is Visual3D)
                {
                    current = VisualTreeHelper.GetParent(current);
                }
                else
                {
                    // If it's neither a Visual nor FlowDocument/TextElement, stop climbing
                    current = LogicalTreeHelper.GetParent(current);
                }
            }

            return null;
        }

        public async Task<string> ExplainClickedElementAsync(FrameworkElement element)
        {
            if (element == null) return "(no element)";

            string elementName = string.IsNullOrEmpty(element.Name) ? "(unnamed)" : element.Name;
            string elementType = element.GetType().Name;
            string xamlSnippet = $"<{elementType} Name=\"{element.Name}\" />";

            // collect visual hints (Content/ToolTip/Tag) so assistant has UI context
            var visualParts = new List<string>();
            if (element is Button btn)
            {
                if (btn.Content != null) visualParts.Add($"Content: '{btn.Content}'");
                if (btn.ToolTip != null) visualParts.Add($"ToolTip: '{btn.ToolTip}'");
                if (!string.IsNullOrEmpty(btn.Tag?.ToString())) visualParts.Add($"Tag: '{btn.Tag}'");
            }
            else
            {
                var tt = element.GetValue(Control.ToolTipProperty);
                if (tt != null) visualParts.Add($"ToolTip: '{tt}'");
                var tag = element.GetValue(FrameworkElement.TagProperty);
                if (tag != null) visualParts.Add($"Tag: '{tag}'");
            }
            string visualDetails = visualParts.Count > 0 ? string.Join("; ", visualParts) : "(no visual hints)";

            var eventsInfo = GatherEventInfo(element);

            return await ExplainComponentAsync(elementType, elementName, xamlSnippet, eventsInfo, visualDetails);
        }

        private async Task<string> ExplainComponentAsync(
            string elementType,
            string elementName,
            string xamlSnippet,
            List<string> eventsInfo,
            string visualDetails = null)
        {
            string eventsDetails = eventsInfo != null && eventsInfo.Count > 0
                ? string.Join("\n\n", eventsInfo)
                : "(No event handlers detected; this component does not perform any actions)";

            visualDetails ??= "(no visual hints)";

            string prompt = $"The user clicked on a {elementType} named '{elementName}'.\n\n" +
                            $"Visual: {visualDetails}\n\n" +
                            $"XAML:\n{xamlSnippet}\n\n" +
                            $"Event Handlers with code:\n{eventsDetails}\n\n" +
                            "Explain in simple terms what this component is intended to do " +
                            "";

            // DEBUG
            Debug.WriteLine("----- GPT PROMPT BEGIN -----");
            Debug.WriteLine(prompt);
            Debug.WriteLine("----- GPT PROMPT END -----");

            return await GetResponseAsync(prompt);
        }

        private List<string> GatherEventInfo(FrameworkElement element)
        {
            var list = new List<string>();
            if (element == null) return list;

            try
            {
                var events = element.GetType().GetEvents(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (var ev in events)
                {
                    try
                    {
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
                    catch { /* ignore single-event failures */ }
                }
            }
            catch { /* ignore reflection failures */ }

            // fallback XAML-style handler names (but only add informative message if the method isn't found)
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
            string body = GetMethodBodyFromFile(handlerName);
            if (body != null)
                list.Add($"{eventName} → {handlerName}\n{body}");
            else
                list.Add($"{eventName} → {handlerName}\n(Method declared in XAML but no matching method found in code-behind)");
        }

        private string GetMethodBodyFromFile(string methodName)
        {
            try
            {
                string dir = AppDomain.CurrentDomain.BaseDirectory;
                DirectoryInfo di = new DirectoryInfo(dir);
                while (di != null && !di.GetFiles("*.csproj").Any())
                    di = di.Parent;

                if (di == null) return null;
                string projectRoot = di.FullName;

                var csFiles = Directory.GetFiles(projectRoot, "*.cs", SearchOption.AllDirectories)
                                       .Where(p => !p.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar) &&
                                                   !p.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar));

                foreach (var file in csFiles)
                {
                    string code = File.ReadAllText(file);

                    if (!code.Contains(methodName + "(", StringComparison.Ordinal)) continue;

                    int idx = 0;
                    while ((idx = code.IndexOf(methodName + "(", idx, StringComparison.Ordinal)) >= 0)
                    {
                        int sigStart = idx;
                        int back = sigStart - 1;
                        while (back > 0 && code[back] != '\n' && code[back] != ';' && code[back] != '{' && code[back] != '}')
                            back--;
                        sigStart = Math.Max(0, back + 1);

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

                        int braceOpen = code.IndexOf('{', pos);
                        if (braceOpen < 0) { idx += methodName.Length; continue; }

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

                        // collapse multiple blank lines
                        methodText = Regex.Replace(methodText, @"\r\n\s*\r\n", "\r\n\r\n");

                        // detect empty body (only whitespace/comments)
                        int firstBrace = methodText.IndexOf('{');
                        int lastBrace = methodText.LastIndexOf('}');
                        if (firstBrace >= 0 && lastBrace > firstBrace)
                        {
                            string inner = methodText.Substring(firstBrace + 1, lastBrace - firstBrace - 1).Trim();
                            string innerNoComments = Regex.Replace(inner, @"//.*?$|/\*.*?\*/", "", RegexOptions.Singleline | RegexOptions.Multiline).Trim();
                            if (string.IsNullOrEmpty(innerNoComments))
                            {
                                return $"(Method {methodName} found but body appears empty — no behavior implemented)";
                            }
                        }

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


        private async Task<string> GetResponseAsync(string prompt)
        {
            var payload = new
            {
                model = "moonshotai/kimi-k2:free",
                messages = new[]
                {
                    new {
                        role = "system",
                        content = "You are Oom Vrikkie, a stereotypical Afrikaner uncle acting as a friendly guide in a municipal app. " +
                                  "Speak mostly in simple English, but sprinkle in a little Zulu, Afrikaans, and South African slang for flavour. " +
                                  "Keep responses short, casual, and neighbourly. Avoid switching fully into another language or overexplaining." +
                                  "The user does not need to know technical details, just what the systems intended purpose" +
                                  "Never give the user the names of the components or methods, only what they need to know to understand the function of the component"
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
    }
}
