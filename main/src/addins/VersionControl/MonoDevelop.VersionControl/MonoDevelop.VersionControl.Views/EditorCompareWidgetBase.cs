
//
// EditorCompareWidgetBase.cs
//
// Author:
//       Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Linq;
using Gtk;
using Gdk;
using System.Collections.Generic;
using Mono.TextEditor;
using Mono.TextEditor.Utils;
using MonoDevelop.Ide;
using MonoDevelop.Components;
using System.ComponentModel;
using MonoDevelop.Core;
using System.Globalization;

namespace MonoDevelop.VersionControl.Views
{
	static class Tuple
	{
		public static Tuple<S, T> Create<S, T> (S s, T t)
		{
			return new Tuple<S, T> (s, t);
		}

		public static Tuple<S, T, R > Create<S, T, R> (S s, T t, R r)
		{
			return new Tuple<S, T, R> (s, t, r);
		}
	}
	
	class Tuple<S, T>
	{
		public S Item1 {
			get;
			set;
		}
		
		public T Item2 {
			get;
			set;
		}
		public Tuple (S item1, T item2)
		{
			this.Item1 = item1;
			this.Item2 = item2;
		}
	}
	
	class Tuple<S, T, R>
	{
		public S Item1 {
			get;
			set;
		}
		
		public T Item2 {
			get;
			set;
		}
		
		public R Item3 {
			get;
			set;
		}
		
		public Tuple (S item1, T item2, R item3)
		{
			this.Item1 = item1;
			this.Item2 = item2;
			this.Item3 = item3;
		}
	}
	
	public abstract class EditorCompareWidgetBase : Gtk.Bin
	{
		internal protected VersionControlDocumentInfo info;

		Adjustment vAdjustment;
		Adjustment[] attachedVAdjustments;

		Adjustment hAdjustment;
		Adjustment[] attachedHAdjustments;

		Gtk.HScrollbar[] hScrollBars;

		DiffScrollbar rightDiffScrollBar, leftDiffScrollBar;
		MiddleArea[] middleAreas;

		protected TextEditor[] editors;
		protected Widget[] headerWidgets;

		protected List<Mono.TextEditor.Utils.Hunk> leftDiff, rightDiff;
		
		static readonly Cairo.Color lightRed = new Cairo.Color (255 / 255.0, 200 / 255.0, 200 / 255.0);
		static readonly Cairo.Color darkRed = new Cairo.Color (178 / 255.0, 140 / 255.0, 140 / 255.0);
		
		static readonly Cairo.Color lightGreen = new Cairo.Color (190 / 255.0, 240 / 255.0, 190 / 255.0);
		static readonly Cairo.Color darkGreen = new Cairo.Color (133 / 255.0, 168 / 255.0, 133 / 255.0);
		
		protected abstract TextEditor MainEditor {
			get;
		}

