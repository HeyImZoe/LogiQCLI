using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LogiQCLI.Tools.Core.Objects;
using LogiQCLI.Tools.ContentManipulation.Objects;
using LogiQCLI.Tools.Core.Interfaces;

namespace LogiQCLI.Tools.ContentManipulation
{
    [ToolMetadata("ContentManipulation", Tags = new[] { "write" })]
    public class SearchAndReplaceTool : ITool
    {
        public override RegisteredTool GetToolInfo()
        {
            return new RegisteredTool
            {
                Name = "search_and_replace",
                Description = "Performs global find-and-replace operations throughout an entire file. " +
                              "Use this tool for simple text substitutions, updating variable names, " +
                              "changing URLs/paths, or pattern-based replacements across a file. " +
                              "Unlike apply_diff, this replaces ALL occurrences and is ideal for repetitive changes. " +
                              "Automatically creates .bak backup files by default.",
                Parameters = new Parameters
                {
                    Type = "object",
                    Properties = new
                    {
                        path = new
                        {
                            type = "string",
                            description = "File path relative to workspace root. " +
                                         "File must exist. " +
                                         "Example: 'src/config.js', 'README.md'"
                        },
                        search = new
                        {
                            type = "string",
                            description = "Text or regex pattern to find. " +
                                         "Plain text example: 'oldVariable'. " +
                                         "Regex example: '\\bversion\\s*=\\s*[\"\\']([^\"\\']+)[\"\\']'"
                        },
                        replace = new
                        {
                            type = "string",
                            description = "Replacement text. " +
                                         "Can use regex capture groups like $1 when useRegex=true. " +
                                         "Use empty string to delete all occurrences."
                        },
                        useRegex = new
                        {
                            type = "boolean",
                            description = "Treat search pattern as regular expression. " +
                                         "Default: false. Enables capture groups and pattern matching."
                        },
                        caseSensitive = new
                        {
                            type = "boolean",
                            description = "Match case exactly when searching. " +
                                         "Default: true. Set false for case-insensitive replacements."
                        },
                        multiline = new
                        {
                            type = "boolean",
                            description = "In regex mode: ^ and $ match line boundaries. " +
                                         "Default: true. Only applies when useRegex=true."
                        },
                        backup = new
                        {
                            type = "boolean",
                            description = "Create .bak backup before modifying file. " +
                                         "Default: true. Backup created only if changes are made."
                        }
                    },
                    Required = new[] { "path", "search", "replace" }
                }
            };
        }

        public override async Task<string> Execute(string args)
        {
            try
            {
                var arguments = JsonSerializer.Deserialize<SearchAndReplaceArguments>(args);
                if (arguments == null || string.IsNullOrEmpty(arguments.Path) || arguments.Search == null)
                {
                    return "Error: Invalid arguments. Path and search are required.";
                }

                var fullPath = Path.GetFullPath(arguments.Path.Replace('/', Path.DirectorySeparatorChar)
                    .Replace('\\', Path.DirectorySeparatorChar));

                var content = await File.ReadAllTextAsync(fullPath);
                var originalContent = content;
                
                var useRegex = arguments.UseRegex ?? false;
                var caseSensitive = arguments.CaseSensitive ?? true;
                var multiline = arguments.Multiline ?? true;
                var backup = arguments.Backup ?? true;
                var replaceText = arguments.Replace ?? string.Empty;

                int replacementCount;
                string newContent;

                if (useRegex)
                {
                    var options = RegexOptions.None;
                    if (!caseSensitive) options |= RegexOptions.IgnoreCase;
                    if (multiline) options |= RegexOptions.Multiline;

                    var regex = new Regex(arguments.Search, options);
                    var matches = regex.Matches(content);
                    replacementCount = matches.Count;
                    newContent = regex.Replace(content, replaceText);
                }
                else
                {
                    var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                    replacementCount = CountOccurrences(content, arguments.Search, comparison);
                    newContent = ReplaceAll(content, arguments.Search, replaceText, comparison);
                }

                if (replacementCount == 0)
                {
                    return $"No matches found for '{arguments.Search}' in {fullPath}";
                }

                if (backup && originalContent != newContent)
                {
                    var backupPath = fullPath + ".bak";
                    await File.WriteAllTextAsync(backupPath, originalContent);
                }

                await File.WriteAllTextAsync(fullPath, newContent);

                return $"Successfully replaced {replacementCount} occurrence(s) in {fullPath}";
            }
            catch (Exception ex)
            {
                return $"Error during search and replace: {ex.Message}";
            }
        }

        private int CountOccurrences(string text, string pattern, StringComparison comparison)
        {
            int count = 0;
            int index = 0;
            while ((index = text.IndexOf(pattern, index, comparison)) != -1)
            {
                count++;
                index += pattern.Length;
            }
            return count;
        }

        private string ReplaceAll(string text, string oldValue, string newValue, StringComparison comparison)
        {
            if (comparison == StringComparison.Ordinal)
            {
                return text.Replace(oldValue, newValue);
            }

            var result = text;
            int index = 0;
            while ((index = result.IndexOf(oldValue, index, comparison)) != -1)
            {
                result = result.Remove(index, oldValue.Length).Insert(index, newValue);
                index += newValue.Length;
            }
            return result;
        }

    }
}
