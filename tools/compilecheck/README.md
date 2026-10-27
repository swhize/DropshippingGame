# Compile-Check ohne Unity

- `tools/compilecheck/setup.sh` – einmalig: dotnet, Unity-6.3-DLLs (GameCI-Image-Layer), Input System 1.20.
- `tools/compilecheck/build.sh [repo]` – kompiliert Core, Runtime und Editor gegen die echten Unity-DLLs (URP über `URPStub.cs`).
- `dotnet test tools/compilecheck/Tests.csproj` – NUnit-Tests der Spiellogik.

Dieser Ordner liegt außerhalb von `Assets/` und wird von Unity ignoriert.
