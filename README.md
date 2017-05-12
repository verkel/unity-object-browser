https://github.com/verkel/unity-object-browser

## Synopsis

ObjectBrowser is a debugging tool that can show values of monitored objects' fields during execution of a Unity3D program. The tool will render the values using GUILayout.* UI controls, and can be flexibly positioned where the developer wants.

## Code Example

Add instances to monitor during program initialization:

ObjectBrowser.instance.Add(instanceToMonitor1);
ObjectBrowser.instance.Add(instanceToMonitor2);

Render the UI with some MonoBehaviour:

public void OnGUI() {
  ObjectBrowser.instance.DrawGui();
}

## Contributors

jaakko.lindvall@iki.fi

## License

Public domain.