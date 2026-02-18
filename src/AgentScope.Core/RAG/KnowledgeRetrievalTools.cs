// Copyright 2024-2026 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AgentScope.Core.Tool;

namespace AgentScope.Core.RAG;

/// <summary>
/// Tool for searching knowledge base.
/// 知识库搜索工具
/// </summary>
public class KnowledgeSearchTool : ToolBase
{
    private readonly IKnowledge _knowledge;
    private readonly int _defaultTopK;

    public KnowledgeSearchTool(IKnowledge knowledge, int defaultTopK = 5) 
        : base("knowledge_search", "Search the knowledge base for relevant information")
    {
        _knowledge = knowledge ?? throw new ArgumentNullException(nameof(knowledge));
        _defaultTopK = defaultTopK;
    }

    public override Dictionary<string, object> GetSchema()
    {
        return new Dictionary<string, object>
        {
            ["type"] = "function",
            ["function"] = new Dictionary<string, object>
            {
                ["name"] = Name,
                ["description"] = Description,
                ["parameters"] = new Dictionary<string, object>
                {
                    ["type"] = "object",
                    ["properties"] = new Dictionary<string, object>
                    {
                        ["query"] = new Dictionary<string, string> 
                        { 
                            ["type"] = "string", 
                            ["description"] = "The search query" 
                        },
                        ["top_k"] = new Dictionary<string, string> 
                        { 
                            ["type"] = "integer", 
                            ["description"] = "Number of results to return (default: 5)" 
                        },
                        ["filter"] = new Dictionary<string, string> 
                        { 
                            ["type"] = "object", 
                            ["description"] = "Optional metadata filters" 
                        }
                    },
                    ["required"] = new List<string> { "query" }
                }
            }
        };
    }

    public override async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        try
        {
            var query = parameters["query"].ToString() ?? "";
            
            var options = new KnowledgeSearchOptions
            {
                TopK = parameters.TryGetValue("top_k", out var topK) ? Convert.ToInt32(topK) : _defaultTopK
            };

            if (parameters.TryGetValue("filter", out var filter) && filter is Dictionary<string, object> filters)
            {
                options.Filters = filters;
            }

            var results = await _knowledge.SearchAsync(query, options);

            if (results.Count == 0)
            {
                return ToolResult.Ok("No relevant information found in the knowledge base.");
            }

            var formatted = FormatResults(results);
            return ToolResult.Ok(formatted);
        }
        catch (System.Exception ex)
        {
            return ToolResult.Fail($"Search failed: {ex.Message}");
        }
    }

    private static string FormatResults(IReadOnlyList<KnowledgeSearchResult> results)
    {
        var lines = new List<string> { "Search Results:" };
        
        foreach (var result in results)
        {
            lines.Add($"\n[{result.Rank}] Score: {result.Score:F3}");
            if (!string.IsNullOrEmpty(result.Document.Title))
            {
                lines.Add($"Title: {result.Document.Title}");
            }
            if (!string.IsNullOrEmpty(result.Document.Source))
            {
                lines.Add($"Source: {result.Document.Source}");
            }
            lines.Add($"Content: {result.Document.Content}");
        }

        return string.Join("\n", lines);
    }
}

/// <summary>
/// Tool for getting a specific document by ID.
/// 文档获取工具
/// </summary>
public class KnowledgeGetDocumentTool : ToolBase
{
    private readonly IKnowledge _knowledge;

    public KnowledgeGetDocumentTool(IKnowledge knowledge) 
        : base("knowledge_get_document", "Get a specific document from the knowledge base by ID")
    {
        _knowledge = knowledge ?? throw new ArgumentNullException(nameof(knowledge));
    }

    public override Dictionary<string, object> GetSchema()
    {
        return new Dictionary<string, object>
        {
            ["type"] = "function",
            ["function"] = new Dictionary<string, object>
            {
                ["name"] = Name,
                ["description"] = Description,
                ["parameters"] = new Dictionary<string, object>
                {
                    ["type"] = "object",
                    ["properties"] = new Dictionary<string, object>
                    {
                        ["document_id"] = new Dictionary<string, string> 
                        { 
                            ["type"] = "string", 
                            ["description"] = "The document ID" 
                        }
                    },
                    ["required"] = new List<string> { "document_id" }
                }
            }
        };
    }

