﻿using Vixen.Commands;
using Vixen.Data.Value;
using Vixen.Intent;
using Vixen.Module;
using Vixen.Sys;

using VixenModules.App.Curves;
using VixenModules.App.Fixture;
using VixenModules.Property.IntelligentFixture;

using ZedGraph;

namespace VixenModules.Effect.Effect
{
	/// <summary>
	/// Base class for intelligent fixture effects.	
	/// This base class is intended to be used with fixture functions that utilize curves.
	/// </summary>
	/// <typeparam name="T_Data">Type of data class associated with the effect</typeparam>
	public abstract class FixtureEffectBase<T_Data> : BaseEffect
		where T_Data : EffectTypeModuleData
	{
		#region Fields

		/// <summary>
		/// Web page help URL associated with the effect.
		/// </summary>
		private string _helpURL;

		#endregion

		#region Constructor

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="helpURL">Help URL associated with the effect</param>
		public FixtureEffectBase(string helpURL)
		{
			// Save off the help URL
			_helpURL = helpURL;
		}

		#endregion

		#region Protected Properties

		/// <summary>
		/// Gets or sets the data associated with the effect instance.
		/// </summary>
		protected T_Data Data { get; set; }

		/// <summary>
		/// Module data associated with the effect.
		/// </summary>
		protected override EffectTypeModuleData EffectModuleData 
		{ 
			get
			{
				return (EffectTypeModuleData)Data;
			}
		}

		/// <summary>
		/// Collection of intents generated by the effect.
		/// </summary>
		protected EffectIntents EffectIntentCollection { get; set; }

		#endregion

		#region Protected Methods

		/// <summary>
		/// Renders the effect.
		/// </summary>
		/// <returns>Intents generated by the effect</returns>
		protected override EffectIntents _Render()
		{
			// Return the collection of intents generated by the effect
			return EffectIntentCollection;
		}

		/// <summary>
		/// Pre-render the effect intents.
		/// </summary>
		/// <param name="cancellationToken">Indicator of the rendering has been cancelled</param>
		protected override void _PreRender(CancellationTokenSource cancellationToken = null)
		{
			// Create the collection of intents
			EffectIntentCollection = new EffectIntents();

			// Delegate to the derived effect to render the intents
			PreRenderInternal(cancellationToken);
		}

		/// <summary>
		/// Returns leaf nodes that have an intelligent fixture property.
		/// </summary>
		/// <param name="node">Node to start search from</param>
		/// <returns>Collection of leaf nodes with intelligent fixture properties</returns>
		protected IEnumerable<IElementNode> GetLeafNodesWithIntelligentFixtureProperty(IElementNode node)
		{ 
			// Return leaf nodes that have an intelligent fixture property
			return node.GetLeafEnumerator().Where(x => x != null && x.Properties.Contains(IntelligentFixtureDescriptor._typeId)); 
		}

		/// <summary>
		/// Retrieves the leaf nodes that support the specified function identity type.
		/// </summary>
		/// <param name="type">Function identity type to search for</param>		
		/// <param name="nodeToFunction">Dictionary of node to function information</param>		
		/// <returns>Collection of nodes that contain the specified function identity</returns>						
		protected List<IElementNode> GetRenderNodesForFunctionIdentity(
			FunctionIdentity type,
			IDictionary<IElementNode, (string label, string tag)> nodeToFunction)
		{
			// Create the return collection
			List<IElementNode> renderNodes = new();
			
			// Loop over the target nodes
			foreach (IElementNode node in TargetNodes)
			{
				// Loop over the leaf nodes that contain an intelligent fixture property
				foreach(IElementNode leafNode in GetLeafNodesWithIntelligentFixtureProperty(node))
				{
					// Add the render nodes that support the function identity
					renderNodes.AddRange(GetRenderNodesForFunctionIdentity(type, leafNode, nodeToFunction));					
				}				
			}

			return renderNodes;
		}

		/// <summary>
		/// Retrieves the leaf nodes that support the specified function identity type.
		/// </summary>
		/// <param name="type">Function identity type to search for</param>		
		/// <param name="node">Node to analyze</param>
		/// <param name="nodeToFunction">Dictionary of node to function information</param>		
		/// <returns>Collection of nodes that contain the specified function identity</returns>						
		protected List<IElementNode> GetRenderNodesForFunctionIdentity(
			FunctionIdentity type,			
			IElementNode node,
			IDictionary<IElementNode, (string label, string tag)> nodeToFunction)
		{
			// Create the return collection
			List<IElementNode> renderNodes = new();

			// If the node is NOT null then...
			if (node != null)
			{
				// Retrieve all leaf nodes that have an intelligent fixture property
				IEnumerable<IElementNode> leaves = GetLeafNodesWithIntelligentFixtureProperty(node);
				
				// Loop over the leaves
				foreach (IElementNode leafNode in leaves)
				{
					// Retrieve the fixture property associated with the node
					IntelligentFixtureModule fixtureProperty = (IntelligentFixtureModule)leafNode.Properties.Single(x => x is IntelligentFixtureModule);

					// If the fixture supports the specified function identity then...
					if (fixtureProperty.FixtureSpecification.SupportsFunction(type))
					{
						// Find the function associated with the effect based on function identity enumeration
						FixtureFunction func = fixtureProperty.FixtureSpecification.FunctionDefinitions.SingleOrDefault(function => function.FunctionIdentity == type);

						// Add the node and function information to the dictionary 
						nodeToFunction.Add(leafNode, new (func.Label, func.Name));

						// Add the node to collection of render nodes
						renderNodes.Add(leafNode);
					}
				}
			}

			return renderNodes;
		}

		/// <summary>
		/// Retrieves the leaf nodes that support the specified function identity type.
		/// </summary>
		/// <param name="type">Function identity type to search for</param>
		/// <param name="tags">Dictionary of function tags to populate</param>
		/// <returns>Nodes that contain the specified function identity type along with the optional label associated with the function</returns>
		protected IEnumerable<Tuple<IElementNode,string>> GetRenderNodesForFunctionIdentity(FunctionIdentity type, Dictionary<IElementNode, string> tags)
		{
			// Create the return collection
			IEnumerable<Tuple<IElementNode, string>> leavesThatSupportFunction = new List<Tuple<IElementNode, string>>();

			// Retrieve the first node
			IElementNode node = TargetNodes.FirstOrDefault();

			// If the node is NOT null then...
			if (node != null)
			{
				leavesThatSupportFunction = GetRenderNodesForFunctionIdentity(type, tags, node);
			}
			
			return leavesThatSupportFunction;
		}

		/// <summary>
		/// Retrieves the leaf nodes that support the specified function identity type.
		/// </summary>
		/// <param name="type">Function identity type to search for</param>
		/// <param name="tags">Dictionary of function tags to populate</param>		
		/// <param name="node">Node to analyze</param>
		/// <returns>Nodes that contain the specified function identity type along with the optional label associated with the function</returns>
		protected IEnumerable<Tuple<IElementNode, string>> GetRenderNodesForFunctionIdentity(FunctionIdentity type, Dictionary<IElementNode, string> tags, IElementNode node)
		{
			// Create the return collection
			List<Tuple<IElementNode, string>> leavesThatSupportFunction = new List<Tuple<IElementNode, string>>();

			// If the node is NOT null then...
			if (node != null)
			{
				// Retrieve all leaf nodes that have an intelligent fixture property
				IEnumerable<IElementNode> leaves = GetLeafNodesWithIntelligentFixtureProperty(node);

				// Clear the function tags associated with the nodes
				tags.Clear();

				// Loop over the leaves
				foreach (IElementNode leafNode in leaves)
				{
					// Retrieve the fixture property associated with the node
					IntelligentFixtureModule fixtureProperty = (IntelligentFixtureModule)leafNode.Properties.Single(x => x is IntelligentFixtureModule);

					// If the fixture supports the specified function identity then...
					if (fixtureProperty.FixtureSpecification.SupportsFunction(type))
					{
						// Find the function associated with the effect based on function identity enumeration
						FixtureFunction func = fixtureProperty.FixtureSpecification.FunctionDefinitions.SingleOrDefault(function => function.FunctionIdentity == type);

						// Add the function name to the collection of tags
						tags.Add(leafNode, func.Name);

						// Add the leaf to the collection of nodes to return
						leavesThatSupportFunction.Add(new Tuple<IElementNode, string>(leafNode, func.Label));
					}
				}
			}

			return leavesThatSupportFunction;
		}

		/// <summary>
		/// Retrieves the leaf nodes that support the specified function.
		/// </summary>
		/// <param name="functionName">Name of the function search for</param>
		/// <param name="functionIdentity">Function identity type to search for</param>
		/// <param name="tags">Dictionary of function tags to populate</param>
		/// <returns>Nodes that contain the specified function identity type  along with the optional label associated with the function</returns>
		protected IEnumerable<Tuple<IElementNode, string>> GetRenderNodesForFunctionName(string functionName, FunctionIdentity functionIdentity)
		{
			// Create the return collection
			List<Tuple<IElementNode, string>> leavesThatSupportFunction = new List<Tuple<IElementNode, string>>();

			// Retrieve the first node
			IElementNode node = TargetNodes.FirstOrDefault();

			// If the node is NOT null then...
			if (node != null)
			{
				// Retrieve all leaf nodes that have an intelligent fixture property
				IEnumerable<IElementNode> leaves = GetLeafNodesWithIntelligentFixtureProperty(node);
				
				// Loop over the leaves
				foreach (IElementNode leafNode in leaves)
				{
					// Retrieve the fixture property associated with the node
					IntelligentFixtureModule fixtureProperty = (IntelligentFixtureModule)leafNode.Properties.Single(x => x is IntelligentFixtureModule);

					// If the fixture supports the specified function identity then...
					if (fixtureProperty.FixtureSpecification.IsFunctionUsed(functionName, functionIdentity))
					{
						// Find the function associated with the effect based on function identity enumeration
						FixtureFunction func = fixtureProperty.FixtureSpecification.GetInUseFunction(functionName, functionIdentity);
																				
						// Add the leaf to the collection of nodes to return
						leavesThatSupportFunction.Add(new Tuple<IElementNode, string>(leafNode, func.Label));
					}
				}
			}

			return leavesThatSupportFunction;
		}

		/// <summary>
		/// Creates fixture intents from the specified curve.
		/// This method also populates a dictionary of tags so that the intents are properly tagged.
		/// </summary>
		/// <param name="curve">Curve to create intents from</param>
		/// <param name="functionType">Function associated with the intents</param>
		/// <param name="tags">Named tags associated with the function</param>
		/// <param name="cancellationToken">Whether or not the rendering has been cancelled</param>
		protected void RenderCurve(Curve curve, FunctionIdentity functionType, Dictionary<IElementNode, string> tags, CancellationTokenSource cancellationToken = null)
		{
			// Get the leaf nodes that support the specified fixture function type
			IEnumerable<Tuple<IElementNode, string>> nodes = GetRenderNodesForFunctionIdentity(functionType, tags);

			// Render the curve associated with the function
			RenderCurve(nodes, curve, functionType, string.Empty, tags, cancellationToken);
		}

		/// <summary>
		/// Creates fixture intents from the specified curve.
		/// This method also populates a dictionary of tags so that the intents are properly tagged.
		/// </summary>
		/// <param name="curve">Curve to create intents from</param>
		/// <param name="functionIdentity">Function associated with the intents</param>
		/// <param name="tag">Name of function to render</param>
		/// <param name="cancellationToken">Whether or not the rendering has been cancelled</param>
		protected void RenderCurve(Curve curve, FunctionIdentity functionIdentity, string tag, CancellationTokenSource cancellationToken = null)
		{
			// Get the leaf nodes that support the specified fixture function type
			IEnumerable<Tuple<IElementNode, string>> nodes = GetRenderNodesForFunctionName(tag, functionIdentity);

			// Render the curve associated with the function
			RenderCurve(nodes, curve, functionIdentity, tag, null, cancellationToken);
		}

		/// <summary>
		/// Creates fixture intents from the specified curve.
		/// This method also populates a dictionary of tags so that the intents are properly tagged.
		/// </summary>
		/// <param name="nodes">Nodes associated with the curve</param>
		/// <param name="curve">Curve to create intents from</param>
		/// <param name="functionType">Function associated with the intents</param>
		/// <param name="tag">Default tag to use when a dictionary of node tags is not provided</param>
		/// <param name="tags">Named tags associated with the function</param>
		/// <param name="cancellationToken">Whether or not the rendering has been cancelled</param>
		protected void RenderCurve(
			IEnumerable<Tuple<IElementNode, string>> nodes,
			Curve curve, FunctionIdentity functionType,
			string tag,
			Dictionary<IElementNode, string> tags,
			CancellationTokenSource cancellationToken = null)
		{
			// If any nodes were found then...
			if (nodes.Any())
			{
				// Sort the points on the curve
				curve.Points.Sort();

				// Create a hashset of point values
				HashSet<double> points = new HashSet<double> { 0.0 };

				// Transfer the point to the hashset
				foreach (PointPair point in curve.Points)
				{
					points.Add(point.X);
				}
				points.Add(100.0);

				// Convert the hash set into a list
				List<double> pointList = points.ToList();

				// Initialize the start time
				TimeSpan startTime = TimeSpan.Zero;

				// Loop over the curve points
				for (int i = 1; i < points.Count; i++)
				{
					// Determine the time span between two points
					TimeSpan timeSpan = TimeSpan.FromMilliseconds(TimeSpan.TotalMilliseconds * ((pointList[i] - pointList[i - 1]) / 100));

					// Loop over the leaf nodes associated with the effect
					foreach (Tuple<IElementNode, string> node in nodes)
					{
						// Default the node tag 
						string nodeTag = tag;

						// If a dictionary of tags was specified then...
						if (tags != null)
						{
							// Retrieve the function tag from the dictionary based on the node being processed
							nodeTag = tags[node.Item1];
						}

						// Get the previous point
						RangeValue<FunctionIdentity> startValue = new RangeValue<FunctionIdentity>(functionType, nodeTag, curve.GetValue(pointList[i - 1]) / 100d, node.Item2);

						// Get the current point
						RangeValue<FunctionIdentity> endValue = new RangeValue<FunctionIdentity>(functionType, nodeTag, curve.GetValue(pointList[i]) / 100d, node.Item2);

						// Extrapolate an intent from the two points
						RangeIntent intent = new RangeIntent(startValue, endValue, timeSpan);

						// If the rendering has been cancelled then...
						if (cancellationToken != null && cancellationToken.IsCancellationRequested)
						{
							// Exit the function
							return;
						}

						// Add the intent to the output collection
						EffectIntentCollection.AddIntentForElement(node.Item1.Element.Id, intent, startTime);
					}

					// Move on to the next point
					startTime = startTime + timeSpan;
				}
			}
		}
		
		/// <summary>
		/// Gives derived classes an opportunity to configure which effect settings are visible.
		/// </summary>
		/// <param name="refresh"></param>
		protected abstract void UpdateAttributes(bool refresh = true);

		/// <summary>
		/// Gives derived classes an opportunity to determines what functions the fixture supports relative to the effect.
		/// </summary>
		protected abstract void UpdateFixtureCapabilities();

		/// <summary>
		/// Abstract method to allow the derived effects to render intents.
		/// </summary>
		/// <param name="cancellationToken">Whether or not the rendering has been cancelled</param>
		protected abstract void PreRenderInternal(CancellationTokenSource cancellationToken = null);

		/// <summary>
		/// Handler for when the target nodes change.
		/// </summary>
		protected override void TargetNodesChanged()
		{
			// Determine what function the fixture(s) support
			UpdateFixtureCapabilities();

			// Show/hide effect control based on the support functions
			UpdateAttributes();
		}

		/// <summary>
		/// Draws the text on the timeline of the effect.
		/// </summary>
		/// <param name="graphics">Graphics drawing context</param>
		/// <param name="rect">Drawing area rectangle</param>
		/// <param name="color">Color of the text and curve</param>
		/// <param name="text">Text to display in the effect box on the timeline</param>
		/// <param name="drawStringYOffset">Y offset for drawing the text</param>
		/// <param name="y">Y Position for drawing the text</param>
		/// <param name="height">Height of the text</param>
		protected void DrawText(Graphics graphics, Rectangle rect, Color color, string text, int drawStringYOffset, int y, int height)
		{
			// Create a draw rectangle for the text
			Rectangle textRect = new Rectangle(rect.X, rect.Y, rect.Width, height);

			// Create the font for the text
			Font font = Vixen.Common.Graphics.GetAdjustedFont(graphics, text, textRect, SystemFonts.MessageBoxFont.Name, 24, SystemFonts.MessageBoxFont);

			// Draw the text on the graphics context
			graphics.DrawString(text, font, new SolidBrush(color), rect.X, y + drawStringYOffset);
		}
		
		/// <summary>
		/// Draws the visual representation of a curve in the timeline area of the effect.
		/// </summary>
		/// <param name="graphics">Graphics drawing context</param>
		/// <param name="rect">Drawing area rectangle</param>
		/// <param name="color">Color of the text and curve</param>
		/// <param name="text">Text to display in the effect box on the timeline</param>
		/// <param name="drawCurveYOffset">Y offset for drawing the curve</param>
		/// <param name="drawStringYOffset">Y offset for drawing the text</param>
		/// <param name="curve">Curve to draw</param>
		/// <param name="yTextPosition">Y position of the text</param>
		protected void DrawVisualRepresentation(Graphics graphics, Rectangle rect, Color color, string text, int drawCurveYOffset, int drawStringYOffset, Curve curve, int y)
		{									
			// Create the curve bitmap
			Bitmap curveBitmap = curve.GenerateGenericCurveImage(new Size(rect.Width, rect.Height), false, false, false, color);

			// Draw the curve bitmap on the graphics context (timeline)
			graphics.DrawImage(curveBitmap, rect.X, drawCurveYOffset);

			// Draw the text on the graphics context
			DrawText(graphics, rect, color, text, drawStringYOffset, y, rect.Height / 2);
		}

		/// <summary>
		/// Draws the visual representation of a curve in the timeline area of the effect with the specified colors for the background.
		/// </summary>
		/// <param name="graphics">Graphics drawing context</param>
		/// <param name="rect">Drawing area rectangle</param>
		/// <param name="color">Color of the text and curve</param>
		/// <param name="text">Text to display in the effect box on the timeline</param>
		/// <param name="drawCurveYOffset">Y offset for drawing the curve</param>
		/// <param name="drawStringYOffset">Y offset for drawing the text</param>
		/// <param name="curve">Curve to draw</param>
		/// <param name="y">Y position of the text</param>
		/// <param name="backgroundColors">Background colors for timeline</param>
		protected void DrawVisualRepresentationWithBackground(
			Graphics graphics, 
			Rectangle rect, 
			Color color, 
			string text, 
			int drawCurveYOffset, 
			int drawStringYOffset, 
			Curve curve, 
			int y,
			List<Color> backgroundColors)
		{
			// Create the curve bitmap
			Bitmap curveBitmap = curve.GenerateGenericCurveImage(new Size(rect.Width, rect.Height), false, false, false, color, backgroundColors);

			// Draw the curve bitmap on the graphics context (timeline)
			graphics.DrawImage(curveBitmap, rect.X, drawCurveYOffset);

			// Draw the text on the graphics context
			DrawText(graphics, rect, color, text, drawStringYOffset, y, rect.Height / 2);
		}

		/// <summary>
		/// Returns the intelligent fixture property associated with the specified element node.
		/// </summary>
		/// <param name="node">Element node to retrieve the property from</param>
		/// <returns>Intelligent fixture property associated with the node</returns>
		protected IntelligentFixtureModule GetIntelligentFixtureProperty(IElementNode node)
		{
			// Retrieve the intelligent fixture property from the node
			return (IntelligentFixtureModule)node.Properties.SingleOrDefault(x => x is IntelligentFixtureModule);
		}

		/// <summary>
		/// Gets the number of frames the effect is active for.
		/// </summary>
		/// <returns>Number of frames</returns>
		protected int GetNumberFrames()
		{
			// Default to zero frames
			int frames = 0;

			// If the time span is non zero then...
			if (TimeSpan != TimeSpan.Zero)
			{
				// Determine the number of frames the effect is active
				frames = (int)(TimeSpan.TotalMilliseconds / FrameTime);
			}

			return frames;
		}

		/// <summary>
		/// Determine where the specified frame number is as a percentage with respect to the duration of the effect.
		/// </summary>
		/// <param name="frame"></param>
		/// <returns>Percentage into the effect that has been rendered</returns>
		protected double GetEffectTimeIntervalPosition(int frame)
		{
			double position = 0;

			// If not at the beginning of the effect then...
			if (TimeSpan != TimeSpan.Zero)
			{
				// Calculate where the frame is within the duration of the effect
				position = (frame * FrameTime) / TimeSpan.TotalMilliseconds;
			}
			return position;
		}
		
		/// <summary>
		/// Returns all the fixture functions associated with the target nodes.
		/// </summary>
		/// <returns>Collection of fixture functions</returns>
		protected List<FixtureFunction> GetFixtureFunctions()
		{
			// Create the return collection
			List<FixtureFunction> fixtureFunctions = new List<FixtureFunction>();

			// Get the target node for the effect
			IElementNode node = TargetNodes.FirstOrDefault();

			// Make sure there is a target node
			if (node != null)
			{
				// If the effect was applied to a group we need to process the leaf nodes
				foreach (IElementNode leafNode in node.GetLeafEnumerator())
				{
					// Attempt to get the Intelligent Fixture property
					IntelligentFixtureModule fixtureProperty = GetIntelligentFixtureProperty(leafNode);

					// If a fixture property was found then...
					if (fixtureProperty != null)
					{
						// Get the fixture associated with the leaf node
						FixtureSpecification fixtureSpecification = fixtureProperty.FixtureSpecification;

						// Loop over the channels of the fixture
						foreach (FixtureChannel channel in fixtureSpecification.ChannelDefinitions)
						{
							// Get the function associated with the channel
							FixtureFunction func = fixtureSpecification.FunctionDefinitions.Single(function => function.Name == channel.Function);

							// If the function has not already been added then...
							if (func.FunctionType != FixtureFunctionType.None && !fixtureFunctions.Contains(func))
							{
								// Add the function to the collection of supported functions
								fixtureFunctions.Add(func);
							}
						}
					}
				}
			}

			return fixtureFunctions;
		}

		/// <summary>
		/// Renders a fixture index command for the specified fixture index and node.
		/// </summary>
		/// <param name="node">Target node for the command</param>
		/// <param name="fixtureIndex">Fixture index to wrap in the command</param>
		/// <param name="curve">Optional curve associated with the index value</param>
		/// <param name="function">Function associated with index</param>
		/// <param name="cancellationToken">Optional cancellation token</param>
		protected void RenderIndex(
			IElementNode node,
			FixtureIndexBase fixtureIndex,
			Curve curve,
			FixtureFunction function,
			CancellationTokenSource cancellationToken)
		{
			// Initialize the start time to zero
			TimeSpan startTime = TimeSpan.Zero;

			// Initialize the amount of time to increment with each frame
			TimeSpan frameTs = new TimeSpan(0, 0, 0, 0, FrameTime);

			// Loop over the frames of the efect
			for (int frameNum = 0; frameNum < GetNumberFrames(); frameNum++)
			{
				// Declare the index value to use for the command
				int indexValue;

				// Default the index range
				int minValue = 0;
				int maxValue = 0;

				// If the index uses a curve then...
				if (fixtureIndex.UseCurve)
				{
					// Get the position within the curve for the specified frame number
					double intervalPos = GetEffectTimeIntervalPosition(frameNum);

					// Convert to a number between 0 - 100.0
					double intervalPosFactor = intervalPos * 100;

					// Scale the value based on the start and stop values of the index
					indexValue = (int)Math.Round(ScaleCurveToValue(curve.GetValue(intervalPosFactor), fixtureIndex.EndValue, fixtureIndex.StartValue));

					// Save off the index range 
					minValue = fixtureIndex.StartValue;
					maxValue = fixtureIndex.EndValue;
				}
				else
				{
					// Retrieve the start value of the index
					indexValue = fixtureIndex.StartValue;
				}

				// Create a tagged command intent
				Named8BitCommand<FixtureIndexType> namedCommand = new Named8BitCommand<FixtureIndexType>(indexValue);

				// Assign the function tag to the command
				namedCommand.Tag = function.Name;

				// Assign the index type to the command
				namedCommand.IndexType = fixtureIndex.IndexType;

				// Assign the label to the command
				namedCommand.Label = function.Label;

				// Assign the index range to the command
				namedCommand.RangeMinimum = (byte)minValue;
				namedCommand.RangeMaximum = (byte)maxValue;

				// Create the command value from the tagged command
				CommandValue commandValue = new CommandValue(namedCommand);

				// Create the command intent from the command value and the time span
				CommandIntent commandIntent = new CommandIntent(commandValue, frameTs);

				// Add the command to the effect intents
				EffectIntentCollection.AddIntentForElement(node.Element.Id, commandIntent, startTime);

				// Increment the start time for the next frame
				startTime = startTime + frameTs;
			}
		}

		/// <summary>
		/// Finds the leaf nodes that support the specified fixture function and value.
		/// </summary>
		/// <param name="fixtureFunctionName">Fixture function name to find</param>
		/// <param name="fixtureIndexValue">Fixture index value to find</param>
		/// <returns>Collection of leaf nodes that support function and index value</returns>
		protected IEnumerable<IElementNode> GetRenderNodesForIndexFunction(string fixtureFunctionName, string fixtureIndexValue)
		{
			// Create the return collection
			List<IElementNode> leavesThatSupportFunction = new List<IElementNode>();

			// Retrieve the first node
			IElementNode node = TargetNodes.FirstOrDefault();

			// If the node is NOT null then
			if (node != null)
			{
				// Retrieve all leaf nodes that have an intelligent fixture property
				IEnumerable<IElementNode> leaves = GetLeafNodesWithIntelligentFixtureProperty(node);
					
				// Loop over the leaves
				foreach (IElementNode leafNode in leaves)
				{
					// Retrieve the fixture property associated with the node
					IntelligentFixtureModule fixtureProperty = GetIntelligentFixtureProperty(leafNode);

					// Find the function associated with the effect based on name
					FixtureFunction func = fixtureProperty.FixtureSpecification.FunctionDefinitions.SingleOrDefault(function => function.Name == fixtureFunctionName);

					// If the function was found and
					// make sure there is at least one channel using the function then...
					if (func != null &&
						fixtureProperty.FixtureSpecification.ChannelDefinitions.Any(channel => channel.Function == func.Name))
					{
						// Find the selected fixture index by name
						FixtureIndexBase fixtureIndex = func.GetIndexDataBase().FirstOrDefault(index => index.Name == fixtureIndexValue);

						// If the fixture index was found then...
						if (fixtureIndex != null)
						{
							// Add the leaf to the collection of nodes to return
							leavesThatSupportFunction.Add(leafNode);
						}
					}
				}
			}

			return leavesThatSupportFunction;
		}

		/// <summary>
		/// Renders the specified function and index value.
		/// </summary>		
		/// <param name="indexValue">Index value to render</param>
		/// <param name="indexCurve">Optional curve associated with the index</param>
		/// <param name="function">Fixture function being rendered</param>
		/// <param name="cancellationToken">Indicator if the rendering has been cancelled</param>
		protected void RenderIndexed(			
			string indexValue,
			Curve indexCurve,
			FixtureFunction function,
			CancellationTokenSource cancellationToken)
		{
			// Get all the leaf nodes that support the function and value
			IEnumerable<IElementNode> nodes = GetRenderNodesForIndexFunction(function.Name, indexValue);

			// Loop over the leaf nodes that support the function
			foreach (IElementNode node in nodes)
			{								
				// Find the index value
				FixtureIndexBase fixtureIndex = function.GetIndexDataBase().First(index => index.Name == indexValue);				

				// Render the index command
				RenderIndex(
					node,
					fixtureIndex,
					indexCurve,
					function,
					cancellationToken);
			}
		}

		#endregion

		#region IEffectModuleInstance

		/// <summary>
		/// Flag indicating if the effect is responsible for drawing the visual representation in the timeline.
		/// </summary>
		public override bool ForceGenerateVisualRepresentation { get { return true; } }

		#endregion

		#region Public Properties

		/// <summary>
		/// Provides the text for a help link.
		/// </summary>
		public override string Information
		{
			get { return "Visit the Vixen Lights website for more information on this effect."; }
		}

		/// <summary>
		/// URL for help link.
		/// </summary>
		public override string InformationLink
		{
			get { return _helpURL; }
		}

		/// <summary>
		/// Data associated with module.
		/// </summary>
		public override IModuleDataModel ModuleData
		{
			get => (IModuleDataModel)Data;
			set
			{
				Data = (T_Data)value;
				UpdateAttributes();
			}
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// Gets the function names with the specified identity associated with the target nodes.
		/// </summary>
		/// <returns>Function names with the specified identity associated with the target nodes</returns>
		public List<string> GetMatchingFunctionNames(FunctionIdentity functionIdentity)
		{
			// Get all the functions from target nodes (fixtures)
			List<FixtureFunction> functions = GetFixtureFunctions();

			// Filter the fixture functions to the applicable functions
			return functions.Where(function => function.FunctionIdentity == functionIdentity).Select(f => f.Name).Distinct().ToList();
		}

		#endregion
	}
}
