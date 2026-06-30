# Decision Log - hMailServer to Stalwart Migration Tool

This file documents the evolution of requirements and decisions made during the specification phase of the hMailServer to Stalwart Mail Server migration tool project.

---

## 📋 Prompt History & Outcomes

| **#** | **Prompt (Verbatim)** | **Outcome** |
|-------|----------------------|-------------|
| **1** | `/spec the intial idea is in the spec.md file` | Applied spec-driven-development skill. Found `initspec.md` with hMailServer→Stalwart migration concept. Created initial SPEC.md structure. |
| **2** | `Standalone CLI` | Answer to "Tool Type" question. Confirmed standalone CLI application approach. |
| **3** | `Small - less than 100 users, but 10 years worth of emails, so a medium volume` | Answer to "Scale" question. Clarified user count vs. data volume distinction. |
| **4** | `Basic validation only` | Answer to "Validation" question. Set validation scope (counts + basic integrity, not full validation). |
| **5** | `zip archives, which use json for data, and eml for emails, and binaries are unmodified` | Answer to "Data Format" question. Defined archive structure: ZIP → JSON metadata + EML emails + binary attachments. |
| **6** | `add this to the spec, and add that user experience point to the documentation` | Updated SPEC.md with conflict resolution strategy, Docker scope decisions, and Docker user experience example. |
| **7** | `use vandelay, explore the codebase to understand the gaps in capability (I think it's aliasses and accounts)` | Explored Vandelay repository. **Confirmed gaps**: Account creation, domain management, **email aliases** (user's suspicion correct), full account metadata. |
| **8** | `Do Vandelay as a subcommand/dependency` | Major architecture pivot. Updated SPEC.md to integrate Vandelay as subprocess/dependency, with our tool filling infrastructure gaps. |
| **9** | `redo that, list the prompts verbatim` | Generated this decision log file. |

---

## 🎯 Key Decisions Summary

### Architecture Decisions

| Decision | Rationale | Impact |
|----------|-----------|--------|
| **Standalone CLI** | User preference | C# 10, .NET Core, cross-platform |
| **Sequential Processing** | Data consistency | Per domain, predictable resource usage |
| **30s Checkpoints** | Balance overhead vs. resume capability | Runtime-based checkpoint frequency |
| **One ZIP per Domain** | Manageability vs. atomicity | Balanced archive structure |

### Integration Decisions

| Decision | Rationale | Impact |
|----------|-----------|--------|
| **Merge Conflict Strategy** | Non-destructive approach | Preserve existing Stalwart data |
| **API Only Docker Scope** | Separation of concerns | Users handle container lifecycle |
| **COM API for hMailServer** | Reliability | Official hMailServer access method |
| **Multi-level Logging** | Flexibility | Error, Warn, Info, Debug levels |

### Vandelay Integration

| Decision | Rationale | Impact |
|----------|-----------|--------|
| **Use Vandelay as dependency** | Leverage existing JMAP migration | Reduces development effort |
| **Fill Vandelay's gaps** | Complete solution | Our tool handles accounts, domains, aliases |
| **Subprocess execution** | Integration approach | Run Vandelay as external process |

---

## 🔍 Critical Findings

### Vandelay Capability Analysis

**Vandelay CAN do:**
- ✅ IMAP Import (hMailServer → SQLite)
- ✅ JMAP Export (SQLite → Stalwart)  
- ✅ Mail, Contacts, Calendars, Files migration
- ✅ Convergent/resumable operations
- ✅ Multi-protocol support

**Vandelay CANNOT do (Our tool fills these):**
- ❌ **Account Creation** - Only works with existing accounts
- ❌ **Domain Management** - No domain creation support
- ❌ **Email Aliases** - Not a JMAP standard object type
- ❌ **Full Account Metadata** - Only basic identities
- ❌ **Orchestration** - Manual process required

---

## 📊 Architecture Evolution

1. **Initial Concept**: Standalone migration tool doing everything
2. **After Requirements Clarification**: Structured CLI tool with ZIP archives
3. **After Vandelay Discovery**: Complementary tool leveraging Vandelay
4. **Final Architecture**: Our tool handles infrastructure (accounts, domains, aliases), Vandelay handles data (mail, contacts, calendars)

---

## 📁 Related Files

- `SPEC.md` - Current specification document
- `initspec.md` - Original initial idea
- This file (`decision-log.md`) - Decision history and rationale

---

*Generated: 2026-06-29*
*Project: hMailServer to Stalwart Mail Server Migration Tool*