# Harmony target validation

All plugins in this repository run this check after compilation, before copying or merging
the DLL. The shared `Directory.Build.props` enables it for existing and future
plugins. It uses the build's resolved references and Mono.Cecil;
it does not load Unity, execute target selectors or install patches.

- Combines class and method annotations, including reverse patches.
- Resolves private methods, inherited methods and explicit overload signatures.
- Recognizes constructors, property accessors and reference/pointer arguments.
- Fails the build when metadata cannot be resolved or a target is missing or ambiguous.
- Reports dynamic selectors, generic target types and unsupported annotations as
  warnings, not verified targets.

| Diagnostic | Meaning |
| --- | --- |
| SHV000 | Validator or metadata resolution failed |
| SHV001 | Missing target type or method name |
| SHV002 | Target method or overload not found |
| SHV003 | Multiple overloads match |
| SHV101 | Dynamic selector was not evaluated |
| SHV102 | Annotation is not supported yet |

This first version checks targets, not Harmony parameter injection, reverse-patch
signature compatibility or runtime behavior. It validates only the game DLLs used
by the current build; use the intended Valheim version in CI.

The tool requires the .NET 8 runtime or a compatible installed runtime and SDK.
