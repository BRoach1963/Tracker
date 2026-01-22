# ProCohere Database – Wiki Index

This folder contains the **authoritative database specification** for ProCohere.

The database is the security and correctness boundary.
Anything not documented here is undefined behavior.

---

## Structure

01 Architecture Overview  
02 Security Model and RLS  
03 Session and Identity  
04 Public Schema  
05 ProCohere Schema  

06 Tables (Authoritative Table Dictionary)  
07 Functions Reference  
08 Indexes and Constraints  
09 Triggers  
10 Grants and ACLs  
11 RLS Policy Reference  
12 Developer Guidance  

13 Schema DDL Reference (Generated Snapshot)  
14 Hierarchy Model  

---

## Reading Order

New engineers:
1 → 2 → 3 → 6

Schema or security changes:
6 → 7 → 11 → 12

---

## Conceptual vs Physical

- Conceptual truth lives in 06–12
- Physical truth lives in 13
- Hierarchy mechanics live in 14

Undocumented changes are defects.
