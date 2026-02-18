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
using AgentScope.Core.RAG;
using Xunit;

namespace AgentScope.Core.Tests.RAG;

public class RAGTests
{
    #region KnowledgeDocument Tests

    [Fact]
    public void KnowledgeDocument_DefaultValues_AreCorrect()
    {
        var doc = new KnowledgeDocument();

        Assert.NotNull(doc.Id);
        Assert.NotEmpty(doc.Id);
        Assert.Empty(doc.Content);
        Assert.Empty(doc.Metadata);
        Assert.NotEqual(default, doc.CreatedAt);
    }

    [Fact]
    public void KnowledgeDocument_SetProperties_Work()
    {
        var doc = new KnowledgeDocument
        {
            Title = "Test Doc",
            Content = "Test content",
            Source = "test.txt",
            Metadata = new Dictionary<string, object> { ["key"] = "value" }
        };

        Assert.Equal("Test Doc", doc.Title);
        Assert.Equal("Test content", doc.Content);
        Assert.Equal("test.txt", doc.Source);
        Assert.Equal("value", doc.Metadata["key"]);
    }

    #endregion

    #region KnowledgeSearchResult Tests

    [Fact]
    public void KnowledgeSearchResult_DefaultValues_AreCorrect()
    {
        var result = new KnowledgeSearchResult();

        Assert.NotNull(result.Document);
        Assert.Equal(0, result.Score);
        Assert.Equal(0, result.Rank);
    }

    #endregion

    #region KnowledgeSearchOptions Tests

    [Fact]
    public void KnowledgeSearchOptions_DefaultValues_AreCorrect()
    {
        var options = new KnowledgeSearchOptions();

        Assert.Equal(5, options.TopK);
        Assert.Null(options.MinScore);
        Assert.Null(options.Filters);
        Assert.False(options.IncludeEmbeddings);
    }

    #endregion

    #region InMemoryVectorStore Tests

    [Fact]
    public async Task InMemoryVectorStore_AddDocument_ReturnsId()
    {
        var store = new InMemoryVectorStore();
        var doc = new KnowledgeDocument { Content = "Test content" };

        var id = await store.AddDocumentAsync(doc);

        Assert.NotNull(id);
        Assert.Equal(doc.Id, id);
    }

    [Fact]
    public async Task InMemoryVectorStore_AddDocuments_ReturnsIds()
    {
        var store = new InMemoryVectorStore();
        var docs = new[]
        {
            new KnowledgeDocument { Content = "Doc 1" },
            new KnowledgeDocument { Content = "Doc 2" }
        };

        var ids = await store.AddDocumentsAsync(docs);

        Assert.Equal(2, ids.Count);
    }

    [Fact]
    public async Task InMemoryVectorStore_GetDocument_Existing_ReturnsDocument()
    {
        var store = new InMemoryVectorStore();
        var doc = new KnowledgeDocument { Content = "Test" };
        await store.AddDocumentAsync(doc);

        var retrieved = await store.GetDocumentAsync(doc.Id);

        Assert.NotNull(retrieved);
        Assert.Equal(doc.Content, retrieved.Content);
    }

    [Fact]
    public async Task InMemoryVectorStore_GetDocument_NonExisting_ReturnsNull()
    {
        var store = new InMemoryVectorStore();

        var retrieved = await store.GetDocumentAsync("non-existing");

        Assert.Null(retrieved);
    }

    [Fact]
    public async Task InMemoryVectorStore_DeleteDocument_Existing_ReturnsTrue()
    {
        var store = new InMemoryVectorStore();
        var doc = new KnowledgeDocument { Content = "Test" };
        await store.AddDocumentAsync(doc);

        var deleted = await store.DeleteDocumentAsync(doc.Id);

        Assert.True(deleted);
        Assert.Null(await store.GetDocumentAsync(doc.Id));
    }

    [Fact]
    public async Task InMemoryVectorStore_DeleteDocument_NonExisting_ReturnsFalse()
    {
        var store = new InMemoryVectorStore();

        var deleted = await store.DeleteDocumentAsync("non-existing");

        Assert.False(deleted);
    }

