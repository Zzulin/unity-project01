---
name: unity-inspector
description: "Unity Inspector design advisor. Use when users want better SerializeField usage, Tooltip/Header organization, validation, CreateAssetMenu, RequireComponent, or cleaner authoring UX in the Inspector. Triggers: Inspector, SerializeField, Tooltip, Header, Range, OnValidate, RequireComponent, CreateAssetMenu, 序列化, 检视面板."
---

# Unity Inspector Design

Use this skill when scripts need to be easier to author, configure, and review in the Inspector.

## Default Rules

- Prefer `private` fields with `[SerializeField]` over unnecessary public fields.
- Use `[Header]`, `[Tooltip]`, `[Space]`, `[Range]`, `[Min]`, `[TextArea]` when they clarify authoring intent.
- Use `[RequireComponent]` for mandatory sibling dependencies.
- Use `[CreateAssetMenu]` for config/data assets that designers should create directly.
- Use `OnValidate` only for lightweight editor-time validation and normalization.
- Use `SerializeReference` only when polymorphic serialized data is genuinely needed.

## Inspector Quality Checklist

- Are defaults safe?
- Are required references obvious?
- Are fields grouped by responsibility?
- Are tuning values constrained?
- Are debug-only fields separated from authoring fields?
- Will another person understand this script from the Inspector alone?

## Output Format

- Field exposure strategy
- Recommended attributes
- Validation rules
- Authoring UX improvements
- Over-design to avoid
