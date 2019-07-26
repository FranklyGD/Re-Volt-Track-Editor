using System.Collections;
using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using System.Collections.Generic;

public class TrackZone : MonoBehaviour {
	public string path;

	[Serializable]
	public struct Zone {
		public int order;
		public Vector3 position;
		public Matrix4x4 rotationMatrix;
		public Vector3 scale;

		public Matrix4x4 transform { get { return Matrix4x4.Translate(position) * rotationMatrix * Matrix4x4.Scale(scale); } }
		public Quaternion rotation {
			get { return rotationMatrix.rotation; }
			set { rotationMatrix = Matrix4x4.Rotate(value); }
		}
	}

	public List<Zone> zones;

	public void Import() {
		path = EditorUtility.OpenFilePanel("Import Track Zones", "", "taz");
		Load();
	}

	public void Load() {
		byte[] data = File.ReadAllBytes(path);
		const int offset = 0x04;

		int length = BitConverter.ToInt32(data, 0);

		zones = new List<Zone>(length);
		for (int i = 0; i < length; i++) {
			int dp = i * 0x40 + offset;

			int order = BitConverter.ToInt32(data, dp);

			float x = BitConverter.ToSingle(data, dp + 0x04);
			float y = BitConverter.ToSingle(data, dp + 0x08);
			float z = BitConverter.ToSingle(data, dp + 0x0C);
			Vector3 position = new Vector3(x / 100, -y / 100, z / 100);
			
			// Rotation stored is a 3x3 matrix
			Matrix4x4 rotation = new Matrix4x4(
				new Vector4(BitConverter.ToSingle(data, dp + 0x10), BitConverter.ToSingle(data, dp + 0x14), BitConverter.ToSingle(data, dp + 0x18), 0),
				new Vector4(BitConverter.ToSingle(data, dp + 0x1C), BitConverter.ToSingle(data, dp + 0x20), BitConverter.ToSingle(data, dp + 0x24), 0),
				new Vector4(BitConverter.ToSingle(data, dp + 0x28), BitConverter.ToSingle(data, dp + 0x2C), BitConverter.ToSingle(data, dp + 0x30), 0),
				new Vector4(0,0,0,1)
			);

			float sx = BitConverter.ToSingle(data, dp + 0x34);
			float sy = BitConverter.ToSingle(data, dp + 0x38);
			float sz = BitConverter.ToSingle(data, dp + 0x3C);
			Vector3 scale = new Vector3(sx / 50, sy / 50, sz / 50);

			zones.Add(new Zone {
				order = order,
				position = position,
				rotationMatrix = rotation,
				scale = scale
			});
		}
	}

	public void Export() {
		if (String.IsNullOrEmpty(path)) {
			path = EditorUtility.SaveFilePanel("Export Track Zones", "", "", "taz");
		} else {
			FileInfo fi = new FileInfo(path);
			path = EditorUtility.SaveFilePanel("Export Track Zones", fi.DirectoryName, fi.Name, "taz");
		}
		Save();
	}

	public void Save() {
		if (string.IsNullOrEmpty(path))
			return;

		const int offset = 0x04;

		int byteLength = offset + 0x40 * zones.Count;
		byte[] data = new byte[byteLength];

		BitConverter.GetBytes(zones.Count).CopyTo(data, 0);

		for (int i = 0; i < zones.Count; i++) {
			Zone zone = zones[i];
			int dp = i * 0x40 + offset;

			BitConverter.GetBytes(zone.order).CopyTo(data, dp);

			BitConverter.GetBytes(zone.position.x * 100).CopyTo(data, dp + 0x04);
			BitConverter.GetBytes(zone.position.y * -100).CopyTo(data, dp + 0x08);
			BitConverter.GetBytes(zone.position.z * 100).CopyTo(data, dp + 0x0C);

			for (int c = 0; c < 3; c++) {
				for (int r = 0; r < 3; r++) {
					BitConverter.GetBytes(zone.rotationMatrix[r, c]).CopyTo(data, dp + 0x10 + (c * 3 + r) * 4);
				}
			}

			BitConverter.GetBytes(zone.scale.x * 50).CopyTo(data, dp + 0x34);
			BitConverter.GetBytes(zone.scale.y * 50).CopyTo(data, dp + 0x38);
			BitConverter.GetBytes(zone.scale.z * 50).CopyTo(data, dp + 0x3C);
		}

		File.WriteAllBytes(path, data);
	}
}