    [Fact]
    public async Task InMemoryVectorStore_GetDocumentCount_ReturnsCorrectCount()
    {
        var store = new InMemoryVectorStore();
        await store.AddDocumentAsync(new KnowledgeDocument { Content = "1" });
        await store.AddDocumentAsync(new KnowledgeDocument { Content = "2" });

        var count = await store.GetDocumentCountAsync();

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task InMemoryVectorStore_Clear_RemovesAllDocuments()
    {
        var store = new InMemoryVectorStore();
        await store.AddDocumentAsync(new KnowledgeDocument { Content = "1" });
        await store.AddDocumentAsync(new KnowledgeDocument { Content = "2" });

        await store.ClearAsync();
        var count = await store.GetDocumentCountAsync();

        Assert.Equal(0, count);
    }

    [Fact]
    public async Task InMemoryVectorStore_UpdateDocument_Existing_UpdatesAndReturnsTrue()
    {
        var store = new InMemoryVectorStore();
        var doc = new KnowledgeDocument { Content = "Original" };
        await store.AddDocumentAsync(doc);

        doc.Content = "Updated";
        var updated = await store.UpdateDocumentAsync(doc);

        Assert.True(updated);
        var retrieved = await store.GetDocumentAsync(doc.Id);
        Assert.Equal("Updated", retrieved?.Content);
    }

    [Fact]
    public async Task InMemoryVectorStore_UpdateDocument_NonExisting_ReturnsFalse()
    {
        var store = new InMemoryVectorStore();
        var doc = new KnowledgeDocument { Content = "Test" };

        var updated = await store.UpdateDocumentAsync(doc);

        Assert.False(updated);
    }

    [Fact]
    public async Task InMemoryVectorStore_DeleteDocumentsByFilter_DeletesMatching()
    {
        var store = new InMemoryVectorStore();
        await store.AddDocumentAsync(new KnowledgeDocument 
        { 
            Content = "Doc 1",
            Metadata = new Dictionary<string, object> { ["type"] = "A" }
        });
        await store.AddDocumentAsync(new KnowledgeDocument 
        { 
            Content = "Doc 2",
            Metadata = new Dictionary<string, object> { ["type"] = "B" }
        });

        var deleted = await store.DeleteDocumentsAsync(new Dictionary<string, object> { ["type"] = "A" });

        Assert.Equal(1, deleted);
        Assert.Equal(1, await store.GetDocumentCountAsync());
    }

    #endregion

    #region SimpleEmbeddingGenerator Tests

    [Fact]
    public async Task SimpleEmbeddingGenerator_GenerateEmbedding_ReturnsVector()
    {
        var generator = new SimpleEmbeddingGenerator(10);

        var embedding = await generator.GenerateEmbeddingAsync("test");

        Assert.NotNull(embedding);
        Assert.Equal(10, embedding.Length);
    }

    [Fact]
    public async Task SimpleEmbeddingGenerator_GenerateEmbedding_SameText_SameEmbedding()
    {
        var generator = new SimpleEmbeddingGenerator(10);

        var embedding1 = await generator.GenerateEmbeddingAsync("test");
        var embedding2 = await generator.GenerateEmbeddingAsync("test");

        Assert.Equal(embedding1, embedding2);
    }

    [Fact]
    public async Task SimpleEmbeddingGenerator_GenerateEmbedding_DifferentText_DifferentEmbedding()
    {
        var generator = new SimpleEmbeddingGenerator(10);

        var embedding1 = await generator.GenerateEmbeddingAsync("test1");
        var embedding2 = await generator.GenerateEmbeddingAsync("test2");

        Assert.NotEqual(embedding1, embedding2);
    }

    [Fact]
    public async Task SimpleEmbeddingGenerator_GenerateEmbeddings_ReturnsMultiple()
    {
        var generator = new SimpleEmbeddingGenerator(10);
        var texts = new[] { "text1", "text2", "text3" };

        var embeddings = await generator.GenerateEmbeddingsAsync(texts);

        Assert.Equal(3, embeddings.Count);
        Assert.All(embeddings, e => Assert.Equal(10, e.Length));
    }

    [Fact]
    public void SimpleEmbeddingGenerator_EmbeddingDimension_ReturnsCorrectValue()
    {
        var generator = new SimpleEmbeddingGenerator(512);

        Assert.Equal(512, generator.EmbeddingDimension);
    }

    #endregion

    #region Search Tests

    [Fact]
    public async Task InMemoryVectorStore_Search_WithEmbeddingGenerator_ReturnsResults()
    {
        var generator = new SimpleEmbeddingGenerator(10);
        var store = new InMemoryVectorStore(generator);
        
        await store.AddDocumentAsync(new KnowledgeDocument { Content = "Machine learning is a subset of AI" });
        await store.AddDocumentAsync(new KnowledgeDocument { Content = "The weather is sunny today" });

        var results = await store.SearchAsync("artificial intelligence", new KnowledgeSearchOptions { TopK = 2 });

        Assert.NotEmpty(results);
    }

    [Fact]
    public async Task InMemoryVectorStore_Search_WithoutGenerator_ThrowsException()
    {
        var store = new InMemoryVectorStore(); // No embedding generator

        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            store.SearchAsync("query"));
    }

