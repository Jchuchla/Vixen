﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.Serialization;
using VixenModules.App.ColorGradients;
using VixenModules.App.Curves;
using VixenModules.Effect.Effect;
using ZedGraph;

namespace VixenModules.Effect.Dissolve
{
	[DataContract]
	public class DissolveData : EffectTypeModuleData {
		
		[DataMember]
		public List<ColorGradient> Colors { get; set; }

		[DataMember]
		public DissolveMode DissolveMode { get; set; }

		[DataMember]
		public DissolveMarkType DissolveMarkType { get; set; }

		[DataMember]
		public Curve DissolveCurve { get; set; }
		
		[DataMember]
		public bool DissolveMethod { get; set; }

		[DataMember]
		public Guid MarkCollectionId { get; set; }

		[DataMember]
		public bool RandomDissolve { get; set; }

		[DataMember]
		public bool DissolveFlip { get; set; }

		public DissolveData()
		{
			Colors = new List<ColorGradient> {new ColorGradient(Color.White)};
			DissolveMode = DissolveMode.TimeInterval;
			DissolveCurve = new Curve(new PointPairList(new[] { 100.0, 0.0 }, new[] { 0.0, 100.0 }));
			DissolveMarkType = DissolveMarkType.PerMark;
			RandomDissolve = true;
			DissolveFlip = true;
		}

		protected override EffectTypeModuleData CreateInstanceForClone()
		{
			var gradientList = Colors.ToList();
			var result = new DissolveData
            {
				Colors = gradientList,
                DissolveMode = DissolveMode,
				MarkCollectionId = MarkCollectionId,
	            DissolveCurve = new Curve(DissolveCurve),
	            DissolveMethod = DissolveMethod,
	            RandomDissolve = RandomDissolve,
	            DissolveFlip = DissolveFlip
			};
			return result;
		}
	}
}