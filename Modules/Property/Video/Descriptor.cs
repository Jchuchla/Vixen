using System;
using Vixen.Module.Property;

namespace VixenModules.Property.Video
{
	public class Descriptor : PropertyModuleDescriptorBase
	{
		private Guid _typeId = new Guid("{26EBB496-DE40-4ABA-A1D4-D28A691D7E6D}");

		public override string TypeName
		{
			get { return "Video"; }
		}

		public override Guid TypeId
		{
			get { return _typeId; }
		}

		public override Type ModuleClass
		{
			get { return typeof (VideoModule); }
		}

		public override Type ModuleDataClass
		{
			get { return typeof (Data); }
		}

		public override string Author
		{
			get { return "Jon Chuchla"; }
		}

		public override string Description
		{
			get { return "Marks the attributed node as a Video Element"; }
		}

		public override string Version
		{
			get { return "1.0"; }
		}
	}
}