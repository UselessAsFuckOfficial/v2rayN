# AGENTS.md

Repository-specific notes for agents working on v2rayN.

## Layout

- `v2rayN/ServiceLib` — shared logic, ViewModels, Handlers, models. No UI dependencies.
- `v2rayN/v2rayN` — WPF app (`net10.0-windows`). Views are XAML + code-behind.
- `v2rayN/v2rayN.Desktop` — Avalonia app. Views are `.axaml` + code-behind.
- `v2rayN/ServiceLib.Tests` — TUnit tests.

Both UIs share the same `ServiceLib` ViewModels and Handlers, so a change there must
work for WPF and Avalonia alike.

## Build

The WPF project targets Windows; on Linux it needs `-p:EnableWindowsTargeting=true`.

```bash
dotnet build v2rayN/v2rayN.csproj -c Debug -p:EnableWindowsTargeting=true
dotnet build v2rayN/v2rayN.Desktop.csproj -c Debug
```

If the container has no `libicu` (and you cannot install it), the CLI aborts with
"Couldn't find a valid ICU package". Work around it with:

```bash
export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
```

## Test

```bash
dotnet run --project v2rayN/ServiceLib.Tests/ServiceLib.Tests.csproj -c Debug
```

TUnit filter syntax:

```bash
dotnet run --project v2rayN/ServiceLib.Tests/ServiceLib.Tests.csproj -c Debug -- \
  --treenode-filter "/*/*/WorkflowHandlerTests/*"
```

## Binding convention (WPF)

`DataContext` is **never** set to the ViewModel. `WindowBase<TViewModel>` inherits
ReactiveUI's `ReactiveWindow<TViewModel>`, and every ViewModel binding is wired in
code-behind with `this.Bind(...)` / `this.OneWayBind(...)` / `this.BindCommand(...)`.

XAML `{Binding ...}` therefore resolves against the **row item** inside `DataGrid`
templates (e.g. `{Binding Remarks}`), or against a named element. A binding such as
`{Binding DataContext.SomeList, RelativeSource={RelativeSource AncestorType=DataGrid}}`
silently yields null and renders an empty control — no error, nothing in the log.

To populate a dropdown that lives in a `DataGrid` cell, expose the choices on the row
item (an editor-only `[JsonIgnore]` property on the DTO) and bind to that directly.

## XAML resources

`StaticResource` keys that do not exist are **not** compile errors; they throw at
window load, so the app builds cleanly and the window fails to open. Check that a key
exists before using it (MaterialDesignThemes 5.3.2 ships `MaterialDesignComboBox`,
`MaterialDesignDataGridComboBox`, `MaterialDesignDataGrid`; it does not ship
`MaterialDesignFilledComboBox` or `MaterialDesignOutlinedComboBox`).

Local styles are defined in `v2rayN/App.xaml` (`DefComboBox`, `DefButton`,
`DefDataGrid`, ...). Reuse these rather than hand-rolling per-window templates.

## Editing a DataGrid cell in place

Cell editors need `UpdateSourceTrigger=PropertyChanged` to write back as the user
types or selects.
