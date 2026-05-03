"""
Diagram generator for PiKoRe learning materials.

Generates PNG files into learning/assets/.

Setup (once):
    cd learning/tools
    python -m venv .venv
    source .venv/bin/activate        # Windows: .venv\\Scripts\\activate
    pip install -r requirements.txt

Run (from repo root, with venv active):
    python learning/tools/generate_diagrams.py

Also requires the Graphviz binary on PATH:
    Arch/Ubuntu:  sudo pacman -S graphviz  /  sudo apt install graphviz
    macOS:        brew install graphviz

Add new diagrams at the bottom of this file.
Each diagram function should:
  1. Build its content (graphviz Source or matplotlib figure)
  2. Save to ASSETS_DIR / "diagram_name.png"
  3. Print a confirmation line

Reference images in lessons with:
    ![Description](../assets/diagram_name.png)
"""

from pathlib import Path

ASSETS_DIR = Path(__file__).parent.parent / "assets"
ASSETS_DIR.mkdir(exist_ok=True)


def _save_graphviz(source: str, name: str) -> None:
    """Render a Graphviz DOT source string and save as PNG."""
    import graphviz
    g = graphviz.Source(source, format="png")
    output = ASSETS_DIR / name
    g.render(str(output), cleanup=True)
    print(f"  [ok] {output}.png")


# ---------------------------------------------------------------------------
# Diagram: overall system component overview
# ---------------------------------------------------------------------------

def diagram_system_overview() -> None:
    source = """
    digraph system_overview {
        rankdir=LR;
        node [shape=box, style=filled, fontname="Helvetica", fontsize=11];

        subgraph cluster_core {
            label="PiKoRe.Core";
            style=filled; color=lightblue;
            PipelineWorker [label="PipelineWorker\\n(BackgroundService)"];
            DagEngine [label="DagEngine\\n(INotificationHandler)"];
            LocalSequentialRunner [label="LocalSequentialRunner\\n(IJobRunner)"];
        }

        subgraph cluster_data {
            label="PiKoRe.Data";
            style=filled; color=lightyellow;
            SqliteJobQueue [label="SqliteJobQueue\\n(IJobQueue)"];
        }

        subgraph cluster_plugins {
            label="Plugins";
            style=filled; color=lightgreen;
            ExifPlugin [label="ExifPlugin\\n(IInProcessPlugin)"];
            ThumbnailPlugin [label="ThumbnailPlugin\\n(IInProcessPlugin)"];
        }

        subgraph cluster_storage {
            label="Storage";
            style=filled; color=lightsalmon;
            SQLite [label="SQLite\\n(job queue,\\nfile index,\\nplugin registry)", shape=cylinder];
            PostgreSQL [label="PostgreSQL\\n(metadata, tags,\\nthumbnails,\\nembeddings)", shape=cylinder];
        }

        PipelineWorker -> SqliteJobQueue [label="DequeueAsync"];
        PipelineWorker -> LocalSequentialRunner [label="RunAsync"];
        LocalSequentialRunner -> ExifPlugin [label="AnalyzeAsync"];
        LocalSequentialRunner -> ThumbnailPlugin [label="AnalyzeAsync"];
        LocalSequentialRunner -> DagEngine [label="JobCompletedEvent\\n(via MediatR)"];
        DagEngine -> SqliteJobQueue [label="EnqueueAsync"];
        SqliteJobQueue -> SQLite;
        ExifPlugin -> PostgreSQL [label="UpsertMetadataAsync"];
        ThumbnailPlugin -> PostgreSQL [label="UpsertThumbnailAsync"];
    }
    """
    _save_graphviz(source, "system_overview")


# ---------------------------------------------------------------------------
# Diagram: job lifecycle state machine
# ---------------------------------------------------------------------------

def diagram_job_lifecycle() -> None:
    source = """
    digraph job_lifecycle {
        rankdir=LR;
        node [shape=ellipse, style=filled, fontname="Helvetica", fontsize=11];

        queued [fillcolor=lightyellow];
        running [fillcolor=lightblue];
        completed [fillcolor=lightgreen];
        failed [fillcolor=lightsalmon];

        queued -> running [label="DequeueAsync"];
        running -> completed [label="MarkCompletedAsync"];
        running -> failed [label="MarkFailedAsync"];
        failed -> queued [label="retry\\n(POST /debug/jobs/{id}/retry)", style=dashed];
    }
    """
    _save_graphviz(source, "job_lifecycle")


# ---------------------------------------------------------------------------
# Diagram: DAG capability chaining example
# ---------------------------------------------------------------------------

def diagram_dag_example() -> None:
    source = """
    digraph dag_example {
        rankdir=LR;
        node [shape=box, style=filled, fontname="Helvetica", fontsize=11, fillcolor=lightyellow];
        edge [fontname="Helvetica", fontsize=10];

        FileIndexed [label="FileIndexedEvent\\n(new file found)", shape=ellipse, fillcolor=lightblue];
        exif [label="exif\\n(ExifPlugin)"];
        thumbnail [label="thumbnail\\n(ThumbnailPlugin)"];
        embedding [label="embedding\\n(ClipAdapter)", fillcolor=lightgreen];

        FileIndexed -> exif [label="no deps"];
        FileIndexed -> thumbnail [label="no deps"];
        thumbnail -> embedding [label="requires: thumbnail"];
    }
    """
    _save_graphviz(source, "dag_example")


# ---------------------------------------------------------------------------
# Diagram: two-database architecture
# ---------------------------------------------------------------------------

def diagram_two_databases() -> None:
    source = """
    digraph two_databases {
        rankdir=TB;
        node [style=filled, fontname="Helvetica", fontsize=11];

        subgraph cluster_sqlite {
            label="SQLite — framework internals";
            style=filled; color=lightyellow;
            file_index [label="file_index\\n(path, mtime, hash,\\nmedia_type)", shape=record];
            job_queue [label="job_queue\\n(id, file_id, capability,\\nstatus, priority)", shape=record];
            plugin_registry [label="plugin_registry\\n(name, version,\\nendpoint, capabilities)", shape=record];
            pipeline_config [label="pipeline_config\\n(dag_json)", shape=record];
        }

        subgraph cluster_postgres {
            label="PostgreSQL + pgvector — analysis results";
            style=filled; color=lightblue;
            metadata [label="metadata\\n(file_id, key, value)", shape=record];
            thumbnails [label="thumbnails\\n(file_id, size_class,\\ndata_path)", shape=record];
            embeddings [label="embeddings\\n(file_id, vector(512))", shape=record];
            tags [label="tags\\n(file_id, label, confidence)", shape=record];
        }

        note [label="Rule: SQLite = pipeline state\\nPostgreSQL = analysis output\\nNever mix!", shape=note, fillcolor=lightsalmon];
    }
    """
    _save_graphviz(source, "two_databases")


# ---------------------------------------------------------------------------
# Entry point
# ---------------------------------------------------------------------------

if __name__ == "__main__":
    print("Generating diagrams...")
    diagram_system_overview()
    diagram_job_lifecycle()
    diagram_dag_example()
    diagram_two_databases()
    print(f"\nAll diagrams written to: {ASSETS_DIR}")
    print("Reference in a lesson with: ![Alt text](../assets/<name>.png)")
