# BARREL RIVALS — ZERO-ALLOCATION CODING RULES

1. Zero allocations in Update/FixedUpdate loops (no LINQ, no foreach on non-lists, no string concatenation).
2. All serialized fields must be private with `[SerializeField]` and public read-only property getters.
3. Event subscriptions must have matching unsubscriptions in OnDisable/OnDestroy.
4. Structs passed by readonly ref where applicable.
5. All ScriptableObjects inherit from `GameData` and validate via `OnValidate()`.
