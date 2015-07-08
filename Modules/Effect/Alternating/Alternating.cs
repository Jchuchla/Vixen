﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading;
using Vixen.Attributes;
using Vixen.Module;
using Vixen.Module.Effect;
using Vixen.Sys;
using Vixen.Sys.Attribute;
using Vixen.TypeConverters;
using VixenModules.App.ColorGradients;
using VixenModules.App.Curves;
using VixenModules.EffectEditor.EffectDescriptorAttributes;
using VixenModules.Property.Color;

namespace VixenModules.Effect.Alternating
{

	public class Alternating : EffectModuleInstanceBase
	{
		private AlternatingData _data;
		private EffectIntents _elementData;

		public Alternating()
		{
			_data = new AlternatingData();
			//InitPropertyDescriptors();
		}

		protected override void TargetNodesChanged()
		{
			CheckForInvalidColorData();
		}

		protected override void _PreRender(CancellationTokenSource cancellationToken = null)
		{
			_elementData = new EffectIntents();

			var targetNodes = TargetNodes.AsParallel();

			if (cancellationToken != null)
				targetNodes = targetNodes.WithCancellation(cancellationToken.Token);

			targetNodes.ForAll(node =>
			{
				if (node != null)
					RenderNode(node);
			});


		}

		//Validate that the we are using valid colors and set appropriate defaults if not.
		//we only need to check against 1 color variable,
		//it should be checked at a later time than what this is doing currently
		private void CheckForInvalidColorData()
		{
			// check for sane default colors when first rendering it
			var validColors = new HashSet<Color>();
			validColors.AddRange(TargetNodes.SelectMany(x => ColorModule.getValidColorsForElementNode(x, true)));

			//We need to beable to modify the list in the loop, since a collection used in foreach is immuatable we need to use a for loop
			for (int i = 0; i < Colors.Count; i++)
			{
				if (validColors.Any() && !Colors[i].ColorGradient.GetColorsInGradient().IsSubsetOf(validColors))
				{
					Colors[i].ColorGradient = new ColorGradient(validColors.First());
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
			set { _data = value as AlternatingData; }
		}

		#region Color

		[Value]
		[ProviderCategory(@"Color", 2)]
		[ProviderDisplayName(@"Color")]
		[ProviderDescription(@"Color")]
		public List<GradientLevelPair> Colors
		{
			get { return _data.Colors; }
			set
			{
				_data.Colors = value;
				IsDirty = true;
			}
		}

		#endregion

		#region Config

		[Value]
		[ProviderCategory(@"Config", 1)]
		[ProviderDisplayName(@"Interval")]
		[Description(@"Interval")]
		[NumberRange(0, 5000, 1, 0)]
		[PropertyOrder(1)]
		public int Interval
		{
			get { return _data.Interval; }
			set
			{
				_data.Interval = value;
				IsDirty = true;
			}
		}

		[Value]
		[ProviderCategory(@"Config", 1)]
		[ProviderDisplayName(@"IntervalSkip")]
		[Description(@"IntervalSkip")]
		[NumberRange(0, 10, 1)]
		[PropertyOrder(2)]
		public int IntervalSkipCount
		{
			get { return _data.IntervalSkipCount; }
			set
			{
				_data.IntervalSkipCount = value;
				IsDirty = true;
			}
		}



		[Value]
		[ProviderCategory(@"Config", 1)]
		[ProviderDisplayName(@"StaticEffect")]
		[Description(@"StaticEffect")]
		[TypeConverter(typeof (BooleanStringTypeConverter))]
		[BoolDescription("Yes", "No")]
		[PropertyEditor("SelectionEditor")]
		[PropertyOrder(3)]
		public bool EnableStatic
		{
			get { return _data.EnableStatic; }
			set
			{
				_data.EnableStatic = value;
				IsDirty = true;
			}
		}

		#endregion


		[Value]
		[ProviderCategory(@"Depth", 10)]
		[ProviderDisplayName(@"Depth")]
		[ProviderDescription(@"Depth")]
		[TypeConverter(typeof (TargetElementDepthConverter))]
		[PropertyEditor("SelectionEditor")]
		[MergableProperty(false)]
		public int DepthOfEffect
		{
			get { return _data.DepthOfEffect; }
			set
			{
				_data.DepthOfEffect = value;
				IsDirty = true;
			}
		}

		[Value]
		[Browsable(false)]
		public int GroupEffect
		{
			get { return _data.GroupEffect; }
			set
			{
				_data.GroupEffect = value;
				IsDirty = true;
			}
		}



		public override bool IsDirty
		{
			get
			{
				if (Colors.Any(x => !x.ColorGradient.CheckLibraryReference() || !x.Curve.CheckLibraryReference()))
				{
					base.IsDirty = true;
				}

				return base.IsDirty;
			}
			protected set { base.IsDirty = value; }
		}

		// renders the given node to the internal ElementData dictionary. If the given node is
		// not a element, will recursively descend until we render its elements.
		private void RenderNode(ElementNode node)
		{
			int intervals = 1;
			var gradientLevelItem = 0;
			var startColor = true;

			if (!EnableStatic)
			{
				intervals = Convert.ToInt32(Math.Ceiling(TimeSpan.TotalMilliseconds/Interval));
			}

			var startTime = TimeSpan.Zero;

			for (int i = 0; i < intervals; i++)
			{
				var intervalTime = intervals == 1
					? TimeSpan
					: TimeSpan.FromMilliseconds(Interval);

				int totalElements = node.Count();
				int currentNode = 0;

				var nodes = node.GetLeafEnumerator();

				while (currentNode < totalElements)
				{

					var elements = nodes.Skip(currentNode).Take(GroupEffect);

					currentNode += GroupEffect;

					foreach (var element in elements)
					{
						RenderElement(Colors[gradientLevelItem], ref startTime, ref intervalTime, element);
					}

					gradientLevelItem++;
					if (gradientLevelItem >= Colors.Count())
					{
						gradientLevelItem = 0;
					}

				}
				startColor = !startColor;
				gradientLevelItem += IntervalSkipCount;
				gradientLevelItem = (gradientLevelItem%Colors.Count());
				if (gradientLevelItem >= Colors.Count() || gradientLevelItem < 0)
				{
					gradientLevelItem = 0;
				}
				startTime += intervalTime;
			}
		}

		private void RenderElement(GradientLevelPair gradientLevelPair, ref TimeSpan startTime, ref TimeSpan intervalTime,
			ElementNode element)
		{
			var pulse = new Pulse.Pulse
			{
				TargetNodes = new[] {element},
				TimeSpan = intervalTime,
				ColorGradient = gradientLevelPair.ColorGradient,
				LevelCurve = gradientLevelPair.Curve
			};

			var result = pulse.Render();

			result.OffsetAllCommandsByTime(startTime);
			_elementData.Add(result);

		}
	}

}