		public EditorCompareWidgetBase ()
		{
			CreateComponents ();

			vAdjustment = new Adjustment (0, 0, 0, 0, 0, 0);
			attachedVAdjustments = new Adjustment[editors.Length];
			attachedHAdjustments = new Adjustment[editors.Length];
			for (int i = 0; i < editors.Length; i++) {
				attachedVAdjustments[i] = new Adjustment (0, 0, 0, 0, 0, 0);
				attachedHAdjustments[i] = new Adjustment (0, 0, 0, 0, 0, 0);
			}

			foreach (var attachedAdjustment in attachedVAdjustments) {
				Connect (attachedAdjustment, vAdjustment);
			}

			hAdjustment = new Adjustment (0, 0, 0, 0, 0, 0);
			foreach (var attachedAdjustment in attachedHAdjustments) {
				Connect (attachedAdjustment, hAdjustment);
			}

			hScrollBars = new Gtk.HScrollbar[attachedHAdjustments.Length];
			for (int i = 0; i < hScrollBars.Length; i++) {
				hScrollBars[i] = new HScrollbar (hAdjustment);
				Add (hScrollBars[i]);
			}

			for (int i = 0; i < editors.Length; i++) {
				var editor = editors[i];
				Add (editor);
				editor.Caret.PositionChanged += CaretPositionChanged;
				editor.FocusInEvent += EditorFocusIn;
				editor.SetScrollAdjustments (attachedHAdjustments[i], attachedVAdjustments[i]);
			}

			if (editors.Length == 2) {
				editors[0].ExposeEvent +=  delegate (object sender, ExposeEventArgs args) {
					var myEditor = (TextEditor)sender;
					PaintEditorOverlay (myEditor, args, leftDiff, true);
				};

				editors[1].ExposeEvent +=  delegate (object sender, ExposeEventArgs args) {
					var myEditor = (TextEditor)sender;
					PaintEditorOverlay (myEditor, args, leftDiff, false);
				};
				
				rightDiffScrollBar = new DiffScrollbar (this, editors[1], true, true);
				Add (rightDiffScrollBar);
			} else {
				editors[0].ExposeEvent +=  delegate (object sender, ExposeEventArgs args) {
					var myEditor = (TextEditor)sender;
					PaintEditorOverlay (myEditor, args, leftDiff, true);
				};
				editors[1].ExposeEvent +=  delegate (object sender, ExposeEventArgs args) {
					var myEditor = (TextEditor)sender;
					PaintEditorOverlay (myEditor, args, leftDiff, false);
					PaintEditorOverlay (myEditor, args, rightDiff, false);
				};
				editors[2].ExposeEvent +=  delegate (object sender, ExposeEventArgs args) {
					var myEditor = (TextEditor)sender;
					PaintEditorOverlay (myEditor, args, rightDiff, true);
				};
				rightDiffScrollBar = new DiffScrollbar (this, editors[2], false, false);
				Add (rightDiffScrollBar);
			}
			
			leftDiffScrollBar = new DiffScrollbar (this, editors[0], true, false);
			Add (leftDiffScrollBar);
			
			foreach (var widget in headerWidgets) {
				Add (widget);
			}

			middleAreas = new MiddleArea [editors.Length - 1];
			if (middleAreas.Length <= 0 || middleAreas.Length > 2)
				throw new NotSupportedException ();

			middleAreas[0] = new MiddleArea (this, editors[0], MainEditor, true);
			Add (middleAreas[0]);

			if (middleAreas.Length == 2) {
				middleAreas[1] = new MiddleArea (this, editors[2], MainEditor, false);
				Add (middleAreas[1]);
			}
			this.MainEditor.EditorOptionsChanged += HandleMainEditorhandleEditorOptionsChanged;
		}

		void HandleMainEditorhandleEditorOptionsChanged (object sender, EventArgs e)
		{
			ClearDiffCache ();
		}
		
		public void SetVersionControlInfo (VersionControlDocumentInfo info)
		{
			this.info = info;
		}
		
		protected abstract void CreateComponents ();
		
		public static void DrawDiffRectangle (TextEditor editor, Cairo.Context cr, int startOffset, int endOffset)
		{
			var point = editor.LocationToPoint (editor.Document.OffsetToLocation (startOffset), true);
			var point2 = editor.LocationToPoint (editor.Document.OffsetToLocation (endOffset), true);
			cr.Rectangle (point.X - editor.TextViewMargin.XOffset, point.Y, point2.X - point.X, editor.LineHeight);
		}
		
		Dictionary<List<Mono.TextEditor.Utils.Hunk>, Dictionary<Hunk, Tuple<Cairo.Path, Cairo.Path>>> diffCache = new Dictionary<List<Mono.TextEditor.Utils.Hunk>, Dictionary<Hunk, Tuple<Cairo.Path, Cairo.Path>>> ();
		
		protected void ClearDiffCache ()
		{
			diffCache.Clear ();
		}
		
		static List<ISegment> BreakTextInWords (TextEditor editor, int start, int count)
		{
			var result = new List<ISegment> ();
			for (int line = start; line < start + count; line++) {
				var lineSegment = editor.Document.GetLine (line);
				int offset = lineSegment.Offset;
				bool wasIdentifierPart = false;
				int lastWordEnd = 0;
				for (int i = 0; i < lineSegment.EditableLength; i++) {
					char ch = editor.GetCharAt (offset + i);
					bool isIdentifierPart = char.IsLetterOrDigit (ch) || ch == '_';
					if (!isIdentifierPart) {
						if (wasIdentifierPart)
							result.Add (new Mono.TextEditor.Segment (offset + lastWordEnd, i - lastWordEnd));
						result.Add (new Mono.TextEditor.Segment (offset + i, 1));
						lastWordEnd = i + 1;
					}
					wasIdentifierPart = isIdentifierPart;
				}
			}
			return result;
		}
		
