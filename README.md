# AGIQ Open Tools

Open-source companion utilities for **AGIQ Solver Enterprise**:

- **AGIQ.CNF** — a simple DIMACS CNF builder SDK for .NET
- **AGIQ.HexAssignmentConverter** — a CLI tool for converting **HEX ↔ variable assignments**

Commercial solver page:  
https://principium.pro/agiq-solver-enterprise/

---

## Why this repository exists

AGIQ Solver Enterprise focuses on high-performance optimization and SAT/MaxSAT-style workloads on GPU.

This repository provides two practical developer tools around that workflow:

1. **Build CNF models programmatically**
2. **Convert solver assignments between HEX and human-readable `x1 = 0/1` format**

These tools are intentionally lightweight, easy to audit, and convenient to integrate into your own pipelines.

---

# Projects

## 1. AGIQ.CNF

A small .NET library for generating **DIMACS CNF** files.

### Features

- Create named variables
- Add clauses programmatically
- Add common constraints:
  - `AddOr(...)`
  - `AddImplication(a, b)`
  - `AddEquivalence(a, b)`
  - `AddAtLeastOne(...)`
  - `AddAtMostOne(...)`
  - `AddExactlyOne(...)`
- Save directly to DIMACS CNF

### Example

```csharp
using AGIQ.CNF;

var model = new CnfModel();

var x1 = model.Var("x1");
var x2 = model.Var("x2");
var x3 = model.Var("x3");
var x4 = model.Var("x4");

var workers = model.Vars("worker", 5);

model.AddOr(x1, x2);
model.AddImplication(x3, x4);
model.AddExactlyOne(workers);

model.Save("task.cnf");
