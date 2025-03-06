using ADOFAI.Editor.Actions;

namespace EditorTweaks.ADOFAI.EditorActions
{
	public class ZoomOutCameraEditorActionEx : EditorAction
	{
		// Token: 0x170005AD RID: 1453
		// (get) Token: 0x06002071 RID: 8305 RVA: 0x000E63E1 File Offset: 0x000E45E1
		public override EditorTabKey sectionKey
		{
			get
			{
				return EditorTabKey.EditorWorkflow;
			}
		}

		// Token: 0x06002072 RID: 8306 RVA: 0x000E63E4 File Offset: 0x000E45E4
		public override void Execute(scnEditor editor)
		{
			editor.ZoomCamera(-0.01f, false, false);
		}
	}
}
