namespace EditorTweaks.Patch.Timeline
{
	public struct VerticalLineData
	{
		public int id;
		public float x;
		public FloorNumberLine obj;

		public VerticalLineData(int i, float posX, FloorNumberLine line)
		{
			id = i;
			x = posX;
			obj = line;
		}
	}
}
