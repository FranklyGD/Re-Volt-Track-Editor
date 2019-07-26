using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Position))]
public class PositionEditor : Editor {
	[DrawGizmo(GizmoType.NotInSelectionHierarchy | GizmoType.InSelectionHierarchy | GizmoType.Pickable)]
	static void RenderCustomGizmos(Position positionData, GizmoType gizmoType) {
		for (int i = 0; i < positionData.nodes.Count; i++) {
			Position.PositionNode positionNode = positionData.nodes[i];

			Vector3 position = positionData.nodes[i].position;
			if (gizmoType == GizmoType.NotInSelectionHierarchy) {
				Gizmos.color = i == positionData.finishLineIndex ? Color.red : Color.green;
				Gizmos.DrawSphere(position, .2f);
			}

			for (int c = 0; c < 4; c++) {
				int connection = positionNode.connectedIndexes[c];
				if (connection == -1)
					continue;

				Vector3 mid = position * .5f + positionData.nodes[connection].position * .5f;
				Gizmos.color = Color.black;
				Gizmos.DrawLine(mid, position);
				Gizmos.color = Color.cyan;
				Gizmos.DrawLine(mid, positionData.nodes[connection].position);
			}
		}
	}

	Position positionData;
	enum DragMode {
		Axis,
		Project
	}
	DragMode dragMode = DragMode.Project;

	int selectedNodeIndex = -1;

	Event e;

	void OnEnable() {
		positionData = target as Position;
		Tools.hidden = true;
	}

	void OnDisable() {
		Tools.hidden = false;
	}

	public override void OnInspectorGUI() {
		if (GUILayout.Button("Import...")) {
			EditorUtility.SetDirty(positionData);
			Undo.RecordObject(positionData, "Load Positions");

			positionData.Import();
		}

		GUI.enabled = false;
		EditorGUILayout.TextField("Path", positionData.path);
		GUI.enabled = true;

		using (new GUILayout.HorizontalScope()) {
			if (GUILayout.Button("Reload", EditorStyles.miniButtonLeft)) {
				EditorUtility.SetDirty(positionData);
				Undo.RecordObject(positionData, "Load Positions");

				positionData.Load();
			}

			if (GUILayout.Button("Save", EditorStyles.miniButtonMid)) {
				positionData.Save();
			}

			if (GUILayout.Button("Export...", EditorStyles.miniButtonRight)) {
				positionData.Export();
			}
		}

		dragMode = (DragMode)EditorGUILayout.EnumPopup("Drag Mode", dragMode);

		EditorGUILayout.Space();

		if (selectedNodeIndex != -1) {
			EditorGUILayout.LabelField(String.Format("Node Info [{0}]", selectedNodeIndex), EditorStyles.boldLabel);
			EditorGUI.indentLevel++;

			bool selectedIsFinishLine = selectedNodeIndex == positionData.finishLineIndex;
			GUI.enabled = !selectedIsFinishLine;
			bool setFinishLine = EditorGUILayout.Toggle("Start/Finish Line", selectedIsFinishLine);
			if (setFinishLine != selectedIsFinishLine) {
				EditorUtility.SetDirty(positionData);
				Undo.RecordObject(positionData, "Change Start/Finish Line");

				positionData.finishLineIndex = selectedNodeIndex;
			}

			EditorGUI.indentLevel--;
			GUI.enabled = true;
		}
	}

	void OnSceneGUI() {
		e = Event.current;

		switch (e.type) {
			case EventType.KeyDown:
				if (e.keyCode == KeyCode.Delete) {
					DeleteNode(selectedNodeIndex);
					e.Use();
				}
				break;
		}

		for (int nodeIndex = 0; nodeIndex < positionData.nodes.Count; nodeIndex++) {
			DoNode(nodeIndex);
		}

		if (e.control) {
			Ray r = HandleUtility.GUIPointToWorldRay(e.mousePosition);
			
			int controlId = GUIUtility.GetControlID(SurfaceHandle.hash, FocusType.Keyboard);
			RaycastHit mouseHit;
			if (Physics.Raycast(r, out mouseHit)) {
				switch (e.type) {
					case EventType.MouseDown:
						if (e.button == 0 || e.button == 1) {
							EditorUtility.SetDirty(positionData);
							Undo.RecordObject(positionData, "Add Node");

							positionData.nodes.Add(new Position.PositionNode {
								position = mouseHit.point,
								connectedIndexes = new int[] { -1, -1, -1, -1 }
							});

							if (e.button == 1) {
								ChangeNodeConnection(positionData.nodes.Count - 1);
								GUIUtility.hotControl = GUIUtility.keyboardControl = controlId;
							}
						}
						break;
				}
				Handles.color = Color.white;
				SurfaceHandle.Do(mouseHit.point, .5f);
			}
		}

	}

