﻿using System;
using Vixen.Data.Value;
using Vixen.Sys;
using System.Drawing;

namespace Vixen.Intent
{
	public class ColorIntent : LinearIntent<ColorValue>
	{
		public ColorIntent(ColorValue startValue, ColorValue endValue, TimeSpan timeSpan)
			: base(startValue, endValue, timeSpan)
		{
		}

		public static Color GetColorForIntents(IIntentStates states)
		{
			Color c = Color.Empty;

			foreach (IIntentState<LightingValue> intentState in states)
			{
				Color intentColor = ((IIntentState<LightingValue>)intentState).GetValue().GetOpaqueIntensityAffectedColor();
				c = Color.FromArgb(Math.Max(c.R, intentColor.R),
								   Math.Max(c.G, intentColor.G),
								   Math.Max(c.B, intentColor.B)
								   );
			}

			c = Color.FromArgb((c.R + c.G + c.B) / 3, c.R, c.G, c.B);

			return c;

		}

	}
}