		Cairo.Path CalculateChunkPath (Cairo.Context cr, TextEditor editor, List<Hunk> diff, List<ISegment> words, bool useRemove)
		{
			int startOffset = -1;
			int endOffset = -1;
			foreach (var hunk in diff) {
				int start = useRemove ? hunk.RemoveStart : hunk.InsertStart;
				int count = useRemove ? hunk.Removed : hunk.Inserted;
				for (int i = 0; i < count; i++) {
					var word = words[start + i];
					if (endOffset != word.Offset) {
						if (startOffset >= 0)
							DrawDiffRectangle (editor, cr, startOffset, endOffset);
						startOffset = word.Offset;
					}
					endOffset = word.EndOffset;
				}
			}
			if (startOffset >= 0)
				DrawDiffRectangle (editor, cr, startOffset, endOffset);
			var result = cr.CopyPath ();
			cr.NewPath ();
			return result;
		}
		
		Tuple<Cairo.Path, Cairo.Path> GetDiffPaths (List<Mono.TextEditor.Utils.Hunk> diff, TextEditor editor, Cairo.Context cr, Hunk hunk)
		{
			if (!diffCache.ContainsKey (diff))
				diffCache[diff] = new Dictionary<Hunk, Tuple<Cairo.Path, Cairo.Path>> ();
			var pathCache = diffCache[diff];
			
			Tuple<Cairo.Path, Cairo.Path> result;
			if (pathCache.TryGetValue (hunk, out result))
				return result;
			
			var words = BreakTextInWords (editor, hunk.RemoveStart, hunk.Removed);
			var cmpWords = BreakTextInWords (MainEditor, hunk.InsertStart, hunk.Inserted);
			
			var wordDiff = new List<Hunk> (Diff.GetDiff (words.Select (w => editor.GetTextAt (w)).ToArray (),
				cmpWords.Select (w => MainEditor.GetTextAt (w)).ToArray ()));
			
			result = Tuple.Create (CalculateChunkPath (cr, editor, wordDiff, words, true), 
				CalculateChunkPath (cr, MainEditor, wordDiff, cmpWords, false));
			
			pathCache[hunk] = result;
			return result;
		}
		
		public virtual void UpdateDiff ()
		{
			ClearDiffCache ();
		}
		public abstract void CreateDiff ();

		void RedrawMiddleAreas ()
		{
			foreach (var middleArea in middleAreas) {
				middleArea.QueueDraw ();
			}
		}
		
		void Connect (Adjustment fromAdj, Adjustment toAdj)
		{
			fromAdj.Changed += AdjustmentChanged;
			fromAdj.ValueChanged += delegate {
				double fromValue = fromAdj.Value / (fromAdj.Upper - fromAdj.Lower);
				if (toAdj.Value != fromValue)
					toAdj.Value = fromValue;
				RedrawMiddleAreas ();
			};

			toAdj.ValueChanged += delegate {
				double toValue = System.Math.Round (toAdj.Value * (fromAdj.Upper - fromAdj.Lower)); 
				if (fromAdj.Value != toValue)
					fromAdj.Value = toValue;
				RedrawMiddleAreas ();
			};
		}

		void AdjustmentChanged (object sender, EventArgs e)
		{
			vAdjustment.SetBounds (0, 1.0,
				attachedVAdjustments.Select (adj => adj.StepIncrement / (adj.Upper - adj.Lower)).Min (),
				attachedVAdjustments.Select (adj => adj.PageIncrement / (adj.Upper - adj.Lower)).Min (),
				attachedVAdjustments.Select (adj => adj.PageSize / (adj.Upper - adj.Lower)).Min ());
			
			
			
			hAdjustment.SetBounds (0, 1.0,
				attachedHAdjustments.Select (adj => adj.StepIncrement / (adj.Upper - adj.Lower)).Min (),
				attachedHAdjustments.Select (adj => adj.PageIncrement / (adj.Upper - adj.Lower)).Min (),
				attachedHAdjustments.Select (adj => adj.PageSize / (adj.Upper - adj.Lower)).Min ());
			
		}

		internal static void EditorFocusIn (object sender, FocusInEventArgs args)
		{
			TextEditor editor = (TextEditor)sender;
			UpdateCaretPosition (editor.Caret);
		}

		internal static void CaretPositionChanged (object sender, DocumentLocationEventArgs e)
		{
			Caret caret = (Caret)sender;
			UpdateCaretPosition (caret);
		}

		static void UpdateCaretPosition (Caret caret)
		{
			int offset = caret.Offset;
			if (offset < 0 || offset > caret.TextEditorData.Document.Length)
				return;
			DocumentLocation location = caret.TextEditorData.LogicalToVisualLocation (caret.Location);
			IdeApp.Workbench.StatusBar.ShowCaretState (caret.Line + 1,
			                                           location.Column + 1,
			                                           caret.TextEditorData.IsSomethingSelected ? caret.TextEditorData.SelectionRange.Length : 0,
			                                           caret.IsInInsertMode);
		}

