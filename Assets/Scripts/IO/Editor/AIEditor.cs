using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AI))]
public class AIEditor : Editor {
	[DrawGizmo(GizmoType.NotInSelectionHierarchy | GizmoType.InSelectionHierarchy | GizmoType.Pickable)]
	static void RenderCustomGizmos(AI aiData, GizmoType gizmoType) {
		for (int i = 0; i < aiData.segments.Count; i++) {
			AI.AISegment aiSegment = aiData.segments[i];

			if (gizmoType == GizmoType.NotInSelectionHierarchy) {
				Gizmos.color = Color.green;
				Gizmos.DrawSphere(aiSegment.left, .2f);
				Gizmos.color = Color.red;
				Gizmos.DrawSphere(aiSegment.right, .2f);
			}
			Gizmos.color = Color.yellow;
			Gizmos.DrawLine(aiSegment.left, aiSegment.right);
			
			for (int c = 0; c < 2; c++) {
				int connection = aiSegment.connectedIndexes[c];
				if (connection == -1)
					continue;

				Vector3 leftMiddle = Vector3.Lerp(aiSegment.left, aiData.segments[connection].left, .5f);
				Vector3 rightMiddle = Vector3.Lerp(aiSegment.right, aiData.segments[connection].right, .5f);
				Vector3 middleSegmentRight = (rightMiddle - leftMiddle).normalized;
				Vector3 middleSegmentForward = (Vector3.Lerp(aiData.segments[connection].left, aiData.segments[connection].right, .5f) - Vector3.Lerp(aiSegment.left, aiSegment.right, .5f)).normalized;

				Gizmos.color = Color.green;
				Gizmos.DrawLine(aiSegment.left, aiData.segments[connection].left);
				Gizmos.DrawLine(leftMiddle, leftMiddle - (middleSegmentForward + middleSegmentRight) * .5f);
				Gizmos.DrawLine(leftMiddle, leftMiddle - (middleSegmentForward - middleSegmentRight) * .5f);
				Gizmos.DrawLine(leftMiddle - (middleSegmentForward + middleSegmentRight) * .5f, leftMiddle - (middleSegmentForward - middleSegmentRight) * .5f);
				if ((aiSegment.walls & AI.AISegment.Wall.Left) > 0) {
					Gizmos.color = new Color(1, .5f, .75f);
					Gizmos.DrawLine(aiSegment.left + Vector3.up, aiData.segments[connection].left + Vector3.up);
				}

				Gizmos.color = Color.red;
				Gizmos.DrawLine(aiSegment.right, aiData.segments[connection].right);
				Gizmos.DrawLine(rightMiddle, rightMiddle - (middleSegmentForward - middleSegmentRight) * .5f);
				Gizmos.DrawLine(rightMiddle, rightMiddle - (middleSegmentForward + middleSegmentRight) * .5f);
				Gizmos.DrawLine(rightMiddle - (middleSegmentForward + middleSegmentRight) * .5f, rightMiddle - (middleSegmentForward - middleSegmentRight) * .5f);
				if ((aiSegment.walls & AI.AISegment.Wall.Right) > 0) {
					Gizmos.color = new Color(.5f, 1, .75f);
					Gizmos.DrawLine(aiSegment.right + Vector3.up, aiData.segments[connection].right + Vector3.up);
				}

				Gizmos.color = Color.white;
				Gizmos.DrawLine(Vector3.Lerp(aiSegment.left, aiSegment.right, aiSegment.raceLine), Vector3.Lerp(aiData.segments[connection].left, aiData.segments[connection].right, aiData.segments[connection].raceLine));
				Gizmos.color = Color.magenta;
				Gizmos.DrawLine(Vector3.Lerp(aiSegment.left, aiSegment.right, aiSegment.altRaceLine), Vector3.Lerp(aiData.segments[connection].left, aiData.segments[connection].right, aiData.segments[connection].altRaceLine));
			}
		}
	}

	AI aiData;
	enum DragMode {
		Axis,
		Project
	}
	DragMode dragMode = DragMode.Project;

	int selectedSegmentIndex = -1;
	enum Side {
		Left,
		Right
	}
	Side selectedSide;

	bool altMode, altModeLock;

	Event e;

	void OnEnable() {
		aiData = target as AI;
		Tools.hidden = true;
	}

	void OnDisable() {
		Tools.hidden = false;
	}

