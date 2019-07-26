using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class SurfaceHandle {
	public static readonly int hash = "SurfaceHandle".GetHashCode();

	public static Vector3 Do(Vector3 position, float size) {
		return Do(GUIUtility.GetControlID(hash, FocusType.Keyboard), position, size);
	}

	public static Vector3 Do(int controlId, Vector3 position, float size) {
		Event e = Event.current;
		bool selected = GUIUtility.hotControl == controlId || GUIUtility.keyboardControl == controlId;
		bool hovered = HandleUtility.nearestControl == controlId;

		switch (e.type) {
			case EventType.MouseDown:
				if (hovered && e.button == 0) {
					GUIUtility.hotControl = GUIUtility.keyboardControl = controlId;
					e.Use();
				}
				break;
			case EventType.MouseUp:
				if (GUIUtility.hotControl == controlId) {
					GUIUtility.hotControl = 0;
					e.Use();
				}
				break;
			case EventType.MouseDrag:
				if (selected) {
					Ray r = HandleUtility.GUIPointToWorldRay(e.mousePosition);

					RaycastHit mouseHit;
					if (Physics.Raycast(r, out mouseHit)) {
						position = mouseHit.point;
					}
				}
				break;
			case EventType.Repaint:
				Vector3 rayToPoint = position - SceneView.currentDrawingSceneView.camera.transform.position;

				bool onPoint = false;
				RaycastHit cameraHit;
				if (Physics.Raycast(SceneView.currentDrawingSceneView.camera.transform.position, rayToPoint, out cameraHit)) {
					onPoint = Vector3.Distance(cameraHit.point, position) < 0.0001f;
				}

				Color original = Handles.color;
				Color transparent = Handles.color;
				transparent.a /= 10;
				if (onPoint) {
					if (selected)
						Handles.color = Handles.selectedColor;
					else if (hovered)
						Handles.color = Handles.preselectionColor;

					Handles.DrawWireDisc(position, cameraHit.normal, size);
					Handles.color = transparent; 
					Handles.DrawSolidDisc(position, cameraHit.normal, size);

				} else {
					if (selected)
						Handles.color = Handles.selectedColor;
					else if (hovered)
						Handles.color = Handles.preselectionColor;
					else
						Handles.color = transparent;
					Handles.DrawSolidDisc(position, SceneView.currentDrawingSceneView.camera.transform.forward, size);
				}
				Handles.color = original;

				break;
			case EventType.Layout:
				float distance = HandleUtility.DistanceToCircle(position, size);
				HandleUtility.AddControl(controlId, distance);
				break;
		}

		return position;
	}
}