		#region Container implementation
		List<ContainerChild> children = new List<ContainerChild> ();
		public override ContainerChild this [Widget w] {
			get {
				return children.FirstOrDefault (c => c.Child == w);
			}
		}

		protected EditorCompareWidgetBase (IntPtr ptr) : base (ptr)
		{
		}

		public override GLib.GType ChildType ()
		{
			return Gtk.Widget.GType;
		}

		protected override void ForAll (bool include_internals, Gtk.Callback callback)
		{
			if (include_internals)
				children.ForEach (child => callback (child.Child));
		}

		protected override void OnAdded (Widget widget)
		{
			widget.Parent = this;
			children.Add (new ContainerChild (this, widget));
			widget.Show ();
		}

		protected override void OnRemoved (Widget widget)
		{
			widget.Unparent ();
			children.RemoveAll (c => c.Child == widget);
		}

		protected override void OnDestroyed ()
		{
			base.OnDestroyed ();
			children.ForEach (child => child.Child.Destroy ());
		}

		#endregion

		protected override void OnSizeAllocated (Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
			const int overviewWidth = 16;
			int vwidth = 1;

			bool hScrollBarVisible = hScrollBars[0].Visible;

			int hheight = hScrollBarVisible ? hScrollBars[0].Requisition.Height : 0;
			int headerSize = 0;

			if (headerWidgets != null)
				headerSize = System.Math.Max (headerWidgets[0].SizeRequest ().Height, 16);

			Rectangle childRectangle = new Rectangle (allocation.X + overviewWidth + 1, allocation.Y + headerSize + 1, allocation.Width - vwidth - overviewWidth * 2, allocation.Height - hheight - headerSize - 1);
			
			
			leftDiffScrollBar.SizeAllocate (new Rectangle (allocation.Left, childRectangle.Y, overviewWidth - 1, childRectangle.Height));
			rightDiffScrollBar.SizeAllocate (new Rectangle (allocation.Right - overviewWidth + 1, childRectangle.Y, overviewWidth - 1, childRectangle.Height ));

			const int middleAreaWidth = 42;
			int editorWidth = (childRectangle.Width - middleAreaWidth * (editors.Length - 1)) / editors.Length;

			for (int i = 0; i < editors.Length; i++) {
				Rectangle editorRectangle = new Rectangle (childRectangle.X + (editorWidth + middleAreaWidth) * i  , childRectangle.Top, editorWidth, childRectangle.Height);
				editors[i].SizeAllocate (editorRectangle);

				if (hScrollBarVisible)
					hScrollBars[i].SizeAllocate (new Rectangle (editorRectangle.X, editorRectangle.Bottom, editorRectangle.Width, hheight));

				if (headerWidgets != null)
					headerWidgets[i].SizeAllocate (new Rectangle (editorRectangle.X, allocation.Y + 1, editorRectangle.Width, headerSize));
			}

			for (int i = 0; i < middleAreas.Length; i++) {
				middleAreas[i].SizeAllocate (new Rectangle (childRectangle.X + editorWidth * (i + 1) + middleAreaWidth * i, childRectangle.Top, middleAreaWidth + 1, childRectangle.Height));
			}
		}

		static double GetWheelDelta (Adjustment adjustment, ScrollDirection direction)
		{
			double delta = adjustment.StepIncrement * 4;
			if (direction == ScrollDirection.Up || direction == ScrollDirection.Left)
				delta = -delta;
			return delta;
		}

		protected override bool OnScrollEvent (EventScroll evnt)
		{
			var adjustment = (evnt.Direction == ScrollDirection.Up || evnt.Direction == ScrollDirection.Down) ? vAdjustment : hAdjustment;

			if (adjustment.PageSize < adjustment.Upper) {
				double newValue = adjustment.Value + GetWheelDelta (adjustment, evnt.Direction);
				newValue = System.Math.Max (System.Math.Min (adjustment.Upper  - adjustment.PageSize, newValue), adjustment.Lower);
				adjustment.Value = newValue;
			}
			return base.OnScrollEvent (evnt);
		}

		protected override void OnSizeRequested (ref Gtk.Requisition requisition)
		{
			base.OnSizeRequested (ref requisition);
			children.ForEach (child => child.Child.SizeRequest ());
		}

		public static Cairo.Color GetColor (Mono.TextEditor.Utils.Hunk hunk, bool removeSide, bool dark, double alpha)
		{
			Cairo.Color result;
			
			if (removeSide) {
				if (hunk.Removed > 0) {
					result = dark ? darkRed : lightRed;
				} else {
					result = dark ? darkGreen : lightGreen;
				}
			} else {
				if (hunk.Inserted > 0) {
					result = dark ? darkGreen : lightGreen;
				} else {
					result = dark ? darkRed : lightRed;
				}
			}
			result.A = alpha;
			return result;
		}
		
