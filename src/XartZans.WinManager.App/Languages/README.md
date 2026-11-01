# Adding or removing interface languages

Language catalogs are UTF-8 JSON files in:

`%LOCALAPPDATA%\XartZans\WinManager\Languages`

Open this folder with **Open language files folder** in the language menu. Add a
JSON file such as `es.json`; remove a catalog file to remove that language from
the menu. Reopen the language menu to reload the list. Keep at least one valid
language file so the app has a catalog to display.

Use this format:

```json
{
  "code": "es",
  "name": "Español",
  "strings": {
    "WindowTitle": "XartZan's WinManager",
    "NavDashboard": "Panel",
    "DashboardTitle": "Panel",
    "HardwareTitle": "Hardware y sistema"
  }
}
```

The file name without `.json` must match `code`. `name` is shown in the language
menu. `strings` maps the application's string keys to translated text. Missing
keys fall back to English, so a new translation can start small and grow over
time. Catalog files contain text only; they cannot execute code.

The complete set of current keys is in `tr.json` and `en.json` in the app's
packaged `Languages` folder.