	public override void OnInspectorGUI() {
		if (selectedSegmentIndex >= aiData.segments.Count)
			selectedSegmentIndex = -1;

		if (GUILayout.Button("Import...")) {
			EditorUtility.SetDirty(aiData);
			Undo.RecordObject(aiData, "Load AI");

			aiData.Import();
		}

		GUI.enabled = false;
		EditorGUILayout.TextField("Path", aiData.path);
		GUI.enabled = true;

		using (new GUILayout.HorizontalScope()) {
			if (GUILayout.Button("Reload", EditorStyles.miniButtonLeft)) {
				EditorUtility.SetDirty(aiData);
				Undo.RecordObject(aiData, "Load AI");

				aiData.Load();
			}

			if (GUILayout.Button("Save", EditorStyles.miniButtonMid)) {
				aiData.Save();
			}

			if (GUILayout.Button("Export...", EditorStyles.miniButtonRight)) {
				aiData.Export();
			}
		}

		dragMode = (DragMode)EditorGUILayout.EnumPopup("Drag Mode", dragMode);

		EditorGUILayout.Space();

		if (selectedSegmentIndex != -1) {
			EditorGUILayout.LabelField(String.Format("Segment Info [{0}]", selectedSegmentIndex), EditorStyles.boldLabel);
			EditorGUI.indentLevel++;

			AI.AISegment selectedSegment = aiData.segments[selectedSegmentIndex];

			float newRaceLineValue = EditorGUILayout.Slider("Race Line Position", selectedSegment.raceLine, 0, 1);
			float newAltRaceLineValue = EditorGUILayout.Slider("Race Line Position", selectedSegment.altRaceLine, 0, 1);
			if (newRaceLineValue != selectedSegment.raceLine || newAltRaceLineValue != selectedSegment.altRaceLine) {
				EditorUtility.SetDirty(aiData);
				Undo.RecordObject(aiData, "Move Racing Line");

				selectedSegment.raceLine = newRaceLineValue;
				selectedSegment.altRaceLine = newAltRaceLineValue;
			}

			AI.AISegment.Wall newWalls = (AI.AISegment.Wall)EditorGUILayout.EnumFlagsField("Walls", selectedSegment.walls);
			if (newWalls != selectedSegment.walls) {
				EditorUtility.SetDirty(aiData);
				Undo.RecordObject(aiData, "Toggle Wall");

				selectedSegment.walls = newWalls;
			}

			AI.AISegment.Priority newPriority = (AI.AISegment.Priority)EditorGUILayout.EnumPopup("Priority", selectedSegment.priority);
			if (newPriority != selectedSegment.priority) {
				EditorUtility.SetDirty(aiData);
				Undo.RecordObject(aiData, "Change Priority");

				selectedSegment.priority = newPriority;
			}

			EditorGUILayout.Space();

			EditorGUILayout.LabelField("Speeds", EditorStyles.boldLabel);

			int newRaceSpeed = EditorGUILayout.IntSlider("Race Speed", selectedSegment.raceSpeed, 0, 30);
			int newCenterSpeed = EditorGUILayout.IntSlider("Center Speed", selectedSegment.centerSpeed, 0, 30);
			int newLeftSpeed = EditorGUILayout.IntSlider("Left Speed", selectedSegment.leftSpeed, 0, 30);
			int newRightSpeed = EditorGUILayout.IntSlider("Right Speed", selectedSegment.rightSpeed, 0, 30);
			if (newRaceSpeed != selectedSegment.raceSpeed || newCenterSpeed != selectedSegment.centerSpeed || newLeftSpeed != selectedSegment.leftSpeed || newRightSpeed != selectedSegment.rightSpeed) {
				EditorUtility.SetDirty(aiData);
				Undo.RecordObject(aiData, "Change Speed");

				selectedSegment.raceSpeed = newRaceSpeed;
				selectedSegment.centerSpeed = newCenterSpeed;
				selectedSegment.leftSpeed = newLeftSpeed;
				selectedSegment.rightSpeed = newRightSpeed;
			}

			EditorGUI.indentLevel--;
			aiData.segments[selectedSegmentIndex] = selectedSegment;
		}
	}

	void OnSceneGUI() {
		e = Event.current;

		switch (e.type) {
			case EventType.KeyDown:
				if (e.keyCode == KeyCode.Delete) {
					DeleteSegment(selectedSegmentIndex);
					e.Use();
				}
				break;
			case EventType.MouseUp:
				altMode = e.alt;
				altModeLock = false;
				break;
		}

		if (!altModeLock)
			altMode = e.alt;

		for (int i = 0; i < aiData.segments.Count; i++) {
			DoSegment(i);
		}

		if (e.control) {
			Ray r = HandleUtility.GUIPointToWorldRay(e.mousePosition);

			GUIUtility.GetControlID(SurfaceHandle.hash, FocusType.Keyboard);

			int controlId = GUIUtility.GetControlID(SurfaceHandle.hash, FocusType.Keyboard);
			RaycastHit mouseHit;
			if (Physics.Raycast(r, out mouseHit)) {
				switch (e.type) {
					case EventType.MouseDown:
						if (e.button == 0 || e.button == 1) {
							EditorUtility.SetDirty(aiData);
							Undo.RecordObject(aiData, "Add Segment");

							aiData.segments.Add(new AI.AISegment {
								left = mouseHit.point,
								leftSpeed = 30,
								right = mouseHit.point,
								rightSpeed = 30,
								raceLine = .5f,
								altRaceLine = .5f,
								raceSpeed = 30,
								centerSpeed = 30,
								connectedIndexes = new int[]{ -1, -1 }
							});

							if (e.button == 1) {
								ChangeSegmentConnection(aiData.segments.Count - 1);
								GUIUtility.hotControl = GUIUtility.keyboardControl = controlId;
							}
						}
						break;
				}
				Handles.color = Color.white;
				SurfaceHandle.Do(controlId, mouseHit.point, .5f);
			}
		}
	}

