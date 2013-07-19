﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using D2D = Microsoft.WindowsAPICodePack.DirectX.Direct2D1;
using DWrite = Microsoft.WindowsAPICodePack.DirectX.DirectWrite;
using WIC = Microsoft.WindowsAPICodePack.DirectX.WindowsImagingComponent;
using System.Threading.Tasks;
using System.Drawing;
using System.Timers;
using System.Diagnostics;
using System.Drawing.Imaging;

namespace Common.Controls.Direct2D
{
	public sealed class DisplayScene : Direct2D.AnimatedScene
	{

		private DWrite.TextFormat textFormat;
		private DWrite.DWriteFactory writeFactory;
		private const int PIXEL_SIZE = 2;
		// These are used for tracking an accurate frames per second
		private DateTime time;
		private int frameCount;
		private int fps;
		private Guid DisplayID;
		public DisplayScene(System.Drawing.Image backgroundImage, Guid displayID)
			: base(100) // Will probably only be about 67 fps due to the limitations of the timer
		{
			this.writeFactory = DWrite.DWriteFactory.CreateFactory();

			DisplayID = displayID;
			BackgroundImage = backgroundImage;
			BackgroundAlpha = 128;
			if (Points == null)
				Points = new ConcurrentDictionary<Guid, ConcurrentDictionary<Guid, List<DisplayPoint>>>();
			ConcurrentDictionary<Guid, List<DisplayPoint>> dic;
			Points.TryRemove(displayID, out dic);
			Points.TryAdd(displayID, new ConcurrentDictionary<Guid, List<DisplayPoint>>());
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing) {
				this.writeFactory.Dispose();

			}

			base.Dispose(disposing);
		}

		protected override void OnCreateResources()
		{
			// We don't need to free any resources because the base class will
			// call OnFreeResources if necessary before calling this method.

			this.textFormat = this.writeFactory.CreateTextFormat("Arial", 10);

			base.OnCreateResources(); // Call this last to start the animation
		}

		protected override void OnFreeResources()
		{
			base.OnFreeResources(); // Call this first to stop the animation

		}
		System.Drawing.Image backgroundImage;
		public System.Drawing.Image BackgroundImage
		{
			get { return backgroundImage; }
			set
			{
				backgroundImage = value;
				isDirty = true;
			}
		}

		public byte BackgroundAlpha { get; set; }
		bool isDirty = true;
		D2D.D2DBitmap background = null;

		public D2D.D2DBitmap Background
		{
			get
			{
				TryCreateBackgroundBitmap();
				return background;
			}
		}

		private void TryCreateBackgroundBitmap()
		{
			if (RenderTarget != null && ((isDirty && BackgroundImage != null) || (background == null && BackgroundImage != null))) {
				using (var ms = new System.IO.MemoryStream()) {

					backgroundImage.Save(ms, backgroundImage.RawFormat);

					ms.Position = 0;
					using (var factory = WIC.ImagingFactory.Create()) {
						using (WIC.BitmapDecoder decoder = factory.CreateDecoderFromStream(ms, WIC.DecodeMetadataCacheOption.OnDemand)) {
							using (WIC.BitmapFrameDecode source = decoder.GetFrame(0)) {
								using (WIC.FormatConverter converter = factory.CreateFormatConverter()) {
									converter.Initialize(source.ToBitmapSource(), WIC.PixelFormats.Pbgra32Bpp, WIC.BitmapDitherType.None, WIC.BitmapPaletteType.MedianCut);
									background = RenderTarget.CreateBitmapFromWicBitmap(converter.ToBitmapSource());
									isDirty = false;
								}
							}
						}
					}
				}
			}
		}



