using UnityEngine;
using UnityEditor;
using System;

[CustomEditor(typeof(TrackZone))]
public class TrackZoneEditor : Editor {
	TrackZone trackZoneData;

	enum ScaleDirection {
		Xp,
		Xn,
		Yp,
		Yn,
		Zp,
		Zn
	}

	int selectedZoneIndex = -1;

	void OnEnable() {
		trackZoneData = target as TrackZone;
		Tools.hidden = true;
	}

	void OnDisable() {
		Tools.hidden = false;
	}

	[DrawGizmo(GizmoType.NotInSelectionHierarchy | GizmoType.InSelectionHierarchy | GizmoType.Pickable)]
	static void RenderCustomGizmos(TrackZone trackZoneData, GizmoType gizmoType) {
		for (int i = 0; i < trackZoneData.zones.Count; i++) {
			TrackZone.Zone zone = trackZoneData.zones[i];
			Color color = GetZoneColor(zone);
			Gizmos.color = color;

			Gizmos.matrix = zone.transform;
			Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
			color.a = .25f;
			Gizmos.color = color;
			Gizmos.DrawCube(Vector3.zero, Vector3.one);
		}

	}

	private static Color GetZoneColor(TrackZone.Zone zone) {
		Color color;
		switch (zone.order % 6) {
			case 0:
				color = Color.red;
				break;
			case 1:
				color = Color.green;
				break;
			case 2:
				color = Color.blue;
				break;
			case 3:
				color = Color.yellow;
				break;
			case 4:
				color = Color.magenta;
				break;
			case 5:
				color = Color.cyan;
				break;
			default:
				color = Color.black;
				break;
		}

		return color;
	}

	public override void OnInspectorGUI() {
		if (GUILayout.Button("Import...")) {
			EditorUtility.SetDirty(trackZoneData);
			Undo.RecordObject(trackZoneData, "Load Track Zones");

			trackZoneData.Import();
		}

		GUI.enabled = false;
		EditorGUILayout.TextField("Path", trackZoneData.path);
		GUI.enabled = true;

		using (new GUILayout.HorizontalScope()) {
			if (GUILayout.Button("Reload", EditorStyles.miniButtonLeft)) {
				EditorUtility.SetDirty(trackZoneData);
				Undo.RecordObject(trackZoneData, "Load Track Zones");

				trackZoneData.Load();
			}

			if (GUILayout.Button("Save", EditorStyles.miniButtonMid)) {
				trackZoneData.Save();
			}

			if (GUILayout.Button("Export...", EditorStyles.miniButtonRight)) {
				trackZoneData.Export();
			}
		}

		EditorGUILayout.Space();

		if (selectedZoneIndex != -1) {
			TrackZone.Zone selectedZone = trackZoneData.zones[selectedZoneIndex];

			Vector3 newPosition = EditorGUILayout.Vector3Field("Position", selectedZone.position);
			if (newPosition != selectedZone.position) {
				EditorUtility.SetDirty(trackZoneData);
				Undo.RecordObject(trackZoneData, "Move Zone");

				selectedZone.position = newPosition;
			}

			Vector3 newRotation = EditorGUILayout.Vector3Field("Rotation", selectedZone.rotation.eulerAngles);
			if (newRotation != selectedZone.rotation.eulerAngles) {
				EditorUtility.SetDirty(trackZoneData);
				Undo.RecordObject(trackZoneData, "Rotate Zone");

				selectedZone.rotation = Quaternion.Euler(newRotation);
			}

			Vector3 newScale = EditorGUILayout.Vector3Field("Scale", selectedZone.scale);
			if (newScale != selectedZone.position) {
				EditorUtility.SetDirty(trackZoneData);
				Undo.RecordObject(trackZoneData, "Scale Zone");

				selectedZone.scale = newScale;
			}

			int newOrder = EditorGUILayout.IntField("Order", selectedZone.order);
			if (newOrder != selectedZone.order) {
				EditorUtility.SetDirty(trackZoneData);
				Undo.RecordObject(trackZoneData, "Change Zone Order");

				selectedZone.order = newOrder;
			}

			trackZoneData.zones[selectedZoneIndex] = selectedZone;
		}
	}