	void DoSegment(int segmentIndex) {
		Handles.color = Color.green;
		DoPositionHandle(segmentIndex, Side.Left);
		Handles.color = Color.red;
		DoPositionHandle(segmentIndex, Side.Right);

		DoMidSegment(segmentIndex);

		if (segmentIndex == selectedSegmentIndex) {
			AI.AISegment segment = aiData.segments[segmentIndex];

			int controlId = GUIUtility.GetControlID(FocusType.Keyboard);
			if (HandleUtility.nearestControl == controlId) {
				switch (e.type) {
					case EventType.MouseDown:
						if (altMode)
							altModeLock = true;
						break;
				}
			}

			float raceLineValue;
			if (altMode) {
				raceLineValue = segment.altRaceLine;
				Handles.color = Color.magenta;
			} else {
				raceLineValue = segment.raceLine;
				Handles.color = Color.white;
			}

			Vector3 raceLinePoint = Vector3.Lerp(segment.left, segment.right, raceLineValue);
			Vector3 slideDirection = segment.right - segment.left;

			float handleScale = HandleUtility.GetHandleSize(raceLinePoint);
			Vector3 newRaceLinePoint = Handles.Slider(controlId, raceLinePoint, slideDirection, handleScale * .05f, Handles.DotHandleCap, .1f);

			if (raceLinePoint != newRaceLinePoint) {
				EditorUtility.SetDirty(aiData);
				Undo.RecordObject(aiData, "Move Racing Line");

				float newRaceLineValue = Mathf.Clamp01(Vector3.Dot(newRaceLinePoint - segment.left, slideDirection) / slideDirection.sqrMagnitude);

				if (altMode)
					segment.altRaceLine = newRaceLineValue;
				else
					segment.raceLine = newRaceLineValue;
			}
			aiData.segments[segmentIndex] = segment;
		}
	}

	private void DoMidSegment(int segmentIndex) {
		Handles.color = new Color(1, 1, 1, .2f);
		AI.AISegment segment = aiData.segments[segmentIndex];

		for (int c = 0; c < 2; c++) {
			int connection = segment.connectedIndexes[c];
			if (connection == -1)
				continue;
			if (selectedSegmentIndex == segmentIndex || connection == selectedSegmentIndex) {
				Vector3 leftMiddle = Vector3.Lerp(segment.left, aiData.segments[connection].left, .5f);
				Vector3 rightMiddle = Vector3.Lerp(segment.right, aiData.segments[connection].right, .5f);
				Handles.DrawDottedLine(leftMiddle, rightMiddle, 4);

				Vector3 middle = Vector3.Lerp(leftMiddle, rightMiddle, .5f);
				float handleScale = HandleUtility.GetHandleSize(middle);
				if (Handles.Button(middle, Quaternion.identity, handleScale * .03f, handleScale * .05f, Handles.DotHandleCap)) {
					EditorUtility.SetDirty(aiData);
					Undo.RecordObject(aiData, "Insert Segment");

					segment.connectedIndexes[c] = aiData.segments.Count;

					aiData.segments.Add(new AI.AISegment() {
						left = leftMiddle,
						leftSpeed = (int)Mathf.Lerp(segment.leftSpeed, aiData.segments[connection].leftSpeed, .5f),
						right = rightMiddle,
						rightSpeed = (int)Mathf.Lerp(segment.rightSpeed, aiData.segments[connection].rightSpeed, .5f),
						raceLine = Mathf.Lerp(segment.raceLine, aiData.segments[connection].raceLine, .5f),
						altRaceLine = Mathf.Lerp(segment.altRaceLine, aiData.segments[connection].altRaceLine, .5f),
						raceSpeed = (int)Mathf.Lerp(segment.raceSpeed, aiData.segments[connection].raceSpeed, .5f),
						centerSpeed = (int)Mathf.Lerp(segment.centerSpeed, aiData.segments[connection].centerSpeed, .5f),
						connectedIndexes = new int[2] { connection, -1 }
					});
				}
			}
		}
	}

