# Re-Volt Track Editor

A Unity Addon Project  
*(Name subject to change for something more catchy)*

The aim is to help bring the editor out of the game's "`MAKEITGOOD`" mode and into a seperate application as an alternative track editing tool. The other reason is that the controls to create and edit tracks in the game as it stands is unintuitive as the controls are confusing.

***This is not a model editor, it only builds upon an existing track model by adding the necessary data.***

## Installing

You first need the Unity editor. You can download it from their [Hub app](https://store.unity.com/download) or [directly from their site](https://unity3d.com/get-unity/download/archive). Once you have downloaded and installed, create a 3D project. Delete everything in the default scene since you aren't exactly going to make a game here, just the track for Re-Volt. Take the all the files/folders from inside the "Assets" folder of the repo and drag them into the project.

Now you have all you need for the editor to work with.

## Initial Setup

If you are working from Blender to make the shapes of the track, you can directly drag in the `.blend` file into the project's "Assets" folder or into Unity's project file explorer.

Once the file is read as a model in Unity, drag the model into the scene, and then make sure it rests on origin (zero out the root position in the *Transform* component).

Add components to it via the *Inspector*: Position, AI, Track Zones (the order in which it is added do not matter)

> Now I don't know if it is me but after dragging it into the scene I had to rotate on the Y-axis by 180 degrees, you can check if everything is generally aligned by adding one Position point or AI segment, exporting it, and opening in the game's editor. But always make sure it lines up.

## Controls

*The control set is dependent on the currently expanded component in the inspector, make sure only one is expanded to avoid confusion.*

### Position Nodes Editor

* `Ctrl` + `Left Mouse` = Add new node
* `Left Mouse` on node = Select node
* `Left Mouse Drag` on node = Move node
* Select node + `Right Mouse` on another = Connect nodes (order matters)
* Select node + `Right Mouse` on connected node = Disconnect nodes
* `Left Mouse` on midpoint = Inserts a node inbetween
* `Delete` = Remove node (bridges connection if any)

### AI Segments Editor
*Shares controls with the position editor*
* `Ctrl` + `Left Mouse` = Add new segment
* `Left Mouse` on segment = Select segment
* `Left Mouse Drag` on segment = Move segment
* Select segment + `Right Mouse` on another = Connect segments (order matters)
* Select segment + `Right Mouse` on connected segment = Disconnect segments
* `Left Mouse` on midpoint = Inserts a segment inbetween
* `Delete` = Remove segment (bridges connection if any)

>

* Drag white dot = modifies race line
* Hold `Alt` = Changes to "Alt" mode
    * Allows modifying the overtaking line (Drag pink dot)
    * Clicking on nodes of the segment toggles a wall depending on the side

### Track Zones Editor

* `Ctrl` + `Left Mouse` = Add new zone
* `Left Mouse` on zone = Select zone
* `Keypad +` or `Keypad -` = Change the zone order
* `Left Mouse Drag` on dots on the side = Expand or contract zone on side

Some of the zone manipulation is based off of Unity's default transform tools (Position, Rotation, Scale) which can be found on the top left buttons. Use those to toggle between them.

---

Any extra properties can be found in the inspector depending on what is currently selected for any of these editors.

***The four different speeds in the AI editor (Left, Right, Racing, Center) do not really affect the final behavior of the AI cars, they are found in the exported save file from `MAKEITGOOD` but happen to serve no purpose.***

## Todo
* Use the track folder as like a project folder
* Load the visual model and collision model instead of using the `.blend` files and adding a *Mesh Collider* component in Unity
* Be able to load and edit track information from the `<trackname>.inf` file