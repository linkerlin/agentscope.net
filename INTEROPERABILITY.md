# AgentScope.NET and AgentScope-Java Interoperability

**Version**: v2.0.1 (develop/v2.0.1) | **Updated**: 2026-08-17

## Overview

Both implementations use compatible JSON serialization, shared SQLite schema, and common `.env` configuration.

## Protocol Support

| Protocol | Status |
|----------|--------|
| JSON Messages | ✅ Compatible (System.Text.Json) |
| SQLite Schema | ✅ Shared schema |
| .env Config | ✅ Common format |
| REST API | ✅ Both support HTTP |
| **A2A Protocol** | ✅ Full client + server |
| **MCP Protocol** | ✅ stdio/SSE/StreamableHTTP |
| Message Queues | ⚠️ Standard formats |

## A2A

.NET provides full A2A client (AgentCardResolver/A2aAgent) and server (AgentScopeA2aServer).

## MCP

Three clients: StdioMcpClient, SseMcpClient, StreamableHttpMcpClient.

## Build Status

- Core: ✅ 0 errors
- Full solution: 🔴 118 errors (test mocks + Uno XAML)
