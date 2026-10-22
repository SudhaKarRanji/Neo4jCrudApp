# Neo4j CRUD — WinForms (.NET Framework 4.8)

A single-screen WinForms app that does Create / Read / Update / Delete against a
`:Person {name, age, city}` node in Neo4j. Everything a form-based CRUD screen
needs, nothing more.

## Layout

```
Neo4jCrudApp.sln
Neo4jCrudApp/
  Neo4jCrudApp.csproj     # net48, UseWindowsForms=true, Neo4j.Driver via NuGet
  App.config               # connection settings — edit this first
  Program.cs                # [STAThread] entry point
  MainForm.cs / MainForm.Designer.cs   # the UI + button handlers
  Models/Person.cs
  Data/PersonRepository.cs   # the 4 CRUD methods — this is the part to reuse elsewhere
```

## Prerequisites

1. Neo4j running (Desktop, or):
   ```bash
   docker run -d --name neo4j -p 7474:7474 -p 7687:7687 \
     -e NEO4J_AUTH=neo4j/your-password-here neo4j:5
   ```
2. Edit `App.config` → `Neo4jUri` / `Neo4jUser` / `Neo4jPassword`.
3. Visual Studio 2019+ (needs the ".NET desktop development" workload for
   WinForms + `net48`).

## Run

Open `Neo4jCrudApp.sln` in Visual Studio and press F5 — or:
```bash
cd Neo4jCrudApp
dotnet restore
dotnet run
```

## How the screen works

- **Top row** — Name / Age / City inputs.
- **Create** — `MERGE`s a `:Person` by name (safe to click twice).
- Click a row in the grid → the form fills in; **Update Selected** writes
  changes (including a renamed `Name`) back by matching the *original* name.
- **Delete Selected** — confirms, then `DETACH DELETE`s the selected node.
- **Refresh** — re-runs the `MATCH (p:Person) RETURN ...` read and repopulates
  the grid.
- **Clear Form** — resets the inputs and grid selection without touching data.

All four operations go through `Data/PersonRepository.cs`, which uses
`session.ExecuteWriteAsync` / `ExecuteReadAsync` (managed transactions —
auto-retry on transient errors) rather than raw `session.RunAsync`. Button
handlers are `async void`, which is the one place that's fine for async code
in WinForms, since the event dispatcher owns the exception anyway.

## Extending it

- Add more node properties → extend `Person`, the Cypher in
  `PersonRepository`, and the grid columns in `MainForm`'s constructor.
- Want relationships (e.g. `Person -[:LIVES_IN]-> City` as a real node
  instead of a string property)? That's the natural next step once this
  basic screen feels familiar — happy to extend it if useful.
