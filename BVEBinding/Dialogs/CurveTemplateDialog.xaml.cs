﻿/*
 * Created by SharpDevelop.
 * User: HAZAMA
 * Date: 2013/04/11
 * Time: 14:46
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using BVE5Binding.Commands;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.Core;
using ICSharpCode.SharpDevelop;

namespace BVE5Binding.Dialogs
{
	/// <summary>
	/// Interaction logic for CurveTemplateDialog.xaml
	/// </summary>
	public partial class CurveTemplateDialog : Window
	{
		int curve_position, length_curve, length_transition_before, length_transition_after, radius, speed, gauge;
		double cant;
		AbstractTCLCalculateStrategy tcl_strategy;
		
		double CantInMeters{
			get{
				return cant / 1000.0;
			}
		}
		
		public TextArea TextArea{get; private set;}
		
		Window parent_window;
		TextDocument document;
		
		/// <summary>
		/// Gets/Sets the start of the text range in which the completion window stays open.
		/// This text portion is used to determine the text used to select an entry in the completion list by typing.
		/// </summary>
		public int StartOffset { get; set; }
		
		/// <summary>
		/// Gets/Sets the end of the text range in which the completion window stays open.
		/// This text portion is used to determine the text used to select an entry in the completion list by typing.
		/// </summary>
		public int EndOffset { get; set; }
		
		/// <summary>
		/// Gets whether the window was opened above the current line.
		/// </summary>
		protected bool IsUp { get; private set; }
		
		public CurveTemplateDialog(TextArea textArea)
		{
			curve_position = length_curve = length_transition_before = length_transition_after = speed = -1;
			radius = int.MinValue;
			cant = double.NaN;
			tcl_strategy = new SineTCLCalculateStrategy();
			InitializeComponent();
			this.TextArea = textArea;
			parent_window = Window.GetWindow(textArea);
			this.Owner = parent_window;
			//this.AddHandler(MouseUpEvent, new MouseButtonEventHandler(OnMouseUp), true);
			
			StartOffset = EndOffset = this.TextArea.Caret.Offset;
			
			AttachEvents();
		}
		
		#region Event Handlers
		void AttachEvents()
		{
			document = this.TextArea.Document;
			if(document != null)
				document.Changing += textArea_Document_Changing;
			
			// LostKeyboardFocus seems to be more reliable than PreviewLostKeyboardFocus - see SD-1729
			this.TextArea.LostKeyboardFocus += TextAreaLostFocus;
			this.TextArea.TextView.ScrollOffsetChanged += TextViewScrollOffsetChanged;
			this.TextArea.DocumentChanged += TextAreaDocumentChanged;
			if(parent_window != null)
				parent_window.LocationChanged += parentWindow_LocationChanged;
			
			// close previous completion windows of same type
			foreach(InputHandler x in this.TextArea.StackedInputHandlers.OfType<InputHandler>()){
				if(x.window.GetType() == this.GetType())
					this.TextArea.PopStackedInputHandler(x);
			}
			
			myInputHandler = new InputHandler(this);
			this.TextArea.PushStackedInputHandler(myInputHandler);
		}
		
		/// <summary>
		/// Detaches events from the text area.
		/// </summary>
		protected virtual void DetachEvents()
		{
			if(document != null)
				document.Changing -= textArea_Document_Changing;
			
			this.TextArea.LostKeyboardFocus -= TextAreaLostFocus;
			this.TextArea.TextView.ScrollOffsetChanged -= TextViewScrollOffsetChanged;
			this.TextArea.DocumentChanged -= TextAreaDocumentChanged;
			if(parent_window != null)
				parent_window.LocationChanged -= parentWindow_LocationChanged;
			
			this.TextArea.PopStackedInputHandler(myInputHandler);
		}
		
		#region InputHandler
		InputHandler myInputHandler;
		
		/// <summary>
		/// A dummy input handler (that justs invokes the default input handler).
		/// This is used to ensure the completion window closes when any other input handler
		/// becomes active.
		/// </summary>
		sealed class InputHandler : TextAreaStackedInputHandler
		{
			internal readonly CurveTemplateDialog window;
			
			public InputHandler(CurveTemplateDialog window)
				: base(window.TextArea)
			{
				Debug.Assert(window != null);
				this.window = window;
			}
			
			public override void Detach()
			{
				base.Detach();
				window.Close();
			}
			
			const Key KeyDeadCharProcessed = (Key)0xac; // Key.DeadCharProcessed; // new in .NET 4
			
			public override void OnPreviewKeyDown(KeyEventArgs e)
			{
				// prevents crash when typing deadchar while CC window is open
				if(e.Key == KeyDeadCharProcessed)
					return;
				
				e.Handled = RaiseEventPair(window, PreviewKeyDownEvent, KeyDownEvent,
				                           new KeyEventArgs(e.KeyboardDevice, e.InputSource, e.Timestamp, e.Key));
			}
			
			public override void OnPreviewKeyUp(KeyEventArgs e)
			{
				if(e.Key == KeyDeadCharProcessed)
					return;
				
				e.Handled = RaiseEventPair(window, PreviewKeyUpEvent, KeyUpEvent,
				                           new KeyEventArgs(e.KeyboardDevice, e.InputSource, e.Timestamp, e.Key));
			}
		}
		#endregion
		
		void TextViewScrollOffsetChanged(object sender, EventArgs e)
		{
			// Workaround for crash #1580 (reproduction steps unknown):
			// NullReferenceException in System.Windows.Window.CreateSourceWindow()
			if (!sourceIsInitialized)
				return;
			
			IScrollInfo scrollInfo = this.TextArea.TextView;
			Rect visibleRect = new Rect(scrollInfo.HorizontalOffset, scrollInfo.VerticalOffset, scrollInfo.ViewportWidth, scrollInfo.ViewportHeight);
			// close completion window when the user scrolls so far that the anchor position is leaving the visible area
			if (visibleRect.Contains(visual_location) || visibleRect.Contains(visual_location_top))
				UpdatePosition();
			else
				Close();
		}
		
		void TextAreaDocumentChanged(object sender, EventArgs e)
		{
			Close();
		}
		
		void TextAreaLostFocus(object sender, RoutedEventArgs e)
		{
			Dispatcher.BeginInvoke(new Action(CloseIfFocusLost), DispatcherPriority.Background);
		}
		
		void parentWindow_LocationChanged(object sender, EventArgs e)
		{
			UpdatePosition();
		}
		
		/// <inheritdoc/>
		protected override void OnDeactivated(EventArgs e)
		{
			base.OnDeactivated(e);
			Dispatcher.BeginInvoke(new Action(CloseIfFocusLost), DispatcherPriority.Background);
		}
		#endregion
		
		/// <summary>
		/// Raises a tunnel/bubble event pair for a WPF control.
		/// </summary>
		/// <param name="target">The WPF control for which the event should be raised.</param>
		/// <param name="previewEvent">The tunneling event.</param>
		/// <param name="event">The bubbling event.</param>
		/// <param name="args">The event args to use.</param>
		/// <returns>The <see cref="RoutedEventArgs.Handled"/> value of the event args.</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1030:UseEventsWhereAppropriate")]
		protected static bool RaiseEventPair(UIElement target, RoutedEvent previewEvent, RoutedEvent @event, RoutedEventArgs args)
		{
			if(target == null)
				throw new ArgumentNullException("target");
			
			if(previewEvent == null)
				throw new ArgumentNullException("previewEvent");
			
			if(@event == null)
				throw new ArgumentNullException("event");
			
			if(args == null)
				throw new ArgumentNullException("args");
			
			args.RoutedEvent = previewEvent;
			target.RaiseEvent(args);
			args.RoutedEvent = @event;
			target.RaiseEvent(args);
			return args.Handled;
		}
		
		// Special handler: handledEventsToo
		void OnMouseUp(object sender, MouseButtonEventArgs e)
		{
			ActivateParentWindow();
		}
		
		/// <summary>
		/// Activates the parent window.
		/// </summary>
		protected virtual void ActivateParentWindow()
		{
			if(parent_window != null)
				parent_window.Activate();
		}
		
		void CloseIfFocusLost()
		{
			if(CloseOnFocusLost){
				Debug.WriteLine("CloseIfFocusLost: this.IsActive=" + this.IsActive + " IsTextAreaFocused=" + IsTextAreaFocused);
				if(!this.IsActive && !IsTextAreaFocused)
					Close();
			}
		}
		
		/// <summary>
		/// Gets whether the completion window should automatically close when the text editor looses focus.
		/// </summary>
		protected virtual bool CloseOnFocusLost {
			get { return true; }
		}
		
		bool IsTextAreaFocused {
			get {
				if(parent_window != null && !parent_window.IsActive)
					return false;
				
				return this.TextArea.IsKeyboardFocused;
			}
		}
		
		bool sourceIsInitialized;
		
		/// <inheritdoc/>
		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);
			
			if(document != null && this.StartOffset != this.TextArea.Caret.Offset)
				SetPosition(new TextViewPosition(document.GetLocation(this.StartOffset)));
			else
				SetPosition(this.TextArea.Caret.Position);
			
			sourceIsInitialized = true;
		}
		
		/// <inheritdoc/>
		protected override void OnClosed(EventArgs e)
		{
			base.OnClosed(e);
			DetachEvents();
		}
		
		/// <inheritdoc/>
		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			if(!e.Handled && e.Key == Key.Escape){
				e.Handled = true;
				Close();
			}
		}
		
		Point visual_location, visual_location_top;
		
		/// <summary>
		/// Positions the completion window at the specified position.
		/// </summary>
		protected void SetPosition(TextViewPosition position)
		{
			TextView text_view = this.TextArea.TextView;
			
			visual_location = text_view.GetVisualPosition(position, VisualYPosition.LineBottom);
			visual_location_top = text_view.GetVisualPosition(position, VisualYPosition.LineTop);
			UpdatePosition();
		}
		
		/// <summary>
		/// Updates the position of the CompletionWindow based on the parent TextView position and the screen working area.
		/// It ensures that the CompletionWindow is completely visible on the screen.
		/// </summary>
		protected void UpdatePosition()
		{
			TextView text_view = this.TextArea.TextView;
			// PointToScreen returns device dependent units (physical pixels)
			Point location = text_view.PointToScreen(visual_location - text_view.ScrollOffset);
			Point location_top = text_view.PointToScreen(visual_location_top - text_view.ScrollOffset);
			
			// Let's use device dependent units for everything
			Size completion_window_size = new Size(this.ActualWidth, this.ActualHeight).TransformToDevice(text_view);
			Rect bounds = new Rect(location, completion_window_size);
			Rect working_screen = System.Windows.Forms.Screen.GetWorkingArea(location.ToSystemDrawing()).ToWpf();
			
			if(!working_screen.Contains(bounds)){
				if(bounds.Left < working_screen.Left){
					bounds.X = working_screen.Left;
				}else if(bounds.Right > working_screen.Right){
					bounds.X = working_screen.Right - bounds.Width;
				}
				
				if(bounds.Bottom > working_screen.Bottom){
					bounds.Y = location_top.Y - bounds.Height;
					IsUp = true;
				}else{
					IsUp = false;
				}
				
				if(bounds.Y < working_screen.Top)
					bounds.Y = working_screen.Top;
			}
			// Convert the window bounds to device independent units
			bounds = bounds.TransformFromDevice(text_view);
			this.Left = bounds.X;
			this.Top = bounds.Y;
		}
		
		/// <inheritdoc/>
		protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
		{
			base.OnRenderSizeChanged(sizeInfo);
			if(sizeInfo.HeightChanged && IsUp)
				this.Top += sizeInfo.PreviousSize.Height - sizeInfo.NewSize.Height;
		}
		
		void textArea_Document_Changing(object sender, DocumentChangeEventArgs e)
		{
			if(e.Offset == StartOffset && e.RemovalLength == 0)
				StartOffset = e.GetNewOffset(StartOffset, AnchorMovementType.AfterInsertion);
			else
				StartOffset = e.GetNewOffset(StartOffset, AnchorMovementType.BeforeInsertion);
			
			EndOffset = e.GetNewOffset(EndOffset, AnchorMovementType.AfterInsertion);
		}
		
		bool IsReady()
		{
			return curve_position != -1 && length_curve != -1 && length_transition_before != -1 && length_transition_after != -1 && radius != int.MinValue &&
				!double.IsNaN(cant);
		}
		
		void TextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			var text_box = (TextBox)sender;
			try{
				switch(text_box.Name){
				case "LengthCurveTextBox":
					length_curve = Convert.ToInt32(text_box.Text);
					break;
					
				case "CurvePositionTextBox":
					curve_position = Convert.ToInt32(text_box.Text);
					break;
					
				case "LengthTransitionBeforeTextBox":
					length_transition_before = Convert.ToInt32(text_box.Text);
					SpeedTextBox.IsEnabled = (length_transition_before < 0 || length_transition_after < 0 || cant < 0.0);
					GaugeTextBox.IsEnabled = (length_transition_before < 0 || length_transition_after < 0 || cant < 0.0);
					break;
					
				case "LengthTransitionAfterTextBox":
					length_transition_after = Convert.ToInt32(text_box.Text);
					SpeedTextBox.IsEnabled = (length_transition_before < 0 || length_transition_after < 0 || cant < 0.0);
					GaugeTextBox.IsEnabled = (length_transition_before < 0 || length_transition_after < 0 || cant < 0.0);
					break;
					
				case "RadiusTextBox":
					radius = Convert.ToInt32(text_box.Text);
					break;
					
				case "CantTextBox":
					cant = Convert.ToDouble(text_box.Text);
					SpeedTextBox.IsEnabled = (length_transition_before < 0 || length_transition_after < 0 || cant < 0.0);
					GaugeTextBox.IsEnabled = (length_transition_before < 0 || length_transition_after < 0 || cant < 0.0);
					break;
					
				case "SpeedTextBox":
					speed = Convert.ToInt32(text_box.Text);
					break;
					
				case "GaugeTextBox":
					gauge = Convert.ToInt32(text_box.Text);
					break;
				}
			}
			catch(FormatException){
				if(text_box.Text != "-" && text_box.Text != "")
					MessageBox.Show(string.Format(StringParser.Parse("${res:CommonStrings.TextNonNumber}"), text_box.Text), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		
		public string GenerateText(string templateText)
		{
			EnsureValues();
			int transition_start_position = curve_position - length_transition_before;
			int curve_end_position = curve_position + length_curve;
			int transition_end_position = curve_end_position + length_transition_after;
			
			if(!IsReady()){
				MessageBox.Show(StringParser.Parse("${res:CurveTemplateDialog.Text.ErrorInserting}"), "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
				return "";
			}
			var tags = new []{
				new StringTagPair("CurvePosition", curve_position.ToString()),
				new StringTagPair("TransitionStartPosition", transition_start_position.ToString()),
				new StringTagPair("CurveEndPosition", curve_end_position.ToString()),
				new StringTagPair("TransitionEndPosition", transition_end_position.ToString()),
				new StringTagPair("Radius", radius.ToString()),
				new StringTagPair("Cant", CantInMeters.ToString()),
				new StringTagPair("TabCharacter", "\t")
			};
			
			return StringParser.Parse(templateText, tags);
		}
		
		void EnsureValues()
		{
			/*if(cant < 0){
				var cant_strategy = new IdealCantCalculateStrategy();
				cant_strategy.CurveRadius = radius;
			}*/
			
			if(length_transition_before < 0){
				if(speed == -1)
					MessageBox.Show(StringParser.Parse("{res:CurveTemplateDialog.Msg.SpeedNotEntered}"), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				
				//The initial value of the transition curve length is determined by the following rule: TCL >= C * 600 where TCL is the transition curve length
				//and C is the cant
				var tmp = CantInMeters * 600;
				
				tcl_strategy.Radius = radius;
				tcl_strategy.Cant = cant;
				tcl_strategy.Speed = speed;
				tcl_strategy.Gauge = gauge;
				var min_tcl = tcl_strategy.Calculate();
				
				length_transition_before = (min_tcl > tmp) ? (int)Math.Ceiling(min_tcl) : (int)Math.Ceiling(tmp);
			}
			
			if(length_transition_after < 0){
				if(speed == -1)
					MessageBox.Show(StringParser.Parse("{res:CurveTemplateDialog.Msg.SpeedNotEntered}"), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				
				var tmp = CantInMeters * 600;
				tcl_strategy.Radius = radius;
				tcl_strategy.Cant = cant;
				tcl_strategy.Speed = speed;
				tcl_strategy.Gauge = gauge;
				var min_tcl = tcl_strategy.Calculate();
				
				length_transition_after = (min_tcl > tmp) ? (int)Math.Ceiling(min_tcl) : (int)Math.Ceiling(tmp);
			}
			
			if(radius < 0)
				cant = -cant;
		}
	}
}