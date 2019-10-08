# unity3d-editor-menu

Fluent style API for constructing custom editor menus presented using `GenericMenu` by
default although alternative presentation can be configured for project.

```sh
$ yarn add rotorz/unity3d-editor-menu
```

This package is compatible with the [unity3d-package-syncer][tool] tool. Refer to the
tools' [README][tool] for information on syncing packages into a Unity project.

[tool]: https://github.com/rotorz/unity3d-package-syncer


## Usage Example

Here is a basic example of building an editor menu:

```csharp
var menu = new EditorMenu();

menu.AddCommand("Open in Designer...")
    .Enabled(selection is IDesignable)
    .Action(this.OnContextMenu_OpenInDesigner);

menu.AddSeparator();

menu.AddCommand("Duplicate")
    .Action(this.OnContextMenu_Duplicate);
menu.AddCommand("Delete")
    .Action(this.OnContextMenu_Delete);

menu.ShowAsContext();
```


Selections can be shown by checking (ticking / selecting) the menu command; for example:

```csharp
if (EditorGUI.DropdownButton(new GUIContent("Menu Button"), FocusType.Keyboard)) {
    var menu = new EditorMenu();

    menu.AddCommand("Show Grid")
        .Checked(this.ShowGrid)
        .Action(() => this.ShowGrid = !this.ShowGrid);

    menu.ShowAsDropdown(GUILayoutUtility.GetLastRect());
}
```


## Setting up a Custom Presentation

By default editor menus are presented using the Unity `GenericMenu` API although this can
be overridden on a per-instance or project-wide basis if desired.

To override the default presentation create a JSON settings file at the path illustrated
below and specify the implementation type that you would like to use in your project:

**/Assets/Plugins/PackageData/@rotorz/unity3d-editor-menu/EditorMenuSettings.json**
```json
{
  "DefaultPresenterTypeName": "MyNamespace.CustomEditorMenuPresenter"
}
```

Packages that provide alternative editor menu presentations should not attempt to override
the setting automatically; the end-user should make a conscious effort to opt-in.


## Contribution Agreement

This project is licensed under the MIT license (see LICENSE). To be in the best
position to enforce these licenses the copyright status of this project needs to
be as simple as possible. To achieve this the following terms and conditions
must be met:

- All contributed content (including but not limited to source code, text,
  image, videos, bug reports, suggestions, ideas, etc.) must be the
  contributors own work.

- The contributor disclaims all copyright and accepts that their contributed
  content will be released to the public domain.

- The act of submitting a contribution indicates that the contributor agrees
  with this agreement. This includes (but is not limited to) pull requests, issues,
  tickets, e-mails, newsgroups, blogs, forums, etc.