		void PaintEditorOverlay (TextEditor editor, ExposeEventArgs args, List<Mono.TextEditor.Utils.Hunk> diff, bool paintRemoveSide)
		{
			if (diff == null)
				return;
			using (var cr = Gdk.CairoHelper.Create (args.Event.Window)) {
				foreach (var hunk in diff) {
					double y1 = editor.LineToY (paintRemoveSide ? hunk.RemoveStart : hunk.InsertStart) - editor.VAdjustment.Value;
					double y2 = editor.LineToY (paintRemoveSide ? hunk.RemoveStart + hunk.Removed : hunk.InsertStart + hunk.Inserted) - editor.VAdjustment.Value;
					if (y1 == y2)
						y2 = y1 + 1;
					cr.Rectangle (0, y1, editor.Allocation.Width, y2 - y1);
					cr.Color = GetColor (hunk, paintRemoveSide, false, 0.15);
					cr.Fill ();
					
					var paths = GetDiffPaths (diff, editors[0], cr, hunk);
					
					cr.Save ();
					cr.Translate (-editor.HAdjustment.Value + editor.TextViewMargin.XOffset, -editor.VAdjustment.Value);
					cr.AppendPath (paintRemoveSide ? paths.Item1 : paths.Item2);
					cr.Color = GetColor (hunk, paintRemoveSide, false, 0.3);
					cr.Fill ();
					cr.Restore ();
					
					cr.Color = GetColor (hunk, paintRemoveSide, true, 0.15);
					cr.MoveTo (0, y1);
					cr.LineTo (editor.Allocation.Width, y1);
					cr.Stroke ();

					cr.MoveTo (0, y2);
					cr.LineTo (editor.Allocation.Width, y2);
					cr.Stroke ();
				}
			}
		}

		Dictionary<Mono.TextEditor.Document, TextEditorData> dict = new Dictionary<Mono.TextEditor.Document, TextEditorData> ();

		List<TextEditorData> localUpdate = new List<TextEditorData> ();

		void HandleInfoDocumentTextEditorDataDocumentTextReplaced (object sender, ReplaceEventArgs e)
		{
			foreach (var data in localUpdate.ToArray ()) {
				data.Document.TextReplaced -= HandleDataDocumentTextReplaced;
				data.Replace (e.Offset, e.Count, e.Value);
				data.Document.TextReplaced += HandleDataDocumentTextReplaced;
				data.Document.CommitUpdateAll ();
			}
		}

		public void SetLocal (TextEditorData data)
		{
			if (info == null)
				throw new InvalidOperationException ("Version control info must be set before attaching the merge view to an editor.");
			dict[data.Document] = data;
			data.Document.Text = info.Document.Editor.Document.Text;
			data.Document.ReadOnly = false;
			CreateDiff ();
			data.Document.TextReplaced += HandleDataDocumentTextReplaced;
		}

		void HandleDataDocumentTextReplaced (object sender, ReplaceEventArgs e)
		{
			var data = dict[(Document)sender];
			localUpdate.Remove (data);
			info.Document.Editor.Replace (e.Offset, e.Count, e.Value);
			localUpdate.Add (data);
			UpdateDiff ();
		}

		public void RemoveLocal (TextEditorData data)
		{
			localUpdate.Remove (data);
			data.Document.ReadOnly = true;
			data.Document.TextReplaced -= HandleDataDocumentTextReplaced;
		}

		protected virtual void UndoChange (TextEditor fromEditor, TextEditor toEditor, Hunk hunk)
		{
			toEditor.Document.BeginAtomicUndo ();
			var start = toEditor.Document.GetLine (hunk.InsertStart);
			int toOffset = start != null ? start.Offset : toEditor.Document.Length;
			if (start != null && hunk.Inserted > 0) {
				int line = Math.Min (hunk.InsertStart + hunk.Inserted - 1, toEditor.Document.LineCount - 1);
				var end = toEditor.Document.GetLine (line);
				toEditor.Remove (start.Offset, end.EndOffset - start.Offset);
			}

			if (hunk.Removed > 0) {
				start = fromEditor.Document.GetLine (Math.Min (hunk.RemoveStart, fromEditor.Document.LineCount - 1));
				int line = Math.Min (hunk.RemoveStart + hunk.Removed - 1, fromEditor.Document.LineCount - 1);
				var end = fromEditor.Document.GetLine (line);
				toEditor.Insert (toOffset, start.Offset == end.EndOffset ? toEditor.EolMarker : fromEditor.Document.GetTextBetween (start.Offset, end.EndOffset));
			}

			toEditor.Document.EndAtomicUndo ();
		}

