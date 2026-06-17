# GenAI Introduction 2026

> Building AI Agents with **Microsoft Agent Framework** & **Azure AI Services**

A hands-on collection of **17 progressive demos** taking you from a simple streaming chat agent all the way to production-grade, distributed multi-agent systems — using Microsoft Agent Framework, Azure OpenAI, Azure AI Foundry, the Model Context Protocol (MCP), and Microsoft Orleans.

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [VS Code](https://code.visualstudio.com/)
- Azure subscription with Azure OpenAI Service (GPT-4o + Whisper deployments)
- [Ollama](https://ollama.ai/) with `llama3.2:3b` pulled *(demo 07)*
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) *(demo 11)*
- [Node.js](https://nodejs.org/) *(demo 10 — MCP servers via npm)*
- SQL Server with vector column support *(demo 12)*

Additional Azure services required per demo are listed in each demo's README.

## Demos

| # | Demo | Key Concept | Source | Slides |
|---|------|-------------|--------|--------|
| 01 | Agent Framework Chat | Basic streaming chat with Azure OpenAI GPT-4o | [src](src/AgentFramework.Chat/) | [slides](slides/chapter-01/) |
| 02 | Chat History | Persistent sessions with `CreateSessionAsync()` | [src](src/AgentFramework.ChatHistory/) | [slides](slides/chapter-02/) |
| 03 | Chat with Persona | Dynamic persona injection at runtime | [src](src/AgentFramework.ChatPersona/) | [slides](slides/chapter-03/) |
| 04 | Plugins & Functions | Tool use with `AIFunctionFactory` | [src](src/AgentFramework.PluginsAndFunctions/) | [slides](slides/chapter-04/) |
| 05 | Structured Output | Typed responses with `RunAsync<T>()` | [src](src/AgentFramework.StructuredOutput/) | [slides](slides/chapter-05/) |
| 06 | Azure OpenAI Agent | `ChatClientAgent` with scoped instructions | [src](src/AgentFramework.AzureOpenAiAgent/) | [slides](slides/chapter-06/) |
| 07 | Ollama — Local LLM | Run AI fully offline with Ollama | [src](src/AgentFramework.OllamaAgent/) | [slides](slides/chapter-07/) |
| 08 | Azure AI Foundry Agent | Persistent agents with server-side storage | [src](src/AgentFramework.FoundryAgent/) | [slides](slides/chapter-08/) |
| 09 | Multimodal — Audio & Images | Whisper transcription + vision image analysis | [src (audio)](src/AgentFramework.MultiModal.Audio/) · [src (images)](src/AgentFramework.MultiModal.Images/) | [slides](slides/chapter-09/) |
| 10 | MCP Clients | GitHub & Google Maps via external MCP servers | [src (GitHub)](src/AgentFramework.AgentUsingGitHubMcpServer/) · [src (Maps)](src/AgentFramework.AgentUsingGoogleMapsMcpServer/) | [slides](slides/chapter-10/) |
| 11 | MCP Server & Client | Stdio and SSE/HTTP transport layers | [src (stdio)](src/AgentFramework.McpServer/) · [src (SSE)](src/AgentFramework.McpSseServer/) · [src (client)](src/AgentFramework.McpClient/) · [src (SSE client)](src/AgentFramework.McpSseClient/) | [slides](slides/chapter-11/) |
| 12 | Embeddings & Vector Search | SQL Server vector columns + cosine similarity | [src](src/AgentFramework.Embeddings/) | [slides](slides/chapter-12/) |
| 13 | MijnCopilot — Multi-Agent App | Production Blazor app with agent factory | [src](src/MijnCopilot/) | [slides](slides/chapter-13/) |
| 14 | Distributed Agents with Orleans | Horizontal scaling via Orleans grains | [src](src/MijnCopilot.Orleans/) | [slides](slides/chapter-14/) |
| 15 | Agent Workflows | Handoff-based multi-agent orchestration | [src](src/AgentFramework.Workflows/) | [slides](slides/chapter-15/) |
| 16 | Document Intelligence | PDF & image extraction with Azure AI | [src](src/DocumentIntelligence.Poc/) | [slides](slides/chapter-16/) |
| 17 | Content Understanding | Semantic analysis with Azure AI | [src](src/ContentUnderstanding.Poc/) | [slides](slides/chapter-17/) |

## Repository Structure

```
GenAI-Introduction-2026/
├── src/
│   ├── AgentFramework.Common/                      # Shared helpers and extensions
│   ├── AgentFramework.Chat/                        # Demo 01 — Basic Chat
│   ├── AgentFramework.ChatHistory/                 # Demo 02 — Chat History
│   ├── AgentFramework.ChatPersona/                 # Demo 03 — Chat Persona
│   ├── AgentFramework.PluginsAndFunctions/         # Demo 04 — Plugins & Functions
│   ├── AgentFramework.StructuredOutput/            # Demo 05 — Structured Output
│   ├── AgentFramework.AzureOpenAiAgent/            # Demo 06 — Azure OpenAI Agent
│   ├── AgentFramework.OllamaAgent/                 # Demo 07 — Ollama Local LLM
│   ├── AgentFramework.FoundryAgent/                # Demo 08 — Azure AI Foundry
│   ├── AgentFramework.MultiModal.Audio/            # Demo 09 — Audio (Whisper)
│   ├── AgentFramework.MultiModal.Images/           # Demo 09 — Vision
│   ├── AgentFramework.AgentUsingGitHubMcpServer/   # Demo 10 — GitHub MCP Client
│   ├── AgentFramework.AgentUsingGoogleMapsMcpServer/ # Demo 10 — Google Maps MCP Client
│   ├── AgentFramework.McpServer/                   # Demo 11 — MCP Server (stdio)
│   ├── AgentFramework.McpSseServer/                # Demo 11 — MCP Server (SSE)
│   ├── AgentFramework.McpClient/                   # Demo 11 — MCP Client (stdio)
│   ├── AgentFramework.McpSseClient/                # Demo 11 — MCP Client (SSE)
│   ├── AgentFramework.Embeddings/                  # Demo 12 — Embeddings
│   ├── AgentFramework.Workflows/                   # Demo 15 — Workflows
│   ├── MijnCopilot/                                # Demo 13 — Multi-Agent App
│   └── MijnCopilot.Orleans/                        # Demo 14 — Orleans
└── slides/
    ├── README.md                                   # Slide index
    ├── chapter-00/                                 # Title slide
    ├── chapter-01/ … chapter-17/                  # One folder per demo
    └── …
```

## Slides

All slides are 1920×1080 SVG files with a consistent dark purple theme.
Each demo folder contains an **intro slide** and a **content slide**.

Browse the full slide deck → [slides/](slides/)
