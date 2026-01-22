# ProCohere – Engineering Documentation

This repository contains the authoritative engineering documentation for ProCohere.

The documentation is organized by domain so each major area of the system can evolve independently while remaining easy to discover in GitHub.

---

## Documentation Areas

### 📦 Database
Comprehensive technical documentation for the ProCohere database, including:
- Architecture and schema design
- Row Level Security (RLS) model
- Tables, relationships, functions, indexes, and constraints
- GRANTS / ACL behavior
- Developer guidance for querying and extending the database

📁 **Location:** [`Wiki/Database/`](Wiki/Database/)

➡️ **Start here:** [Wiki / Database README](Wiki/Database/README.md)

---

### 🖥️ UI
User interface architecture, patterns, and implementation notes.

📁 **Location:** `Wiki/UI/`  
*(Documentation to be added)*

---

### ⚙️ Backend / Services
API design, background processing, and integration details.

📁 **Location:** `Wiki/Backend/`  
*(Documentation to be added)*

---

### 🤖 AI & Automation
AI flows, prompt strategy, vector usage, and automation pipelines.

📁 **Location:** `Wiki/AI/`  
*(Documentation to be added)*

---

## How to Use This Repository

- Each section under `Wiki/` is designed to function like a standalone wiki
- Documentation is written to be read top-to-bottom by engineers new to the system
- Markdown files are the source of truth and should be kept in sync with the codebase

---

## Contribution Guidelines

- Update documentation alongside schema or architectural changes
- Prefer clarity over brevity
- Avoid duplicating business logic explanations already covered in code

---

## Source of Truth

This documentation reflects the current production architecture and schema.
If the documentation and code disagree, the code is authoritative until the documentation is updated.

---

**Primary Database Entry Point:** `Wiki/Database/README.md`