		class MiddleArea : DrawingArea
		{
			EditorCompareWidgetBase widget;
			TextEditor fromEditor, toEditor;
			bool useLeft;

			IEnumerable<Mono.TextEditor.Utils.Hunk> Diff {
				get {
					return useLeft ? widget.leftDiff : widget.rightDiff;
				}
			}

			public MiddleArea (EditorCompareWidgetBase widget, TextEditor fromEditor, TextEditor toEditor, bool useLeft)
			{
				this.widget = widget;
				this.Events |= EventMask.PointerMotionMask | EventMask.ButtonPressMask;
				this.fromEditor = fromEditor;
				this.toEditor = toEditor;
				this.useLeft = useLeft;
				this.toEditor.EditorOptionsChanged += HandleToEditorhandleEditorOptionsChanged;
			}
			
			protected override void OnDestroyed ()
			{
				this.toEditor.EditorOptionsChanged -= HandleToEditorhandleEditorOptionsChanged;
				base.OnDestroyed ();
			}
			
			void HandleToEditorhandleEditorOptionsChanged (object sender, EventArgs e)
			{
				QueueDraw ();
			}

			Mono.TextEditor.Utils.Hunk selectedHunk = Mono.TextEditor.Utils.Hunk.Empty;
			protected override bool OnMotionNotifyEvent (EventMotion evnt)
			{
				bool hideButton = widget.MainEditor.Document.ReadOnly;
				Mono.TextEditor.Utils.Hunk selectedHunk = Mono.TextEditor.Utils.Hunk.Empty;
				if (!hideButton) {
					int delta = widget.MainEditor.Allocation.Y - Allocation.Y;
					foreach (var hunk in Diff) {
						double z1 = delta + fromEditor.LineToY (hunk.RemoveStart) - fromEditor.VAdjustment.Value;
						double z2 = delta + fromEditor.LineToY (hunk.RemoveStart + hunk.Removed) - fromEditor.VAdjustment.Value;
						if (z1 == z2)
							z2 = z1 + 1;

						double y1 = delta + toEditor.LineToY (hunk.InsertStart) - toEditor.VAdjustment.Value;
						double y2 = delta + toEditor.LineToY (hunk.InsertStart + hunk.Inserted) - toEditor.VAdjustment.Value;

						if (y1 == y2)
							y2 = y1 + 1;
						double x, y, w, h;
						GetButtonPosition (hunk, y1, y2, z1, z2, out x, out y, out w, out h);

						if (evnt.X >= x && evnt.X < x + w && evnt.Y >= y && evnt.Y < y + h) {
							selectedHunk = hunk;
							TooltipText = GettextCatalog.GetString ("Revert this change");
							break;
						}
					}
				} else {
					selectedHunk = Mono.TextEditor.Utils.Hunk.Empty;
				}

				if (selectedHunk.IsEmpty)
					TooltipText = null;

				if (this.selectedHunk != selectedHunk) {
					this.selectedHunk = selectedHunk;
					QueueDraw ();
				}
				return base.OnMotionNotifyEvent (evnt);
			}

			protected override bool OnButtonPressEvent (EventButton evnt)
			{
				if (!selectedHunk.IsEmpty)
					widget.UndoChange (fromEditor, toEditor, selectedHunk);
				return base.OnButtonPressEvent (evnt);
			}

			protected override bool OnLeaveNotifyEvent (EventCrossing evnt)
			{
				selectedHunk = Mono.TextEditor.Utils.Hunk.Empty;
				TooltipText = null;
				QueueDraw ();
				return base.OnLeaveNotifyEvent (evnt);
			}

			const int buttonSize = 16;
			double lineWidth;

			public bool GetButtonPosition (Mono.TextEditor.Utils.Hunk hunk, double y1, double y2, double z1, double z2, out double x, out double y, out double w, out double h)
			{
				if (hunk.Removed > 0) {
					var b1 = z1;
					var b2 = z2;
					x = useLeft ? lineWidth : Allocation.Width - buttonSize;
					y = b1;
					w = buttonSize;
					h = b2 - b1;
					return hunk.Inserted > 0;
				} else {
					var b1 = y1;
					var b2 = y2;

					x = useLeft ? Allocation.Width - buttonSize : lineWidth;
					y = b1;
					w = buttonSize - lineWidth;
					h = b2 - b1;
					return  hunk.Removed > 0;
				}
			}