	void DoPositionHandle(int segementIndex, Side side) {
		Vector3 originalPosition = new Vector3();
		AI.AISegment segment = aiData.segments[segementIndex];
		switch (side) {
			case Side.Left:
				originalPosition = segment.left;
				break;
			case Side.Right:
				originalPosition = segment.right;
				break;
		}

		Vector3 newPosition = originalPosition;
		if (dragMode == DragMode.Project) {
			int controlId = GUIUtility.GetControlID(SurfaceHandle.hash, FocusType.Keyboard);
			if (GUIUtility.keyboardControl == controlId) {
				switch (e.type) {
					case EventType.MouseUp:
						OnSelect(segementIndex, side);
						break;
				}
			} else if (HandleUtility.nearestControl == controlId) {
				switch (e.type) {
					case EventType.MouseDown:
						if (e.button == 1 && selectedSegmentIndex != segementIndex && selectedSegmentIndex != -1) {
							ChangeSegmentConnection(segementIndex);

							e.Use();
						}
						break;
				}
			}
			newPosition = SurfaceHandle.Do(controlId, newPosition, .5f);
		} else {
			if (selectedSegmentIndex == segementIndex && selectedSide == side) {
				newPosition = Handles.PositionHandle(newPosition, Quaternion.identity);
			} else {
				float handleScale = HandleUtility.GetHandleSize(originalPosition);
				if (Handles.Button(originalPosition, Quaternion.identity, handleScale * .03f, handleScale * .05f, Handles.DotHandleCap)) {
					OnSelect(segementIndex, side);
				}
			}
		}

		if (newPosition != originalPosition) {
			EditorUtility.SetDirty(aiData);
			Undo.RecordObject(aiData, "Move Side Position");

			switch (side) {
				case Side.Left:
					segment.left = newPosition;
					break;
				case Side.Right:
					segment.right = newPosition;
					break;
			}
			aiData.segments[segementIndex] = segment;
		}
	}

	private void OnSelect(int segementIndex, Side side) {
		if (altMode) {
			EditorUtility.SetDirty(aiData);
			Undo.RecordObject(aiData, "Toggle Wall");

			AI.AISegment segment = aiData.segments[segementIndex];

			segment.walls ^= side == Side.Left ? AI.AISegment.Wall.Left : AI.AISegment.Wall.Right;

			aiData.segments[segementIndex] = segment;
		} else {
			selectedSegmentIndex = segementIndex;
			selectedSide = side;
			Repaint();
		}
	}

	private void ChangeSegmentConnection(int segementIndex) {
		if (segementIndex == selectedSegmentIndex || selectedSegmentIndex == -1)
			return;

		EditorUtility.SetDirty(aiData);
		Undo.RecordObject(aiData, "Change Segment Connection");

		AI.AISegment selectedSegment = aiData.segments[selectedSegmentIndex];
		if (aiData.segments[segementIndex].connectedIndexes[0] == selectedSegmentIndex) {
			aiData.segments[segementIndex].connectedIndexes[0] = -1;
		} else if (aiData.segments[segementIndex].connectedIndexes[1] == selectedSegmentIndex) {
			aiData.segments[segementIndex].connectedIndexes[1] = -1;
		} else if (selectedSegment.connectedIndexes[0] == segementIndex) {
			selectedSegment.connectedIndexes[0] = -1;
		} else if (selectedSegment.connectedIndexes[1] == segementIndex) {
			selectedSegment.connectedIndexes[1] = -1;
		} else if (selectedSegment.connectedIndexes[0] == -1) {
			selectedSegment.connectedIndexes[0] = segementIndex;
		} else if (selectedSegment.connectedIndexes[1] == -1) {
			selectedSegment.connectedIndexes[1] = segementIndex;
		}
	}

	private void DeleteSegment(int segementIndex) {
		if (segementIndex == -1)
			return;

		EditorUtility.SetDirty(aiData);
		Undo.RecordObject(aiData, "Delete Segment");

		for (int i = 0; i < aiData.segments.Count; i++) {
			AI.AISegment previousSegment = aiData.segments[i];
			if (previousSegment.connectedIndexes[0] == segementIndex)
				previousSegment.connectedIndexes[0] = aiData.segments[segementIndex].connectedIndexes[0];
			if (previousSegment.connectedIndexes[1] == segementIndex)
				previousSegment.connectedIndexes[1] = aiData.segments[segementIndex].connectedIndexes[0];
			aiData.segments[i] = previousSegment;
		}

		aiData.segments.RemoveAt(segementIndex);

		for (int i = 0; i < aiData.segments.Count; i++) {
			AI.AISegment segment = aiData.segments[i];
			if (segment.connectedIndexes[0] > segementIndex)
				segment.connectedIndexes[0]--;
			if (segment.connectedIndexes[1] > segementIndex)
				segment.connectedIndexes[1]--;
			aiData.segments[i] = segment;
		}
	}
}