    [Fact]
    public async Task InMemoryVectorStore_Search_WithMinScore_FiltersResults()
    {
        var generator = new SimpleEmbeddingGenerator(10);
        var store = new InMemoryVectorStore(generator);
        
        await store.AddDocumentAsync(new KnowledgeDocument { Content = "Test content" });

        var results = await store.SearchAsync("completely different", new KnowledgeSearchOptions 
        { 
            TopK = 5, 
            MinScore = 0.99f 
        });

        Assert.Empty(results);
    }

    [Fact]
    public async Task InMemoryVectorStore_SearchByEmbedding_ReturnsResults()
    {
        var generator = new SimpleEmbeddingGenerator(10);
        var store = new InMemoryVectorStore(generator);
        var embedding = await generator.GenerateEmbeddingAsync("test");
        
        await store.AddDocumentAsync(new KnowledgeDocument { Content = "Test doc" });

        var results = await store.SearchByEmbeddingAsync(embedding, new KnowledgeSearchOptions { TopK = 5 });

        Assert.NotEmpty(results);
    }

    [Fact]
    public async Task InMemoryVectorStore_Search_WithFilter_AppliesFilter()
    {
        var generator = new SimpleEmbeddingGenerator(10);
        var store = new InMemoryVectorStore(generator);
        
        await store.AddDocumentAsync(new KnowledgeDocument 
        { 
            Content = "Doc A",
            Metadata = new Dictionary<string, object> { ["category"] = "A" }
        });
        await store.AddDocumentAsync(new KnowledgeDocument 
        { 
            Content = "Doc B",
            Metadata = new Dictionary<string, object> { ["category"] = "B" }
        });

        var results = await store.SearchAsync("doc", new KnowledgeSearchOptions 
        { 
            TopK = 5,
            Filters = new Dictionary<string, object> { ["category"] = "A" }
        });

        Assert.All(results, r => Assert.Equal("A", r.Document.Metadata["category"]));
    }

    #endregion

    #region KnowledgeSearchTool Tests

    [Fact]
    public async Task KnowledgeSearchTool_Execute_Success_ReturnsResults()
    {
        var generator = new SimpleEmbeddingGenerator(10);
        var knowledge = new InMemoryVectorStore(generator);
        await knowledge.AddDocumentAsync(new KnowledgeDocument { Content = "AI is the future" });
        
        var tool = new KnowledgeSearchTool(knowledge);
        var result = await tool.ExecuteAsync(new Dictionary<string, object> 
        { 
            ["query"] = "artificial intelligence" 
        });

        Assert.True(result.Success);
        Assert.NotNull(result.Result);
    }

    [Fact]
    public async Task KnowledgeSearchTool_Execute_NoResults_ReturnsMessage()
    {
        var generator = new SimpleEmbeddingGenerator(10);
        var knowledge = new InMemoryVectorStore(generator);
        
        var tool = new KnowledgeSearchTool(knowledge);
        var result = await tool.ExecuteAsync(new Dictionary<string, object> 
        { 
            ["query"] = "something" 
        });

        Assert.True(result.Success);
        Assert.Contains("No relevant information", result.Result?.ToString());
    }

    [Fact]
    public async Task KnowledgeSearchTool_Execute_InvalidParams_ReturnsFailure()
    {
        var knowledge = new InMemoryVectorStore();
        var tool = new KnowledgeSearchTool(knowledge);
        
        // Missing required parameter should return failure
        var result = await tool.ExecuteAsync(new Dictionary<string, object>());
        
        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }

    #endregion

    #region KnowledgeGetDocumentTool Tests

    [Fact]
    public async Task KnowledgeGetDocumentTool_Execute_Existing_ReturnsDocument()
    {
        var knowledge = new InMemoryVectorStore();
        var doc = new KnowledgeDocument { Content = "Test", Title = "Title" };
        await knowledge.AddDocumentAsync(doc);
        
        var tool = new KnowledgeGetDocumentTool(knowledge);
        var result = await tool.ExecuteAsync(new Dictionary<string, object> 
        { 
            ["document_id"] = doc.Id 
        });

        Assert.True(result.Success);
    }

