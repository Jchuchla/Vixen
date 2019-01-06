using Vixen.Module.SmartController;
using Vixen.Sys;

namespace VixenModules.SmartController.VideoScreen
{
	public class VideoScreenControllerModule : SmartControllerModuleInstanceBase
	{
		//private DebugControllerOutputForm _form;

		//public DebugControllerModule()
		//{
		//	DataPolicyFactory = new DataPolicyFactory();
		//}

		//public override void UpdateState(int chainIndex, ICommand[] outputStates)
		//{
		//	_form.UpdateState(outputStates);
		//}

		//public override void Start()
		//{
		//	base.Start();

		//	if (_form != null) {
		//		_form.Dispose();
		//		_form = null;
		//	}

		//	_form = new DebugControllerOutputForm();
		//	_form.Show();
		//}

		//public override void Stop()
		//{
		//	base.Stop();

		//	if (_form != null) {
		//		_form.Hide();
		//		_form.Dispose();
		//		_form = null;
		//	}
		//}

		//protected override void Dispose(bool disposing)
		//{
		//	if (disposing)
		//	{
		//		if (_form != null && !_form.IsDisposed)
		//		{
		//			_form.Dispose();
		//		}
		//		_form = null;	
		//	}
		
		//	base.Dispose(disposing);
		//}

		#region Overrides of SmartControllerModuleInstanceBase

		/// <inheritdoc />
		public override void UpdateState(IntentChangeCollection[] outputStates)
		{
			
		}

		/// <inheritdoc />
		public override int OutputCount { get; set; }

		#endregion
	}
}