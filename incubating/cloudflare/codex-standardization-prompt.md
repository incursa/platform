# Codex Prompt: Apply Incursa C# Standard To Another Repo

Use this prompt with Codex when targeting another repository under `C:\src\incursa`.

```md
Repository target:
<PUT-REPO-PATH-HERE>

Task:
Apply the Incursa C# repository style/analyzer standard from:
- C:\src\incursa\llm\csharp-repo-standard.md
- C:\src\incursa\llm\templates

Requirements:
1. Replace or align these files in the target repo with the standard templates:
   - .editorconfig
   - .gitattributes
   - stylecop.json
2. Add:
   - strongvalidation.core.props
   - strongvalidation.extended.props
3. Update Directory.Build.props:
   - add UseExtendedAnalyzers=false default
   - import strongvalidation.core.props for non-analyzer projects
   - import strongvalidation.extended.props only when UseExtendedAnalyzers=true
4. Keep repo-specific metadata intact (metadata.props/company/repository URL/package branding).
5. Keep line endings policy:
   - LF for text files
   - CRLF only for .cmd and .bat
6. Preserve existing test/common/analyzers import structure unless required to satisfy the standard.
7. Do not use destructive git commands.

Validation:
1. Show changed files list.
2. Report any conflicts found between existing config and standard.
3. Run non-mutating verification commands when feasible:
   - dotnet restore
   - dotnet build
4. Summarize any follow-up manual fixes needed (if any).

Output format:
1. Short summary of what was changed.
2. File-by-file notes with rationale.
3. Any risks or compatibility notes.
```
