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
- Checks Prefix/Postfix/Finalizer returns, named and indexed arguments, `ref`/`out`,
  `__instance`, `__result`, `__args`, `__originalMethod`, `__runOriginal`,
  `__exception`, paired `__state` values and named field injections.
- Checks reverse-patch argument order, instance slots and return types.

| Diagnostic | Meaning |
| --- | --- |
| SHV000 | Validator or metadata resolution failed |
| SHV001 | Missing target type or method name |
| SHV002 | Target method or overload not found |
| SHV003 | Multiple overloads match |
| SHV004 | Invalid patch signature or injection |
| SHV101 | Dynamic selector was not evaluated |
| SHV102 | Annotation is not supported yet |
| SHV103 | Signature or conversion could not be verified statically |

The checker validates metadata, not runtime behavior. It validates only the game
DLLs used by the current build; use the intended Valheim version in CI.

Transpilers, argument remapping annotations, delegate injections, ref-return
injections, numeric field ordering and complex generic/array conversions remain
unverified. Non-identical reference storage and reverse-patch types are also
reported conservatively when compatibility cannot be established. Warnings must
not be interpreted as successful signature validation.

Rules follow the installed HarmonyX injection implementation and the
[Harmony injection documentation](https://harmony.pardeike.net/articles/patching-injections.html).

The tool requires the .NET 8 runtime or a compatible installed runtime and SDK.