		protected override void OnRender()
		{

			try {
				Stopwatch w = Stopwatch.StartNew();

				// Calculate our actual frame rate
				this.frameCount++;

				if (DateTime.UtcNow.Subtract(this.time).TotalSeconds >= 1) {
					this.fps = this.frameCount;
					this.frameCount = 0;
					this.time = DateTime.UtcNow;
				}


				this.RenderTarget.BeginDraw();

				this.RenderTarget.Clear(new D2D.ColorF(0, 0, 0, 1f));
				//this.RenderTarget.Clear();

				if (Background != null) {
					//if (isDirty || currentState == null) {
					//	currentState = this.Factory.CreateDrawingStateBlock();
					this.RenderTarget.DrawBitmap(Background);
					//	this.RenderTarget.SaveDrawingState(currentState);
					//	isDirty = false;
					//} else
					//	this.RenderTarget.RestoreDrawingState(currentState);
				}

				var points = Points[DisplayID].SelectMany(s => s.Value.ToList()).ToList();
				Random r = new Random();
				points.ForEach(p => {
					var rect = new D2D.RectF();
					rect.Top = p.Shape.Top;
					rect.Bottom = p.Shape.Bottom;
					rect.Left = p.Shape.Left;
					rect.Right = p.Shape.Right;
					rect.Height = p.Shape.Height;
					rect.Width = p.Shape.Width;

					p.Color = Color.FromArgb(r.Next(0, 255), r.Next(0, 255), r.Next(0, 255), r.Next(0, 255));

					using (var brush = this.RenderTarget.CreateSolidColorBrush(p.Color.ToColorF())) {
						//this.RenderTarget.DrawRectangle(rect, brush, .5f);
						//this.RenderTarget.FillRectangle(rect, brush);
						this.RenderTarget.FillEllipse(new D2D.Ellipse() { Point = new D2D.Point2F(p.Shape.Location.X, p.Shape.Location.Y), RadiusX = p.PixelSize / 2, RadiusY = p.PixelSize / 2 }, brush);
					}
				});



				w.Stop();
				// Draw a little FPS in the top left corner
				string text = string.Format("FPS {0} Points {1} ElapsedMS: {2}", this.fps, Points[DisplayID].Count(), w.ElapsedMilliseconds);

				using (var textBrush = this.RenderTarget.CreateSolidColorBrush(Color.White.ToColorF())) {
					this.RenderTarget.DrawText(text, this.textFormat, new D2D.RectF(10, 10, 100, 20), textBrush);
				}

				// All done!

				this.RenderTarget.EndDraw();




			} catch (Exception e) {

				Console.WriteLine(e.Message);
			}
		}



		public static ConcurrentDictionary<Guid, ConcurrentDictionary<Guid, List<DisplayPoint>>> Points = new ConcurrentDictionary<Guid, ConcurrentDictionary<Guid, List<DisplayPoint>>>();
		public static ConcurrentDictionary<Guid, bool> Paused = new ConcurrentDictionary<Guid, bool>();


		//private void GenerateDemoPoints()
		//{
		//	if (Points == null) Points = new ConcurrentBag< DisplayPoint>();


		//	Stopwatch w = Stopwatch.StartNew();


		//	var size = this.RenderTarget.Size;

		//	if (size.Width > 1 && size.Height > 1) {

		//		Random rnd = new Random((int)((double)DateTime.Now.Millisecond * Math.PI));

		//		for (int i = 0; i < size.Width; i++) {
		//			for (int j = 1; j < size.Height + 1; j++) {
		//				if (rnd.Next(1, 100) <= 5) {
		//					DisplayPoint p = new DisplayPoint();

		//					p = new DisplayPoint() {
		//						Shape = new Rectangle() {
		//							Height = PIXEL_SIZE,
		//							Width = PIXEL_SIZE,
		//							X = (int)i * PIXEL_SIZE,
		//							Y = (int)j * PIXEL_SIZE,
		//						},
		//						Identifier = (float)i + ((float)j / 100),
		//						Color = System.Drawing.Color.FromArgb(rnd.Next(0, 255), rnd.Next(0, 255), rnd.Next(0, 255), rnd.Next(0, 255))
		//					};

		//					Points[p.Identifier] = p;
		//				}
		//			}
		//		}
		//		Console.WriteLine("Generated {1} Points In {0}", w.Elapsed, Points.Count());

		//	}

		//}

		public struct DisplayPoint
		{
			public Rectangle Shape { get; set; }
			public System.Drawing.Color Color { get; set; }
			public string Identifier { get { return string.Format("{0}-{1}-{2}-{3}", Shape.X, Shape.Y, Shape.Width, Shape.Height); } }
			public int PixelSize { get; set; }

		}


	}
}