    [Fact]
    public async Task KnowledgeGetDocumentTool_Execute_NonExisting_ReturnsFailure()
    {
        var knowledge = new InMemoryVectorStore();
        var tool = new KnowledgeGetDocumentTool(knowledge);
        
        var result = await tool.ExecuteAsync(new Dictionary<string, object> 
        { 
            ["document_id"] = "non-existing" 
        });

        Assert.False(result.Success);
    }

    #endregion

    #region KnowledgeAddDocumentTool Tests

    [Fact]
    public async Task KnowledgeAddDocumentTool_Execute_Success_ReturnsId()
    {
        var knowledge = new InMemoryVectorStore();
        var tool = new KnowledgeAddDocumentTool(knowledge);
        
        var result = await tool.ExecuteAsync(new Dictionary<string, object> 
        { 
            ["content"] = "New document content",
            ["title"] = "New Doc"
        });

        Assert.True(result.Success);
        Assert.NotNull(result.Result);
    }

    [Fact]
    public async Task KnowledgeAddDocumentTool_Execute_WithoutContent_ReturnsFailure()
    {
        var knowledge = new InMemoryVectorStore();
        var tool = new KnowledgeAddDocumentTool(knowledge);
        
        // Missing required parameter should return failure
        var result = await tool.ExecuteAsync(new Dictionary<string, object>());
        
        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }

    #endregion

    #region RAGTools Factory Tests

    [Fact]
    public void RAGTools_CreateAll_ReturnsAllTools()
    {
        var knowledge = new InMemoryVectorStore();
        
        var tools = RAGTools.CreateAll(knowledge);

        Assert.Equal(3, tools.Count);
        Assert.Contains(tools, t => t.Name == "knowledge_search");
        Assert.Contains(tools, t => t.Name == "knowledge_get_document");
        Assert.Contains(tools, t => t.Name == "knowledge_add_document");
    }

    [Fact]
    public void RAGTools_CreateSearchTool_ReturnsSearchTool()
    {
        var knowledge = new InMemoryVectorStore();
        
        var tool = RAGTools.CreateSearchTool(knowledge);

        Assert.Equal("knowledge_search", tool.Name);
    }

    #endregion

    #region RAGContext Tests

    [Fact]
    public void RAGContext_DefaultValues_AreCorrect()
    {
        var context = new RAGContext();

        Assert.Empty(context.Query);
        Assert.Empty(context.RetrievedDocuments);
        Assert.Empty(context.FormattedContext);
        Assert.Equal(RAGMode.Retrieval, context.Mode);
        Assert.False(context.Success);
    }

    #endregion

    #region RAGHookBuilder Tests

    [Fact]
    public void RAGHookBuilder_Build_WithKnowledge_ReturnsHook()
    {
        var knowledge = new InMemoryVectorStore();
        
        var hook = RAGHookBuilder.Create()
            .WithKnowledge(knowledge)
            .Build();

        Assert.NotNull(hook);
    }

    [Fact]
    public void RAGHookBuilder_Build_WithoutKnowledge_ThrowsException()
    {
        Assert.Throws<InvalidOperationException>(() => 
            RAGHookBuilder.Create().Build());
    }

    [Fact]
    public void RAGHookBuilder_WithTopK_SetsTopK()
    {
        var knowledge = new InMemoryVectorStore();
        
        var hook = RAGHookBuilder.Create()
            .WithKnowledge(knowledge)
            .WithTopK(10)
            .Build();

        Assert.NotNull(hook);
    }

    [Fact]
    public void RAGHookBuilder_WithMode_SetsMode()
    {
        var knowledge = new InMemoryVectorStore();
        
        var hook = RAGHookBuilder.Create()
            .WithKnowledge(knowledge)
            .WithMode(RAGMode.RetrievalOnly)
            .Build();

        Assert.NotNull(hook);
    }

    #endregion

    #region KnowledgeConfig Tests

    [Fact]
    public void KnowledgeConfig_DefaultValues_AreCorrect()
    {
        var config = new KnowledgeConfig();

        Assert.Equal("default", config.Name);
        Assert.Equal(1536, config.EmbeddingDimension);
        Assert.Equal(5, config.DefaultTopK);
        Assert.Null(config.DefaultMinScore);
        Assert.Equal(1000, config.ChunkSize);
        Assert.Equal(200, config.ChunkOverlap);
        Assert.True(config.EnableHybridSearch);
    }

    #endregion
}
