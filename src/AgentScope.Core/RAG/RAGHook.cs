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
using System.Linq;
using System.Threading.Tasks;
using AgentScope.Core.Hook;
using AgentScope.Core.Message;

namespace AgentScope.Core.RAG;

/// <summary>
/// RAG context for retrieval.
/// RAG 检索上下文
/// </summary>
public class RAGContext
{
    /// <summary>
    /// Original user query.
    /// </summary>
    public string Query { get; set; } = "";

    /// <summary>
    /// Retrieved documents.
    /// </summary>
    public IReadOnlyList<KnowledgeSearchResult> RetrievedDocuments { get; set; } = new List<KnowledgeSearchResult>();

    /// <summary>
    /// Formatted context string for LLM.
    /// </summary>
    public string FormattedContext { get; set; } = "";

    /// <summary>
    /// RAG mode used.
    /// </summary>
    public RAGMode Mode { get; set; } = RAGMode.Retrieval;

    /// <summary>
    /// Whether retrieval was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if retrieval failed.
    /// </summary>
    public string? Error { get; set; }
}

/// <summary>
/// Generic RAG Hook that retrieves relevant context before agent execution.
/// 通用 RAG Hook
/// 
/// Java参考: io.agentscope.core.rag.GenericRAGHook
/// </summary>
public class GenericRAGHook : HookBase
{
    private readonly IKnowledge _knowledge;
    private readonly KnowledgeSearchOptions _searchOptions;
    private readonly RAGMode _mode;
    private readonly Func<Msg, string>? _queryExtractor;

    /// <summary>
    /// Creates a new GenericRAGHook.
    /// </summary>
    public GenericRAGHook(
        IKnowledge knowledge,
        KnowledgeSearchOptions? searchOptions = null,
        RAGMode mode = RAGMode.Retrieval,
        Func<Msg, string>? queryExtractor = null)
    {
        _knowledge = knowledge ?? throw new ArgumentNullException(nameof(knowledge));
        _searchOptions = searchOptions ?? new KnowledgeSearchOptions();
        _mode = mode;
        _queryExtractor = queryExtractor;
    }

    /// <inheritdoc />
    public override async Task OnPreReasoningAsync(PreReasoningEvent @event)
    {
        try
        {
            var message = @event.CurrentMessage;
            var query = _queryExtractor?.Invoke(message) ?? message.GetTextContent() ?? "";
            
            if (string.IsNullOrWhiteSpace(query))
            {
                return;
            }

            // Retrieve relevant documents
            var searchResults = await _knowledge.SearchAsync(query, _searchOptions);

            var ragContext = new RAGContext
            {
                Query = query,
                RetrievedDocuments = searchResults,
                Mode = _mode,
                Success = searchResults.Count > 0
            };

            if (searchResults.Count == 0)
            {
                ragContext.FormattedContext = "No relevant context found.";
            }
            else
            {
                // Format retrieved documents as context
                ragContext.FormattedContext = FormatContext(searchResults);
            }

            // Store RAG context in message metadata
            message.Metadata ??= new Dictionary<string, object>();
            message.Metadata["RAGContext"] = ragContext;

            // Modify message based on RAG mode
            switch (_mode)
            {
                case RAGMode.Retrieval:
                    // Append context to the message
                    var enhancedContent = $"Context:\n{ragContext.FormattedContext}\n\nQuestion: {query}";
                    message.SetTextContent(enhancedContent);
                    break;

                case RAGMode.RetrievalQA:
                    // Replace with RAG-enhanced prompt
                    var qaPrompt = $"Based on the following context, answer the question.\n\nContext:\n{ragContext.FormattedContext}\n\nQuestion: {query}\n\nAnswer:";
                    message.SetTextContent(qaPrompt);
                    break;

                case RAGMode.RetrievalOnly:
                    // Don't modify message, just store context
                    break;
            }
        }
        catch (System.Exception ex)
        {
            // Log error but don't block execution
            Console.WriteLine($"RAG retrieval failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Formats search results as context string.
    /// </summary>
    private static string FormatContext(IReadOnlyList<KnowledgeSearchResult> results)
    {
        var parts = results.Select((r, i) => 
            $"[{i + 1}] {r.Document.Title ?? "Document"}\n{r.Document.Content}");
        
        return string.Join("\n\n", parts);
    }
}

/// <summary>
/// RAG Hook builder for fluent configuration.
/// </summary>
public class RAGHookBuilder
{
    private IKnowledge? _knowledge;
    private KnowledgeSearchOptions _searchOptions = new();
    private RAGMode _mode = RAGMode.Retrieval;
    private Func<Msg, string>? _queryExtractor;

    public RAGHookBuilder WithKnowledge(IKnowledge knowledge)
    {
        _knowledge = knowledge;
        return this;
    }

    public RAGHookBuilder WithTopK(int topK)
    {
        _searchOptions.TopK = topK;
        return this;
    }

    public RAGHookBuilder WithMinScore(float minScore)
    {
        _searchOptions.MinScore = minScore;
        return this;
    }

    public RAGHookBuilder WithMode(RAGMode mode)
    {
        _mode = mode;
        return this;
    }

    public RAGHookBuilder WithQueryExtractor(Func<Msg, string> extractor)
    {
        _queryExtractor = extractor;
        return this;
    }

    public RAGHookBuilder WithFilters(Dictionary<string, object> filters)
    {
        _searchOptions.Filters = filters;
        return this;
    }

    public GenericRAGHook Build()
    {
        if (_knowledge == null)
        {
            throw new InvalidOperationException("Knowledge base must be set");
        }

        return new GenericRAGHook(_knowledge, _searchOptions, _mode, _queryExtractor);
    }

    public static RAGHookBuilder Create() => new();
}
