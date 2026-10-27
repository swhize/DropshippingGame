# Compile-Check ohne Unity

- `tools/compilecheck/setup.sh` – einmalig: dotnet, Unity-6.3-DLLs (GameCI-Image-Layer), Input System 1.20.
- `tools/compilecheck/build.sh [repo]` – kompiliert Core, Runtime und Editor gegen die echten Unity-DLLs (URP über `URPStub.cs`)
  sowie die PlayMode-Tests (`Tests/PlayMode`, Selbsttest im Test Runner) gegen NUnit aus dem NuGet-Cache und eine
  Nachbildung des Test-Frameworks (`TestFrameworkStub.cs`) → `PLAYMODE OK`. Fehlt NUnit, einmal `dotnet test …` ausführen.
- `dotnet test tools/compilecheck/Tests.csproj` – NUnit-Tests der Spiellogik.

Dieser Ordner liegt außerhalb von `Assets/` und wird von Unity ignoriert.