    public override async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        try
        {
            var documentId = parameters["document_id"].ToString() ?? "";
            var document = await _knowledge.GetDocumentAsync(documentId);

            if (document == null)
            {
                return ToolResult.Fail($"Document '{documentId}' not found");
            }

            var result = new Dictionary<string, object>
            {
                ["id"] = document.Id,
                ["title"] = document.Title ?? "",
                ["content"] = document.Content,
                ["source"] = document.Source ?? "",
                ["created_at"] = document.CreatedAt,
                ["metadata"] = document.Metadata
            };

            return ToolResult.Ok(result);
        }
        catch (System.Exception ex)
        {
            return ToolResult.Fail($"Failed to get document: {ex.Message}");
        }
    }
}

/// <summary>
/// Tool for adding documents to knowledge base.
/// 文档添加工具
/// </summary>
public class KnowledgeAddDocumentTool : ToolBase
{
    private readonly IKnowledge _knowledge;

    public KnowledgeAddDocumentTool(IKnowledge knowledge) 
        : base("knowledge_add_document", "Add a document to the knowledge base")
    {
        _knowledge = knowledge ?? throw new ArgumentNullException(nameof(knowledge));
    }

    public override Dictionary<string, object> GetSchema()
    {
        return new Dictionary<string, object>
        {
            ["type"] = "function",
            ["function"] = new Dictionary<string, object>
            {
                ["name"] = Name,
                ["description"] = Description,
                ["parameters"] = new Dictionary<string, object>
                {
                    ["type"] = "object",
                    ["properties"] = new Dictionary<string, object>
                    {
                        ["content"] = new Dictionary<string, string> 
                        { 
                            ["type"] = "string", 
                            ["description"] = "The document content" 
                        },
                        ["title"] = new Dictionary<string, string> 
                        { 
                            ["type"] = "string", 
                            ["description"] = "The document title (optional)" 
                        },
                        ["source"] = new Dictionary<string, string> 
                        { 
                            ["type"] = "string", 
                            ["description"] = "The document source/URL (optional)" 
                        },
                        ["metadata"] = new Dictionary<string, string> 
                        { 
                            ["type"] = "object", 
                            ["description"] = "Additional metadata (optional)" 
                        }
                    },
                    ["required"] = new List<string> { "content" }
                }
            }
        };
    }

    public override async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        try
        {
            var document = new KnowledgeDocument
            {
                Content = parameters["content"].ToString() ?? "",
                Title = parameters.TryGetValue("title", out var title) ? title.ToString() : null,
                Source = parameters.TryGetValue("source", out var source) ? source.ToString() : null
            };

            if (parameters.TryGetValue("metadata", out var metadata) && metadata is Dictionary<string, object> meta)
            {
                document.Metadata = meta;
            }

            var id = await _knowledge.AddDocumentAsync(document);

            return ToolResult.Ok(new Dictionary<string, object>
            {
                ["document_id"] = id,
                ["message"] = "Document added successfully"
            });
        }
        catch (System.Exception ex)
        {
            return ToolResult.Fail($"Failed to add document: {ex.Message}");
        }
    }
}

/// <summary>
/// Factory for creating RAG-related tools.
/// RAG 工具工厂
/// </summary>
public static class RAGTools
{
    /// <summary>
    /// Creates all standard RAG tools.
    /// </summary>
    public static List<ITool> CreateAll(IKnowledge knowledge)
    {
        return new List<ITool>
        {
            new KnowledgeSearchTool(knowledge),
            new KnowledgeGetDocumentTool(knowledge),
            new KnowledgeAddDocumentTool(knowledge)
        };
    }

    /// <summary>
    /// Creates only search tool.
    /// </summary>
    public static ITool CreateSearchTool(IKnowledge knowledge, int defaultTopK = 5)
    {
        return new KnowledgeSearchTool(knowledge, defaultTopK);
    }
}