			void DrawArrow (Cairo.Context cr, double x, double y)
			{
				if (useLeft) {
					cr.MoveTo (x - 2, y - 3);
					cr.LineTo (x + 2, y);
					cr.LineTo (x - 2, y + 3);
				} else {
					cr.MoveTo (x + 2, y - 3);
					cr.LineTo (x - 2, y);
					cr.LineTo (x + 2, y + 3);
				}
			}
			static void DrawCross (Cairo.Context cr, double x, double y)
			{
				cr.MoveTo (x - 2, y - 3);
				cr.LineTo (x + 2, y + 3);
				cr.MoveTo (x + 2, y - 3);
				cr.LineTo (x - 2, y + 3);
			}

			protected override bool OnExposeEvent (EventExpose evnt)
			{
				bool hideButton = widget.MainEditor.Document.ReadOnly;
				using (Cairo.Context cr = Gdk.CairoHelper.Create (evnt.Window)) {
					lineWidth = cr.LineWidth;
					int delta = widget.MainEditor.Allocation.Y - Allocation.Y;
					foreach (Mono.TextEditor.Utils.Hunk hunk in Diff) {
						double z1 = delta + fromEditor.LineToY (hunk.RemoveStart) - fromEditor.VAdjustment.Value;
						double z2 = delta + fromEditor.LineToY (hunk.RemoveStart + hunk.Removed) - fromEditor.VAdjustment.Value;
						if (z1 == z2)
							z2 = z1 + 1;

						double y1 = delta + toEditor.LineToY (hunk.InsertStart) - toEditor.VAdjustment.Value;
						double y2 = delta + toEditor.LineToY (hunk.InsertStart + hunk.Inserted) - toEditor.VAdjustment.Value;

						if (y1 == y2)
							y2 = y1 + 1;

						if (!useLeft) {
							var tmp = z1;
							z1 = y1;
							y1 = tmp;

							tmp = z2;
							z2 = y2;
							y2 = tmp;
						}

						int x1 = 0;
						int x2 = Allocation.Width;

						if (!hideButton) {
							if (useLeft && hunk.Removed > 0 || !useLeft && hunk.Removed == 0) {
								x1 += 16;
							} else {
								x2 -= 16;
							}
						}

						if (z1 == z2)
							z2 = z1 + 1;

						cr.MoveTo (x1, z1);
						cr.CurveTo ((x2 - x1) / 2, z1,
							(x2 - x1) / 2,  y1,
							x2, y1);

						cr.LineTo (x2, y2);
						cr.CurveTo ((x2 - x1) / 2, y2,
							(x2 - x1) / 2, z2,
							x1, z2);
						cr.ClosePath ();
						cr.Color = GetColor (hunk, this.useLeft, false, 1.0);
						cr.Fill ();

						cr.Color = GetColor (hunk, this.useLeft, true, 1.0);
						cr.MoveTo (x2, y1);
						cr.CurveTo ((x2 - x1) / 2, y1,
							(x2 - x1) / 2,  z1,
							x1, z1);
						cr.Stroke ();

						cr.MoveTo (x1, z2);
						cr.CurveTo ((x2 - x1) / 2, z2,
							(x2 - x1) / 2, y2,
							x2, y2);
						cr.Stroke ();

						if (!hideButton) {
							bool isButtonSelected = hunk == selectedHunk;

							double x, y, w, h;
							bool drawArrow = useLeft ? GetButtonPosition (hunk, y1, y2, z1, z2, out x, out y, out w, out h) :
								GetButtonPosition (hunk, z1, z2, y1, y2, out x, out y, out w, out h);

							cr.Rectangle (x, y, w, h);
							if (isButtonSelected) {
								cr.Color = new Cairo.Color (0.7, 0.7, 0.7, 0.3);
								cr.FillPreserve ();
							}
							cr.Color = new Cairo.Color (0.7, 0.7, 0.7);
							cr.Stroke ();
							cr.LineWidth = 1;
							cr.Color = new Cairo.Color (0, 0, 0);
							if (drawArrow) {
								DrawArrow (cr, x + w / 1.5, y + h / 2);
								DrawArrow (cr, x + w / 2.5, y + h / 2);
							} else {
								DrawCross (cr, x + w / 2 , y + (h) / 2);
							}
							cr.Stroke ();
						}
					}
				}
				var result = base.OnExposeEvent (evnt);

				Gdk.GC gc = Style.DarkGC (State);
				evnt.Window.DrawLine (gc, Allocation.X, Allocation.Top, Allocation.X, Allocation.Bottom);
				evnt.Window.DrawLine (gc, Allocation.Right, Allocation.Top, Allocation.Right, Allocation.Bottom);

				evnt.Window.DrawLine (gc, Allocation.Left, Allocation.Y, Allocation.Right, Allocation.Y);
				evnt.Window.DrawLine (gc, Allocation.Left, Allocation.Bottom, Allocation.Right, Allocation.Bottom);

				return result;
			}
		}

