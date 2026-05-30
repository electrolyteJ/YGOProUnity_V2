using System;
using System.Collections.Generic;
using UnityEngine;

namespace App.UI.Common.States
{
    public sealed class BusyOverlayHost : MonoBehaviour
    {
        public sealed class NoOpScope : IDisposable
        {
            private static readonly NoOpScope instance = new NoOpScope();

            public static NoOpScope Instance
            {
                get { return instance; }
            }

            public void Dispose()
            {
            }
        }

        private sealed class BusyScope : IDisposable
        {
            private BusyOverlayHost owner;
            private int requestId;

            public BusyScope(BusyOverlayHost ownerHost, int requestIdValue)
            {
                owner = ownerHost;
                requestId = requestIdValue;
            }

            public void Dispose()
            {
                if (owner == null)
                {
                    return;
                }

                owner.ReleaseBusy(requestId);
                owner = null;
                requestId = 0;
            }
        }

        private struct BusyRequest
        {
            public int Id;
            public string Message;
        }

        private readonly List<BusyRequest> busyRequests = new List<BusyRequest>();
        private int nextRequestId = 1;
        private string errorMessage = string.Empty;

        public event Action StateChanged;

        public bool IsBusy
        {
            get { return busyRequests.Count > 0; }
        }

        public int BusyDepth
        {
            get { return busyRequests.Count; }
        }

        public string BusyMessage
        {
            get
            {
                if (busyRequests.Count == 0)
                {
                    return string.Empty;
                }

                return busyRequests[busyRequests.Count - 1].Message ?? string.Empty;
            }
        }

        public string ErrorMessage
        {
            get { return errorMessage; }
        }

        public bool HasError
        {
            get { return !string.IsNullOrEmpty(errorMessage); }
        }

        public IDisposable PushBusy(string message)
        {
            BusyRequest request = new BusyRequest
            {
                Id = nextRequestId,
                Message = message ?? string.Empty
            };

            nextRequestId += 1;
            busyRequests.Add(request);
            RaiseStateChanged();
            return new BusyScope(this, request.Id);
        }

        public void ShowError(string message)
        {
            errorMessage = message ?? string.Empty;
            RaiseStateChanged();
        }

        public void ClearError()
        {
            if (string.IsNullOrEmpty(errorMessage))
            {
                return;
            }

            errorMessage = string.Empty;
            RaiseStateChanged();
        }

        public void ClearBusy()
        {
            if (busyRequests.Count == 0)
            {
                return;
            }

            busyRequests.Clear();
            RaiseStateChanged();
        }

        private void ReleaseBusy(int requestId)
        {
            for (int i = busyRequests.Count - 1; i >= 0; i--)
            {
                if (busyRequests[i].Id == requestId)
                {
                    busyRequests.RemoveAt(i);
                    RaiseStateChanged();
                    return;
                }
            }
        }

        private void RaiseStateChanged()
        {
            Action stateChanged = StateChanged;
            if (stateChanged != null)
            {
                stateChanged();
            }
        }
    }
}
