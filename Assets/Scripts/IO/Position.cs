using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class Position : MonoBehaviour {
	public string path;

	public int finishLineIndex;
	[Serializable]
	public struct PositionNode {
		public Vector3 position;
		public int[] connectedIndexes;
	}

	public List<PositionNode> nodes;

	public void Import() {
		path = EditorUtility.OpenFilePanel("Import Position", "", "pan");
		Load();
	}

	public void Load() {
		byte[] data = File.ReadAllBytes(path);
		const int offset = 0x0C;

		int length = BitConverter.ToInt32(data, 0);
		finishLineIndex = BitConverter.ToInt32(data, 0x04);

		nodes = new List<PositionNode>(length);
		for (int i = 0; i < length; i++) {
			int dp = i * 0x30 + offset;

			float x = BitConverter.ToSingle(data, dp);
			float y = BitConverter.ToSingle(data, dp + 0x04);
			float z = BitConverter.ToSingle(data, dp + 0x08);
			Vector3 position = new Vector3(x / 100, -y / 100, z / 100);

			int[] indexes = new int[4];
			for (int c = 0; c < 4; c++) {
				indexes[c] = BitConverter.ToInt32(data, dp + c * 0x04 + 0x20);
			}

			nodes.Add(new PositionNode {
				position = position,
				connectedIndexes = indexes
			});
		}
	}

	public void Export() {
		FileInfo fi = new FileInfo(path);
		path = EditorUtility.SaveFilePanel("Export Position", fi.DirectoryName, fi.Name, "pan");
		Save();
	}

	public void Save() {
		if (string.IsNullOrEmpty(path))
			return;

		const int offset = 0x0C;

		int byteLength = offset + 0x30 * nodes.Count;
		byte[] data = new byte[byteLength];

		BitConverter.GetBytes(nodes.Count).CopyTo(data, 0);
		BitConverter.GetBytes(finishLineIndex).CopyTo(data, 0x04);

		// TODO: Calculate largest path distance for overall track

		for (int i = 0; i < nodes.Count; i++) { // Fill reverse segment connections with null index
			int dp = i * 0x30 + offset;
			for (int c = 0; c < 4; c++) {
				BitConverter.GetBytes(-1).CopyTo(data, dp + c * 0x04 + 0x10);
			}
		}

		for (int i = 0; i < nodes.Count; i++) {
			PositionNode node = nodes[i];
			int dp = i * 0x30 + offset;

			BitConverter.GetBytes(node.position.x * 100).CopyTo(data, dp);
			BitConverter.GetBytes(node.position.y * -100).CopyTo(data, dp + 0x04);
			BitConverter.GetBytes(node.position.z * 100).CopyTo(data, dp + 0x08);

			// TODO: Calculate largest path distance per node

			for (int c = 0; c < 4; c++) {
				BitConverter.GetBytes(node.connectedIndexes[c]).CopyTo(data, dp + c * 0x04 + 0x20);
				if (node.connectedIndexes[c] != -1) {
					int dpc = node.connectedIndexes[c] * 0x30 + offset;
					for (int cc = 0; cc < 4; c++) {
						if (BitConverter.ToInt32(data, dpc + cc * 0x04 + 0x10) == -1) {
							BitConverter.GetBytes(i).CopyTo(data, dpc + cc * 0x04 + 0x10);
							break;
						}
					}
				}
			}
		}

		File.WriteAllBytes(path, data);
	}
}