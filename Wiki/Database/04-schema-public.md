# 04 – Public Schema (Functional Specification)

This document is the **functional specification** for the `public` schema used by ProCohere.

It defines:
- The purpose and boundaries of the `public` schema
- Each table that lives in `public`
- The relationships and invariants enforced there
- How `public` participates in security, tenancy, and identity
- What the `public` schema is explicitly **not allowed** to contain

This document must be read before reviewing any product-domain tables.

---

## 0. Role of the `public` Schema

The `public` schema is **cross-product infrastructure**.

It exists to support:
- Authentication integration
- Organization tenancy
- Licensing and entitlement
- Seat assignment and product access
- Cross-product user metadata

The `public` schema is **not** ProCohere-specific.
It may be shared by multiple products in the future.

---

## 1. Non-Negotiable Rules for `public`

The following rules are mandatory.

### 1.1 No Product Domain Logic

The `public` schema must not contain:
- Goals, meetings, tasks, metrics, reviews
- Product-specific ownership or visibility logic
- Product-specific hierarchy logic
- Product-specific activity feeds or audit trails

If a table exists only because ProCohere exists, it does not belong in `public`.

---

### 1.2 Organization Is Still the Hard Boundary

Even though `public` is cross-product:

- Every tenant-aware table must still reference an organization
- Cross-organization access must be prevented by RLS where applicable
- Identity resolution must still fail closed

---

## 2. Table: `organizations`

### 2.1 Purpose

Represents a tenant organization.

An organization is the **root tenancy unit** for all product data.

---

### 2.2 Core Invariants

- Each organization represents exactly one tenant.
- Organization rows must never be shared across tenants.
- Deleting an organization is an exceptional, controlled operation.

---

### 2.3 Key Columns (Conceptual)

- `id` – primary key
- `name` – display name
- `slug` – stable external identifier
- lifecycle fields (`created_at`, `updated_at`, soft delete fields if applicable)

Exact column definitions are provided in the table reference.

---

### 2.4 Relationships

- Referenced by all product-domain tables via `organization_id`
- Referenced by internal users
- Referenced by licensing and seat tables

---

### 2.5 Security Considerations

- Organization rows must not be readable cross-tenant by application-visible roles.
- RLS must enforce that users only see their own organization.
- Administrative access must be explicit.

---

## 3. Table: `users` (Internal Users)

### 3.1 Purpose

Represents ProCohere’s **internal user record**.

This table bridges Supabase authentication (`auth.users`) to:
- organizations
- products
- team members

---

### 3.2 Core Invariants

- Each internal user row binds an auth identity to one organization.
- An auth identity must not have multiple active internal user rows.
- Internal users may be soft-deleted without deleting auth identity history.

---

### 3.3 Key Columns (Conceptual)

- `id` – primary key
- `auth_user_id` – references `auth.users.id`
- `organization_id` – tenant context
- lifecycle fields (`is_deleted`, timestamps)

---

### 3.4 Relationships

- Referenced by `procohere.team_members` via `linked_user_id`
- Referenced by audit fields (`created_by`, `deleted_by`) in some tables

---

### 3.5 Security and RLS

- RLS must ensure users only see their own internal user row.
- Insert/update must validate organization consistency.
- This table is foundational to identity resolution.

---

## 4. Table: `organization_products`

### 4.1 Purpose

Tracks which products an organization is entitled to use.

This table represents **licensing**, not usage.

---

### 4.2 Core Invariants

- Each row links one organization to one product.
- A product may be enabled or disabled per organization.
- Historical licensing changes must be auditable.

---

### 4.3 Key Columns (Conceptual)

- `organization_id`
- `product_code` or product identifier
- licensing status fields
- billing metadata

---

### 4.4 Relationships

- Referenced by seat assignment tables
- Consulted during authorization checks at the application layer

---

### 4.5 Security Considerations

- Organizations must only see their own product entitlements.
- Administrative roles may manage licensing.

---

## 5. Table: `user_product_seats`

### 5.1 Purpose

Represents **seat assignment** for a given product within an organization.

This table answers:
- Who is allowed to use which product
- In what role

---

### 5.2 Core Invariants

- Seats are always scoped to an organization and product.
- A user may have at most one active seat per product per organization.
- Seat assignment may be revoked via soft delete.

---

### 5.3 Key Columns (Conceptual)

- `organization_id`
- `user_id` (internal user)
- `product_code`
- `role` or access level
- lifecycle fields

---

### 5.4 Relationships

- References `public.users`
- References `organization_products`

---

### 5.5 Security and RLS

- RLS must prevent users from seeing other users’ seat assignments unless explicitly allowed.
- Seat management operations must validate organization and product consistency.

---

## 6. GRANTS and RLS in `public`

### 6.1 GRANTS Philosophy

GRANTS in `public` are generally conservative.

Rules:

- `anon` should have minimal access.
- `authenticated` may have limited read access to self-scoped rows.
- `service_role` may have broader access for operational needs.

---

### 6.2 RLS Usage

Not all `public` tables require RLS, but when tenant data exists:

- RLS must enforce organization scoping
- Identity resolution must still fail closed
- Soft deletes must be respected where applicable

---

## 7. What Does Not Belong in `public`

Explicitly forbidden:

- ProCohere meetings, goals, tasks, metrics
- Visibility or hierarchy rules
- Product-specific activity or audit logs
- Product-specific configuration tables

If such a table exists, it must be moved to `procohere`.

---

## 8. Change Control Requirements

Any change to the `public` schema must include:

- Validation that the table is cross-product in nature
- Updated documentation in this file
- Updated RLS and GRANTS if tenant-scoped data is involved
- Impact analysis on identity and session resolution

---

## 9. Why This Matters

The `public` schema is foundational.

Mistakes here:
- Break identity resolution
- Break tenant isolation
- Affect all products, not just ProCohere

Changes must be conservative, explicit, and well-documented.

---

**Next:** `05-schema-procohere.md`
