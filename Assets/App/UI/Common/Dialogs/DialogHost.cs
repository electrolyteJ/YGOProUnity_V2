using System;
using System.Collections.Generic;
using UnityEngine;

namespace App.UI.Common.Dialogs
{
    public sealed class DialogHost : MonoBehaviour
    {
        public sealed class DialogRequest
        {
            private readonly Action onDismissed;
            private readonly Action<bool> onCompleted;

            public DialogRequest(string title, string message, bool requiresDecision, Action onDismissedCallback, Action<bool> onCompletedCallback)
            {
                Title = title ?? string.Empty;
                Message = message ?? string.Empty;
                RequiresDecision = requiresDecision;
                onDismissed = onDismissedCallback;
                onCompleted = onCompletedCallback;
            }

            public string Title { get; private set; }

            public string Message { get; private set; }

            public bool RequiresDecision { get; private set; }

            internal void Complete(bool confirmed)
            {
                if (onCompleted != null)
                {
                    onCompleted(confirmed);
                }

                if (onDismissed != null)
                {
                    onDismissed();
                }
            }
        }

        private DialogRequest currentDialog;
        private readonly Queue<DialogRequest> pendingDialogs = new Queue<DialogRequest>();

        public event Action<DialogRequest> DialogShown;

        public event Action<DialogRequest, bool> DialogClosed;

        public DialogRequest CurrentDialog
        {
            get { return currentDialog; }
        }

        public bool HasActiveDialog
        {
            get { return currentDialog != null; }
        }

        public int PendingCount
        {
            get { return pendingDialogs.Count; }
        }

        public void ShowMessage(string title, string message)
        {
            ShowMessage(title, message, null);
        }

        public void ShowMessage(string title, string message, Action onDismissed)
        {
            ShowDialog(new DialogRequest(title, message, false, onDismissed, null));
        }

        public void ShowConfirmation(string title, string message, Action<bool> onCompleted)
        {
            ShowDialog(new DialogRequest(title, message, true, null, onCompleted));
        }

        public void Dismiss()
        {
            CompleteCurrentDialog(false);
        }

        public void Confirm()
        {
            CompleteCurrentDialog(true);
        }

        public void Cancel()
        {
            CompleteCurrentDialog(false);
        }

        public void Clear()
        {
            currentDialog = null;
            pendingDialogs.Clear();
        }

        private void ShowDialog(DialogRequest request)
        {
            if (request == null)
            {
                return;
            }

            if (currentDialog != null)
            {
                pendingDialogs.Enqueue(request);
                return;
            }

            currentDialog = request;

            Action<DialogRequest> dialogShown = DialogShown;
            if (dialogShown != null)
            {
                dialogShown(request);
            }
        }

        private void CompleteCurrentDialog(bool confirmed)
        {
            if (currentDialog == null)
            {
                return;
            }

            DialogRequest completedDialog = currentDialog;
            currentDialog = null;
            completedDialog.Complete(confirmed);

            Action<DialogRequest, bool> dialogClosed = DialogClosed;
            if (dialogClosed != null)
            {
                dialogClosed(completedDialog, confirmed);
            }

            ShowNextDialogIfNeeded();
        }

        private void ShowNextDialogIfNeeded()
        {
            if (currentDialog != null || pendingDialogs.Count == 0)
            {
                return;
            }

            ShowDialog(pendingDialogs.Dequeue());
        }
    }
}
