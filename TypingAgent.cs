using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using System.Threading;
using System.Windows.Threading;
using Microsoft.VisualStudio.Text.Tagging;

namespace TypingAgent
{
    /// <summary>
    /// An agent that types a given string in the given buffer over time.
    /// </summary>
    internal class TypingAgent
    {
        private ITrackingPoint _insertionPoint;
        private string _textToType;
        private Dispatcher _dispatcher;

        public TypingAgent(RemoteAgentTag tag, string textToType, SnapshotPoint insertionPoint, Dispatcher dispatcher)
        {
            Tag = tag;

            _textToType = textToType;
            _insertionPoint = insertionPoint.Snapshot.CreateTrackingPoint(insertionPoint, PointTrackingMode.Positive);
            _dispatcher = dispatcher;
        }

        /// <summary>
        /// The span of text inserted by this agent.
        /// </summary>
        public ITrackingSpan InsertionSpan { get; private set;}

        /// <summary>
        /// The agent's identifying tag.
        /// </summary>
        public RemoteAgentTag Tag { get; private set; }

        /// <summary>
        /// Raised whenever the InsertionSpan is updated.
        /// </summary>
        public event EventHandler InsertionSpanUpdated;

        public void Start()
        {
            // On a background thread, perform our typing work.
            ThreadPool.QueueUserWorkItem((state) =>
            {
                Random random = new Random();

                for (int i = 0; i < _textToType.Length; i++)
                {
                    // Dispatch back to the UI thread to make the edit on the buffer
                    _dispatcher.Invoke(new Action(() => 
                    {
                        ITextSnapshot snapshot = _insertionPoint.TextBuffer.CurrentSnapshot;
                        SnapshotPoint insertionPoint = _insertionPoint.GetPoint(snapshot);

                        if (i == 0)
                            CaptureInsertionSpan(insertionPoint);

                        snapshot.TextBuffer.Insert(insertionPoint, _textToType.Substring(i, 1));

                    }), DispatcherPriority.Normal);

                    // Between each typing action, sleep for a little while
                    Thread.Sleep(50 + random.Next(200));
                }
            });
        }

        private void CaptureInsertionSpan(SnapshotPoint insertionPoint)
        {
            InsertionSpan = insertionPoint.Snapshot.CreateTrackingSpan(new SnapshotSpan(insertionPoint, insertionPoint), SpanTrackingMode.EdgeInclusive);
            var temp = InsertionSpanUpdated;
            if (temp != null)
                temp(this, EventArgs.Empty);
        }
    }
}
