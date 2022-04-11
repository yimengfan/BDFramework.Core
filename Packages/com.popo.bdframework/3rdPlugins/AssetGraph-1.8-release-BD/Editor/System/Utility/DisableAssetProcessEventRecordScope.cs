using System;

namespace UnityEngine.AssetGraph {
	public sealed class DisableAssetProcessEventRecordScope : IDisposable
	{
		private bool m_Disposed;
		private readonly bool m_oldState;

		public DisableAssetProcessEventRecordScope ()
		{
			m_oldState = AssetProcessEventRecord.GetRecord().EnabledRecording;
			AssetProcessEventRecord.GetRecord().EnabledRecording = false;
		}
		
		~DisableAssetProcessEventRecordScope()
		{
			if (m_Disposed)
				return;
			Debug.LogError("Scope was not disposed! You should use the 'using' keyword or manually call Dispose.");
		}

		public void Dispose()
		{
			if (m_Disposed)
			{
				return;
			}
			m_Disposed = true;
			AssetProcessEventRecord.GetRecord().EnabledRecording = m_oldState;
		}
	}
}
