# PiKoRe 🌸
**Pi**cture · **Ko**llection · **Re**cognition

> Extensible local photo & video intelligence. Plugin-first. Privacy-first. Your memories, your machine.

---

## What is PiKoRe?

PiKoRe is a personal media library framework for people with large photo/video collections who want serious AI analysis — face recognition, object detection, semantic search, NSFW scoring, aesthetic rating, VLM captions — without any of it leaving their own hardware.

It is built around a minimal C# core (UI shell, plugin engine, pipeline runner) and an open plugin protocol. Plugins can be written in any language. Python is a first-class citizen for ML workloads. The core does not care.

This is not a finished product. It is a framework being built for personal use, designed to be extended.

---

## Status

🌱 **Early development — architecture phase complete, implementation starting.**

- [x] Architecture design & kickoff
- [ ] Solution skeleton
- [ ] Core: file scanner + SQLite job queue
- [ ] Core: plugin registry + HTTP protocol
- [ ] Core: Avalonia virtual grid UI
- [ ] Default plugin: ExifExtractor
- [ ] Default plugin: ThumbnailGenerator
- [ ] Default plugin: CLIP embedder (Python)
- [ ] Text search MVP

---

## Key Design Decisions

- **Plugin protocol:** language-agnostic HTTP JSON. Python, Rust, Go, compiled binary — anything that speaks HTTP is a valid plugin.
- **Storage:** SQLite for framework internals (job queue, plugin registry). PostgreSQL + pgvector for analysis data (embeddings, tags, faces, scores).
- **Privacy:** nothing leaves the machine. No cloud APIs, no telemetry, no accounts.
- **Simplicity first:** the architecture leaves room to grow, but the default path is always the simplest thing that works.

Full architecture rationale: [`docs/architecture.md`](docs/architecture.md)

---

## Requirements

- .NET 9+
- Docker (for PostgreSQL + optional observability stack)
- Python 3.11+ (for ML plugins)
- NVIDIA GPU recommended (RTX series, CUDA 12+)

---

## Project name

*PiKoRe* — almost *PixCore*, but kawaii. 🌸  
Stands for **Pi**cture · **Ko**llection · **Re**cognition.  
Part of the Kumpli World.
