# 06e – Metrics Domain Tables

This document covers the metrics tables in the `procohere` schema.

**Last Updated:** January 2026  
**Total Tables in this domain:** 2

---

## Tables in this Document

| # | Table Name | Has Model? |
|---|------------|------------|
| 1 | metrics | ✅ MetricDetail.cs (fixed) |
| 2 | metric_values | ✅ MetricHistoryEntry.cs |

---

## procohere.metrics

**Purpose**  
KPI/metric definitions owned by team members.

**Columns**
| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| id | uuid | NO | PK |
| organization_id | uuid | NO | FK → organizations |
| owner_id | uuid | YES | FK → team_members |
| name | text | NO | |
| description | text | YES | |
| metric_type | text | NO | 'number', 'percentage', 'currency' |
| unit | text | YES | |
| target_value | numeric | YES | |
| current_value | numeric | YES | |
| direction | text | YES | 'higher_is_better', 'lower_is_better', 'neutral' |
| frequency | text | YES | 'daily', 'weekly', 'monthly', 'quarterly' |
| is_deleted | boolean | NO | |
| created_at | timestamptz | NO | |
| updated_at | timestamptz | NO | |
| deleted_at | timestamptz | YES | |
| deleted_by | uuid | YES | |

**Model:** `MetricDetail.cs` ✅ Verified match (after fix)

**Fixes Applied:**
- `metric_type` changed from `string?` to `string` (DB is NOT NULL)
- `current_value` changed from `decimal` to `decimal?` (DB is NULLABLE)

**RLS:** Organization isolation.

---

## procohere.metric_values

**Purpose**  
Historical metric value recordings over time.

**Columns**
| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| id | uuid | NO | PK |
| organization_id | uuid | NO | FK → organizations |
| metric_id | uuid | NO | FK → metrics |
| recorded_by | uuid | YES | FK → team_members |
| value | numeric | NO | |
| recorded_at | timestamptz | NO | |
| notes | text | YES | |
| is_deleted | boolean | NO | |
| created_at | timestamptz | NO | |
| updated_at | timestamptz | NO | |
| deleted_at | timestamptz | YES | |
| deleted_by | uuid | YES | |

**Model:** `MetricHistoryEntry.cs` ✅ Verified match

**RLS:** Organization isolation.