	private void ChangeNodeConnection(int nodeIndex) {
		if (nodeIndex == selectedNodeIndex || selectedNodeIndex == -1)
			return;

		EditorUtility.SetDirty(positionData);
		Undo.RecordObject(positionData, "Change Node Connection");

		Position.PositionNode selectedNode = positionData.nodes[selectedNodeIndex];
		bool used = false;
		for (int c = 0; c < 4; c++) {
			if (positionData.nodes[nodeIndex].connectedIndexes[c] == selectedNodeIndex) {
				positionData.nodes[nodeIndex].connectedIndexes[c] = -1;
				used = true;
				break;
			}
		}

		if (!used) {
			for (int c = 0; c < 4; c++) {
				if (selectedNode.connectedIndexes[c] == nodeIndex) {
					selectedNode.connectedIndexes[c] = -1;
					used = true;
					break;
				}
			}
		}

		if (!used) {
			for (int c = 0; c < 4; c++) {
				if (selectedNode.connectedIndexes[c] == -1) {
					selectedNode.connectedIndexes[c] = nodeIndex;
					break;
				}
			}
		}
	}

	private void DeleteNode(int nodeIndex) {
		if (nodeIndex == -1)
			return;

		EditorUtility.SetDirty(positionData);
		Undo.RecordObject(positionData, "Delete Node");

		for (int i = 0; i < positionData.nodes.Count; i++) {
			Position.PositionNode previousNode = positionData.nodes[i];
			for (int c = 0; c < 4; c++) {
				if (previousNode.connectedIndexes[c] == nodeIndex)
					previousNode.connectedIndexes[c] = positionData.nodes[nodeIndex].connectedIndexes[0];
			}
			positionData.nodes[i] = previousNode;
		}

		positionData.nodes.RemoveAt(nodeIndex);

		for (int i = 0; i < positionData.nodes.Count; i++) {
			Position.PositionNode node = positionData.nodes[i];
			for (int c = 0; c < 4; c++) {
				if (node.connectedIndexes[c] > nodeIndex)
					node.connectedIndexes[c]--;
			}
			positionData.nodes[i] = node;
		}
	}

	private void DoNode(int nodeIndex) {
		Position.PositionNode positionNode = positionData.nodes[nodeIndex];
		Vector3 newPosition = positionNode.position;

		DoMidPoint(nodeIndex);

		Handles.color = Color.green;
		if (dragMode == DragMode.Project) {
			int controlId = GUIUtility.GetControlID(SurfaceHandle.hash, FocusType.Keyboard);
			if (GUIUtility.keyboardControl == controlId) {
				switch (e.type) {
					case EventType.MouseUp:
						OnSelect(nodeIndex);
						break;
				}
			} else if (HandleUtility.nearestControl == controlId) {
				switch (e.type) {
					case EventType.MouseDown:
						if (e.button == 1 && selectedNodeIndex != nodeIndex && selectedNodeIndex != -1) {
							ChangeNodeConnection(nodeIndex);

							e.Use();
						}
						break;
				}
			}
			newPosition = SurfaceHandle.Do(controlId, newPosition, nodeIndex == positionData.finishLineIndex ? 1.5f : .5f);
		} else {
			if (selectedNodeIndex == nodeIndex) {
				newPosition = Handles.PositionHandle(newPosition, Quaternion.identity);
			} else {
				float handleScale = HandleUtility.GetHandleSize(newPosition);
				if (Handles.Button(positionNode.position, Quaternion.identity, handleScale * .03f, handleScale * .05f, Handles.DotHandleCap)) {
					OnSelect(nodeIndex);
				}
			}
		}

		if (newPosition != positionNode.position) {
			EditorUtility.SetDirty(positionData);
			Undo.RecordObject(positionData, "Move Position");

			positionNode.position = newPosition;
		}

		positionData.nodes[nodeIndex] = positionNode;
	}

	private void DoMidPoint(int nodeIndex) {
		Handles.color = new Color(1, 1, 1, .2f);
		Position.PositionNode node = positionData.nodes[nodeIndex];
	
		for (int c = 0; c < 4; c++) {
			int connection = node.connectedIndexes[c];
			if (connection == -1)
				continue;
			if (selectedNodeIndex == nodeIndex || connection == selectedNodeIndex) {
				Position.PositionNode otherNode = positionData.nodes[connection];

				Vector3 middle = Vector3.Lerp(node.position, otherNode.position, .5f);
				Vector3 connectionDirection = otherNode.position - node.position;
				Vector3 viewDirection = middle - SceneView.currentDrawingSceneView.camera.transform.position;
				Vector3 cross = Vector3.Cross(connectionDirection, viewDirection).normalized;

				Handles.DrawLine(middle + cross, middle - cross);

				float handleScale = HandleUtility.GetHandleSize(middle);
				if (Handles.Button(middle, Quaternion.identity, handleScale * .03f, handleScale * .05f, Handles.DotHandleCap)) {
					EditorUtility.SetDirty(positionData);
					Undo.RecordObject(positionData, "Insert Node");

					node.connectedIndexes[c] = positionData.nodes.Count;

					positionData.nodes.Add(new Position.PositionNode() {
						position = middle,
						connectedIndexes = new int[4] { connection, -1, -1, -1 }
					});
				}
			}
		}

		positionData.nodes[nodeIndex] = node;
	}

	private void OnSelect(int nodeIndex) {
		selectedNodeIndex = nodeIndex;
		Repaint();
	}
}