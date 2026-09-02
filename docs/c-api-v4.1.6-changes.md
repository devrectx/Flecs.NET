# C API migration map: flecs 4.0.4 → 4.1.6

Prepared during the flecs v4.1.6 low-level spike. This is the source of truth for
porting `Flecs.NET.Core` (Workstream 3).

Empirical data: `Flecs.NET.Bindings` + `Flecs.NET` (Core) compile errors after
regenerating bindings from `flecs v4.1.6` (with the Bindgen.NET fork fixes).

## Summary

- Total Core compile errors: **306** (153 unique lines reported per build pass;
  list was captured from a Debug build).
- **282/306 (92%)** are a single mechanical pattern: `ecs_ensure_id` gained an
  `size` parameter, breaking all `FetchPointers\T*.g.cs` generated helpers
  (T1..T16) plus 4 hand-written calls in `Entity.cs`.
- Remaining **24 errors** are ~17 unique spots across 6 hand-written files.

## 1. `ecs_ensure_id` signature change (mechanical, bulk)

```c
// 4.0.4
ecs_entity_t ecs_ensure_id(ecs_world_t *world, ecs_entity_t entity);
// 4.1.6
ecs_entity_t ecs_ensure_id(ecs_world_t *world, ecs_entity_t entity, size_t size);
```

- Fix source: `src/Flecs.NET.Codegen` template that emits `Generated/Ecs/FetchPointers/T*.g.cs`
  (add `sizeof(T)` argument), then re-run the code generator.
- Hand-written call sites to update:
  - `src/Flecs.NET/Core/Entity.cs` lines 2953, 3131, 3153, 3240
- `ecs_ensure` / `ecs_ensure_id` semantics unchanged otherwise.

## 2. Struct/field renames or removals (hand-written files)

| File | Line(s) | 4.0.4 member | 4.1.6 change |
|---|---|---|---|
| `Core/Ecs/Aliases.cs` | 181 | `EcsPrivate` | removed (component no longer exists) |
| `Core/Ecs/Aliases.cs` | 457 | `EcsUnion` | renamed/removed — verify 4.1.6 (pair/tag constants changed) |
| `Core/WorldInfo.cs` | 37, 42 | `min_id`, `max_id` | removed from `ecs_world_info_t` (see ranges) |
| `Core/WorldInfo.cs` | 142, 147 | `systems_ran_frame`, `observers_ran_frame` | renamed/removed — check new counters |
| `Core/Iter.cs` | 411 | `iter.group_id` | field removed/renamed in `ecs_iter_t` (see query cache rework) |
| `Core/IterIterable.cs` | 63, 85, 107 | `ecs_query_iter_t.query` | member gone — query cache rework (`query/cache/*`) |

## 3. Removed C functions (call sites in Core)

| File | Function | 4.1.6 replacement |
|---|---|---|
| `Core/World.cs` | `ecs_set_entity_range` | new entity range API (see ranges) |
| `Core/World.cs` | `ecs_enable_range_check` | new entity range API |

## 4. Function signature changes (script)

| File | Call | 4.1.6 |
|---|---|---|
| `Core/World.cs` 3214 | `ecs_script_run(world, name, code, result)` | now takes an out `ecs_script_eval_result_t*` result param |

## 5. Bindgen.NET fixes required (already in devrectx/Bindgen.NET fork)

- Upstream PR #4 (`GetTypeIdentifier`): function-pointer constant arrays got
  invalid struct names.
- `[InlineArray]` over `delegate*` is CS9184 → fork emits a struct with N
  explicit `delegate*` fields instead (`ecs_script_function_t`,
  `ecs_function_desc_t` have `vector_callbacks[18]`).
- `_iobuf` FILE pointers (zig win-gnu libc spelling) → `void*`.

## 6. New v4.1 capabilities observed (candidates for Core, additive)

- Entity range API (replaces `ecs_set_entity_range`).
- Non-fragmenting hierarchy storage (`EcsDontFragment`, `EcsOrderedChildren`,
  `EcsParentDepth`, `storage/non_fragmenting_childof.c`, `ordered_children.c`).
- New `ecs_script_eval_result_t` error reporting.
- `query/cache/*` rework (grouping/order-by/caching internals) — check
  `ecs_query_iter_t` API changes.
- Stats addon: `addons/stats/memory.c`.
- Script addon: vector functions + perlin math (`functions_math_perlin.c`).