	private void OnSceneGUI() {
		Event e = Event.current;

		switch (e.type) {
			case EventType.KeyDown:
				if (e.keyCode == KeyCode.Delete) {
					DeleteNode(selectedZoneIndex);
					e.Use();
				} else if (e.keyCode == KeyCode.KeypadPlus) {
					if (selectedZoneIndex != -1) {
						TrackZone.Zone selectedZone = trackZoneData.zones[selectedZoneIndex];

						EditorUtility.SetDirty(trackZoneData);
						Undo.RecordObject(trackZoneData, "Change Zone Order");

						selectedZone.order++;
						trackZoneData.zones[selectedZoneIndex] = selectedZone;
						e.Use();
					}
				} else if (e.keyCode == KeyCode.KeypadMinus) {
					if (selectedZoneIndex != -1) {
						TrackZone.Zone selectedZone = trackZoneData.zones[selectedZoneIndex];

						EditorUtility.SetDirty(trackZoneData);
						Undo.RecordObject(trackZoneData, "Change Zone Order");

						selectedZone.order--;
						trackZoneData.zones[selectedZoneIndex] = selectedZone;
						e.Use();
					}
				}
				break;
		}

		for (int i = 0; i < trackZoneData.zones.Count; i++) {
			DoZone(i);
		}

		if (e.control) {
			Ray r = HandleUtility.GUIPointToWorldRay(e.mousePosition);

			int controlId = GUIUtility.GetControlID(SurfaceHandle.hash, FocusType.Keyboard);
			RaycastHit mouseHit;
			if (Physics.Raycast(r, out mouseHit)) {
				switch (e.type) {
					case EventType.MouseDown:
						if (e.button == 0 || e.button == 1) {
							EditorUtility.SetDirty(trackZoneData);
							Undo.RecordObject(trackZoneData, "Add Zone");

							selectedZoneIndex = trackZoneData.zones.Count;

							trackZoneData.zones.Add(new TrackZone.Zone {
								position = mouseHit.point,
								rotationMatrix = Matrix4x4.identity,
								scale = Vector3.one,
								order = 0,
							});
						}
						break;
				}
				Handles.color = Color.white;
				Handles.DrawWireCube(mouseHit.point, Vector3.one);
				Handles.color = new Color(1, 1, 1, .25f);
				Handles.DrawWireCube(mouseHit.point, Vector3.one);

				float handleSize = HandleUtility.GetHandleSize(mouseHit.point);
				Handles.Button(mouseHit.point, Quaternion.identity, handleSize * .05f, handleSize * .1f, Handles.DotHandleCap);
			}
		}

	}

	private void DeleteNode(int zoneIndex) {
		if (zoneIndex == -1)
			return;

		EditorUtility.SetDirty(trackZoneData);
		Undo.RecordObject(trackZoneData, "Delete Node");

		trackZoneData.zones.RemoveAt(zoneIndex);
	}

