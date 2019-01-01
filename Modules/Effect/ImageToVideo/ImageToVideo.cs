using System.IO;
using System.Threading;
using Vixen.Attributes;
using Vixen.Intent;
using Vixen.Module;
using Vixen.Sys;
using Vixen.Sys.Attribute;
using VixenModules.Effect.Effect;
using VixenModules.EffectEditor.EffectDescriptorAttributes;

namespace VixenModules.Effect.ImageToVideo
{
	public class ImageToVideo : BaseEffect
	{
		private ImageToVideoData _data;
		private EffectIntents _elementData = null;

		public ImageToVideo()
		{
			_data = new ImageToVideoData();
		}

		protected override void TargetNodesChanged()
		{
		}

		protected override void _PreRender(CancellationTokenSource tokenSource = null)
		{
			_elementData = new EffectIntents();
			
			foreach (ElementNode node in TargetNodes) {
				if (tokenSource != null && tokenSource.IsCancellationRequested)
					return;

				if (node != null)
				{
					_elementData = RenderNode(node);
					_elementData = IntentBuilder.ConvertToStaticArrayIntents(_elementData, TimeSpan, false);					
				}
			}
		}

		protected override EffectIntents _Render()
		{
			return _elementData;
		}

		public override IModuleDataModel ModuleData
		{
			get { return _data; }
			set
			{
				_data = value as ImageToVideoData;
				IsDirty = true;
			}
		}

		protected override EffectTypeModuleData EffectModuleData
		{
			get { return _data; }
		}

        [Value]
        [ProviderCategory(@"Config", 2)]
        [ProviderDisplayName(@"Filename")]
        [ProviderDescription(@"Filename")]
        [PropertyEditor("ImagePathEditor")]
        [PropertyOrder(1)]
        public string FileName
        {
            get { return _data.FileName; }
            set
            {
                _data.FileName = ConvertPath(value);
                IsDirty = true;
                OnPropertyChanged();
            }
        }
        
        #region Information

        public override string Information
		{
			get { return "Visit the Vixen Lights website for more information on this effect."; }
		}

		public override string InformationLink
		{
			get { return "http://www.vixenlights.com/vixen-3-documentation/sequencer/effects/"; }
		}

        #endregion

        // renders the given node to the internal ElementData dictionary. If the given node is
        // not a element, will recursively descend until we render its elements.
	    private EffectIntents RenderNode(ElementNode node)
            {
                EffectIntents effectIntents = new EffectIntents();
                foreach (ElementNode elementNode in node.GetLeafEnumerator())
                {
                    if (true)  //Change condition to check if element has a video property
                    {
                        //Create the intents                    
                    }
                    //add the intents
                    //effectIntents.AddIntentForElement(elementNode.Element.Id, intent, TimeSpan.Zero);
                }
                return effectIntents;
            }

        private string ConvertPath(string path)
            {
                if (string.IsNullOrEmpty(path))
                {
                    return path;
                }
                if (Path.IsPathRooted(path))
                {
                    return CopyLocal(path);
                }
                return path;
            }

        private string CopyLocal(string path)
        {
            string name = Path.GetFileName(path);
            var destPath = Path.Combine(ImageToVideoDescriptor.ModulePath, name);
            if (path != destPath)
            {
                File.Copy(path, destPath, true);
            }
            return name;
        }
    }
}