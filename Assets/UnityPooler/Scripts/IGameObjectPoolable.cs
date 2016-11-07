namespace UnityPooler
{
	/// <summary>
	/// Components that implement this interface will have functions invoked when created and reused.
	/// </summary>
	public interface IGameObjectPoolable
	{
		/// <summary>
		/// This function is invoked when the GameObject is created.
		/// </summary>
		void OnObjecteCreated();

		/// <summary>
		/// This function is invoked when the GameObject is reused.
		/// </summary>
		void OnObjectReused();
	}
}