	private void DoZone(int zoneIndex) {
		TrackZone.Zone zone = trackZoneData.zones[zoneIndex];

		Handles.color = GetZoneColor(zone);
		if (zoneIndex == selectedZoneIndex) {
			Quaternion toolRotation = Tools.pivotRotation == PivotRotation.Local ? zone.rotation : Quaternion.identity;
			switch (Tools.current) {
				case Tool.Move:
					Vector3 newPosition = Handles.DoPositionHandle(zone.position, toolRotation);
					if (newPosition != zone.position) {
						EditorUtility.SetDirty(trackZoneData);
						Undo.RecordObject(trackZoneData, "Move Zone");

						zone.position = newPosition;
					}
					break;
				case Tool.Rotate:
					Quaternion newRotation = Handles.DoRotationHandle(zone.rotation, zone.position);
					if (newRotation != zone.rotation) {
						EditorUtility.SetDirty(trackZoneData);
						Undo.RecordObject(trackZoneData, "Rotate Zone");

						zone.rotation = newRotation;
					}
					break;
				case Tool.Scale:
					Vector3 newScale = Handles.DoScaleHandle(zone.scale, zone.position, zone.rotation, HandleUtility.GetHandleSize(zone.position));
					if (newScale != zone.scale) {
						EditorUtility.SetDirty(trackZoneData);
						Undo.RecordObject(trackZoneData, "Scale Zone");

						zone.scale = newScale;
					}
					break;
			}

			if (Tools.current != Tool.Scale) {
				DoScaleHandle(ref zone, ScaleDirection.Xp);
				DoScaleHandle(ref zone, ScaleDirection.Xn);
				DoScaleHandle(ref zone, ScaleDirection.Yp);
				DoScaleHandle(ref zone, ScaleDirection.Yn);
				DoScaleHandle(ref zone, ScaleDirection.Zp);
				DoScaleHandle(ref zone, ScaleDirection.Zn);
			}
		} else {
			float handleSize = HandleUtility.GetHandleSize(zone.position);
			if (Handles.Button(zone.position, Quaternion.identity, handleSize * .05f, handleSize * .1f, Handles.DotHandleCap)) {
				selectedZoneIndex = zoneIndex;
				Repaint();
			}
		}

		Handles.Label(zone.position, zone.order.ToString(), new GUIStyle(GUI.skin.label) { fontSize=24, fontStyle=FontStyle.Bold, normal = new GUIStyleState() { textColor = Handles.color} });

		trackZoneData.zones[zoneIndex] = zone;
	}

	private void DoScaleHandle(ref TrackZone.Zone zone, ScaleDirection direction) {
		Vector3 axis = Vector3.zero;
		switch (direction) {
			case ScaleDirection.Xp:
				axis = Vector3.right;
				break;
			case ScaleDirection.Xn:
				axis = Vector3.left;
				break;
			case ScaleDirection.Yp:
				axis = Vector3.up;
				break;
			case ScaleDirection.Yn:
				axis = Vector3.down;
				break;
			case ScaleDirection.Zp:
				axis = Vector3.forward;
				break;
			case ScaleDirection.Zn:
				axis = Vector3.back;
				break;
		}
		Vector3 globalAxis = zone.rotationMatrix.MultiplyPoint(axis);

		Vector3 handlePosition = zone.transform.MultiplyPoint(axis / 2);
		float handleSize = HandleUtility.GetHandleSize(handlePosition);

		Color originalColor = Handles.color;
		
		if (Vector3.Dot(globalAxis, handlePosition - SceneView.currentDrawingSceneView.camera.transform.position) > 0)
			Handles.color *= new Color(.25f, .25f, .25f);
		Vector3 newPosition = Handles.Slider(handlePosition, globalAxis, handleSize * .05f, Handles.DotHandleCap, 1);
		Handles.color = originalColor;

		if (newPosition != handlePosition) {
			EditorUtility.SetDirty(trackZoneData);
			Undo.RecordObject(trackZoneData, "Scale Zone");

			Vector3 otherSide = zone.transform.MultiplyPoint(-axis / 2);
			float size = Vector3.Distance(newPosition, otherSide);
			Vector3 scale = zone.scale;

			zone.position = Vector3.Lerp(newPosition, otherSide, .5f);
			switch (direction) {
				case ScaleDirection.Xp:
				case ScaleDirection.Xn:
					scale.x = size;
					break;
				case ScaleDirection.Yp:
				case ScaleDirection.Yn:
					scale.y = size;
					break;
				case ScaleDirection.Zp:
				case ScaleDirection.Zn:
					scale.z = size;
					break;
			}

			zone.scale = scale;
		}
	}
}