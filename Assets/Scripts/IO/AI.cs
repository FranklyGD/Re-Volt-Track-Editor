using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class AI : MonoBehaviour {
	public string path;

	[Serializable]
	public struct AISegment {
		public bool start;
		public Vector3 left;
		public Vector3 right;

		public int[] connectedIndexes;

		public int leftSpeed;
		public int rightSpeed;
		public int raceSpeed;
		public int centerSpeed;

		[Range(0, 1)] public float raceLine;
		[Range(0, 1)] public float altRaceLine;

		public enum Priority {
			RacingLine,
			PickupRoute,
			Stairs,
			Bumpy,
			SlowDown25MPH,
			SoftSuspension,
			JumpWall,
			TitleScreen,
			TurboLine,
			LongPickupRoute,
			ShortCut,
			LongCut,
			BarrelBlock,
			OffThrottle,
			PetrolThrottle,
			Wilderness,
			SlowDown15MPH,
			SlowDown20MPH,
			SlowDown30MPH
		}
		public Priority priority;

		[Flags]
		public enum Wall {
			None,
			Left = 1,
			Right = 2
		}
		public Wall walls;
	}
	public List<AISegment> segments;

	public void Import() {
		path = EditorUtility.OpenFilePanel("Import AI", "", "fan");
		Load();
	}

	public void Load() {
		byte[] data = File.ReadAllBytes(path);
		const int offset = 0x04;

		short length = BitConverter.ToInt16(data, 0);

		segments = new List<AISegment>(length);
		for (int i = 0; i < length; i++) {
			int dp = i * 0x4C + offset;

			AISegment.Priority priority = (AISegment.Priority)data[dp];
			bool start = BitConverter.ToBoolean(data, dp + 0x01);
			AISegment.Wall walls = (AISegment.Wall)data[dp + 0x02];

			float raceLinePosition = BitConverter.ToSingle(data, dp + 0x04);
			float altRaceLinePosition = BitConverter.ToSingle(data, dp + 0x0C);
			int raceSpeed = BitConverter.ToInt32(data, dp + 0x14);
			int centerSpeed = BitConverter.ToInt32(data, dp + 0x18);

			int[] indexes = new int[2];
			indexes[0] = BitConverter.ToInt16(data, dp + 0x24);
			indexes[1] = BitConverter.ToInt16(data, dp + 0x28);

			int rightSpeed = BitConverter.ToInt32(data, dp + 0x2C);
			float rx = BitConverter.ToSingle(data, dp + 0x30);
			float ry = BitConverter.ToSingle(data, dp + 0x34);
			float rz = BitConverter.ToSingle(data, dp + 0x38);
			Vector3 rightPosition = new Vector3(rx / 100, -ry / 100, rz / 100);

			int leftSpeed = BitConverter.ToInt32(data, dp + 0x3C);
			float lx = BitConverter.ToSingle(data, dp + 0x40);
			float ly = BitConverter.ToSingle(data, dp + 0x44);
			float lz = BitConverter.ToSingle(data, dp + 0x48);
			Vector3 leftPosition = new Vector3(lx / 100, -ly / 100, lz / 100);

			segments.Add(new AISegment {
				priority = priority,
				start = start,
				walls = walls,
				raceSpeed = raceSpeed,
				centerSpeed = centerSpeed,
				raceLine = raceLinePosition,
				altRaceLine = altRaceLinePosition,
				rightSpeed = rightSpeed,
				right = rightPosition,
				leftSpeed = leftSpeed,
				left = leftPosition,
				connectedIndexes = indexes,
			});
		}
	}

	public void Export() {
		if (String.IsNullOrEmpty(path)) {
			path = EditorUtility.SaveFilePanel("Export AI", "", "", "fan");
		} else {
			FileInfo fi = new FileInfo(path);
			path = EditorUtility.SaveFilePanel("Export AI", fi.DirectoryName, fi.Name, "fan");
		}
		Save();
	}

	public void Save() {
		if (string.IsNullOrEmpty(path))
			return;

		const int offset = 0x04;

		int byteLength = offset + 0x4C * segments.Count;
		byte[] data = new byte[byteLength];

		BitConverter.GetBytes((short)segments.Count).CopyTo(data, 0);

		for (int i = 0; i < segments.Count; i++) { // Fill reverse node connections with null index
			int dp = i * 0x4C + offset;

			BitConverter.GetBytes(-1).CopyTo(data, dp + 0x1C);
			BitConverter.GetBytes(-1).CopyTo(data, dp + 0x20);
		}

		for (int i = 0; i < segments.Count; i++) {
			AISegment segment = segments[i];
			int dp = i * 0x4C + offset;

			data[dp] = (byte)segment.priority;
			if (segment.start)
				data[dp + 0x01]++;

			data[dp + 0x02] = data[dp + 0x03] = (byte)segment.walls; // Value is doubled in file for some reason, but just to be safe

			BitConverter.GetBytes(segment.raceLine).CopyTo(data, dp + 0x04);
			BitConverter.GetBytes(segment.altRaceLine).CopyTo(data, dp + 0x0C);
			BitConverter.GetBytes(segment.raceSpeed).CopyTo(data, dp + 0x14);
			BitConverter.GetBytes(segment.centerSpeed).CopyTo(data, dp + 0x18);

			if (segment.connectedIndexes[0] != -1) {
				int c0dp = segment.connectedIndexes[0] * 0x4C + offset;
				if (BitConverter.ToInt32(data, c0dp + 0x1C) == -1) {
					BitConverter.GetBytes(i).CopyTo(data, c0dp + 0x1C);
				} else {
					BitConverter.GetBytes(i).CopyTo(data, c0dp + 0x20);
				}
			}

			if (segment.connectedIndexes[1] != -1) {
				int c1dp = segment.connectedIndexes[1] * 0x4C + offset;
				if (BitConverter.ToInt32(data, c1dp + 0x1C) == -1) {
					BitConverter.GetBytes(i).CopyTo(data, c1dp + 0x1C);
				} else {
					BitConverter.GetBytes(i).CopyTo(data, c1dp + 0x20);
				}
			}

			BitConverter.GetBytes(segment.connectedIndexes[0]).CopyTo(data, dp + 0x24);
			BitConverter.GetBytes(segment.connectedIndexes[1]).CopyTo(data, dp + 0x28);

			BitConverter.GetBytes(segment.rightSpeed).CopyTo(data, dp + 0x2C);
			BitConverter.GetBytes(segment.right.x * 100).CopyTo(data, dp + 0x30);
			BitConverter.GetBytes(segment.right.y * -100).CopyTo(data, dp + 0x34);
			BitConverter.GetBytes(segment.right.z * 100).CopyTo(data, dp + 0x38);

			BitConverter.GetBytes(segment.leftSpeed).CopyTo(data, dp + 0x3C);
			BitConverter.GetBytes(segment.left.x * 100).CopyTo(data, dp + 0x40);
			BitConverter.GetBytes(segment.left.y * -100).CopyTo(data, dp + 0x44);
			BitConverter.GetBytes(segment.left.z * 100).CopyTo(data, dp + 0x48);
		}

		File.WriteAllBytes(path, data);
	}
}
