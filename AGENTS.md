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

## Sort order lives on ProfileExItem

`Sort` is a column of `ProfileExItem`, **not** `ProfileItem`. `AppManager.ProfileModels`
selects neither `Sort` nor an `ORDER BY`, so a `ProfileItemModel` returned from it has
`Sort == 0` for every row.

Consequences:

- `lstModel.OrderBy(t => t.Sort).FirstOrDefault()` is a no-op that returns whichever row
  the database happened to emit first, not the topmost row the user sees. Read the value
  with `ProfileExManager.Instance.GetSort(indexId)` instead.
- `ProfilesViewModel.GetProfileItemsEx` joins `ProfileExItem` to restore `Sort` for the
  grid; the raw `ProfileModels` result is not display-ordered.
- `SortServers` assigns `(i + 1) * 10` so later inserts can land between rows. Never
  assume the values are contiguous.

## dotnet SDK in this container

The SDK is not preinstalled. Install it once with:

```bash
curl -sSL -o /tmp/dotnet-install.sh https://dot.net/v1/dotnet-install.sh
chmod +x /tmp/dotnet-install.sh
/tmp/dotnet-install.sh --channel 10.0 --install-dir "$HOME/.dotnet"
```

Then prefix commands with `export PATH="$HOME/.dotnet:$PATH"`.