		class DiffScrollbar : DrawingArea
		{
			TextEditor editor;
			EditorCompareWidgetBase widget;
			bool useLeftDiff;
			bool paintInsert;
			
			public DiffScrollbar (EditorCompareWidgetBase widget, TextEditor editor, bool useLeftDiff, bool paintInsert)
			{
				this.editor = editor;
				this.useLeftDiff = useLeftDiff;
				this.paintInsert = paintInsert;
				this.widget = widget;
				widget.vAdjustment.ValueChanged += delegate {
					QueueDraw ();
				};
				WidthRequest = 50;

				Events |= EventMask.ButtonPressMask | EventMask.ButtonReleaseMask | EventMask.ButtonMotionMask;

				Show ();
			}

			public void MouseMove (double y)
			{
				var adj = widget.vAdjustment;
				double position = (y / Allocation.Height) * adj.Upper - (double)adj.PageSize / 2;
				position = Math.Max (0, Math.Min (position, adj.Upper - adj.PageSize));
				widget.vAdjustment.Value = position;
			}

			protected override bool OnMotionNotifyEvent (EventMotion evnt)
			{
				if (button != 0)
					MouseMove (evnt.Y);
				return base.OnMotionNotifyEvent (evnt);
			}

			uint button;
			protected override bool OnButtonPressEvent (EventButton evnt)
			{
				button |= evnt.Button;
				MouseMove (evnt.Y);
				return base.OnButtonPressEvent (evnt);
			}

			protected override bool OnButtonReleaseEvent (EventButton evnt)
			{
				button &= ~evnt.Button;
				return base.OnButtonReleaseEvent (evnt);
			}

			protected override bool OnExposeEvent (Gdk.EventExpose e)
			{
				if (widget.leftDiff == null)
					return true;
				var adj = widget.vAdjustment;
				
				var diff = useLeftDiff ? widget.leftDiff : widget.rightDiff;
				
				using (Cairo.Context cr = Gdk.CairoHelper.Create (e.Window)) {
					cr.LineWidth = 1;
					double curY = 0;
					
					foreach (var hunk in diff) {
						double y, count;
						if (paintInsert) {
							y = hunk.InsertStart / (double)editor.LineCount;
							count = hunk.Inserted / (double)editor.LineCount;
						} else {
							y = hunk.RemoveStart / (double)editor.LineCount;
							count = hunk.Removed / (double)editor.LineCount;
						}
						
						double start  = y *  Allocation.Height;
						cr.Rectangle (0.5, 0.5 + curY, Allocation.Width, (start - curY));
						cr.Color = new Cairo.Color (1, 1, 1);
						cr.Fill ();
						
						curY = start;
						double height = Math.Max (cr.LineWidth, count * Allocation.Height);
						cr.Rectangle (0.5, 0.5 + curY, Allocation.Width, height);
						cr.Color = GetColor (hunk, !paintInsert, false, 1.0);
						cr.Fill ();
						curY += height;
					}
					
					cr.Rectangle (0.5, 0.5 + curY, Allocation.Width, Allocation.Height - curY);
					cr.Color = new Cairo.Color (1, 1, 1);
					cr.Fill ();

					cr.Rectangle (1,
					              Allocation.Height * adj.Value / adj.Upper + cr.LineWidth + 0.5,
					              Allocation.Width - 2,
					              Allocation.Height * (adj.PageSize / adj.Upper));
					cr.Color = new Cairo.Color (0, 0, 0, 0.5);
					cr.StrokePreserve ();

					cr.Color = new Cairo.Color (0, 0, 0, 0.03);
					cr.Fill ();
					cr.Rectangle (0.5, 0.5, Allocation.Width - 1, Allocation.Height - 1);
					cr.Color = (Mono.TextEditor.HslColor)Style.Dark (StateType.Normal);
					cr.Stroke ();
				}
				return true;
			}

			void IncPos(Mono.TextEditor.Utils.Hunk h, ref int pos)
			{
				pos += System.Math.Max (h.Inserted, h.Removed);
			}
		}
	}

}
