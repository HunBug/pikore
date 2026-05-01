---
mode: agent
description: Scaffold a new PiKoRe plugin (in-process C# or external Python)
---

Create a new PiKoRe plugin with the following details:

- **Plugin name:** ${input:pluginName}
- **Type:** ${input:pluginType:C# in-process|Python external}
- **Capabilities produced:** ${input:capabilities} (e.g. tags, faces, embedding, scores, description)
- **Requires capabilities:** ${input:requires} (e.g. thumbnail, or leave empty)

## For C# in-process plugins

Create `src/PiKoRe.Plugins.${pluginName}/`:
- Implement `IAnalyzerPlugin` interface from `PiKoRe.Core`
- Register in DI as scoped
- Add XML doc comment on the class explaining what it produces and which library it uses
- Add at least one unit test in `tests/`

## For Python external plugins

Create `plugins/${pluginName}/`:
- `plugin.json` — manifest with name, version, capabilities_produced, requires_capabilities, endpoint, gpu_memory_mb
- `plugin.py` — FastAPI app exposing `POST /analyze` and `GET /health`
- `requirements.txt` — pinned dependencies
- `install.sh` — creates venv, installs requirements, prints success message
- `README.md` — what model it uses, how to install, what it produces

The `/analyze` endpoint receives:
```json
{ "job_id": "uuid", "file_id": "uuid", "file_path": "/abs/path", "preview_path": "/abs/path/preview.jpg" }
```

And returns:
```json
{
  "file_id": "uuid",
  "tags": [{"label": "beach", "confidence": 0.91}],
  "faces": null,
  "embedding": null,
  "scores": {},
  "description": null
}
```

Return only the fields this plugin produces. Set others to null.

Always include: structured logging with job_id and file_id in every log entry. Error handling that returns HTTP 500 with a JSON error body rather than crashing.
