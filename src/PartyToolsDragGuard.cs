using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ErenshorPartyTools
{
    // Left-button gesture ownership begins at pointer-down, before Unity's drag threshold. The
    // process registry is shared by key so the final participating suite owner restores the native
    // pre-gesture value instead of blindly clearing another UI owner's claim.
    internal sealed class PartyToolsDragGuard : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerUpHandler
    {
        private const string ProcessOwnersKey = "forgetwhtuno.erenshor.ui.drag.owners.v1";
        private const string ProcessBaselineKey = "forgetwhtuno.erenshor.ui.drag.nativeBaseline.v1";
        private const string ProcessBaselineCapturedKey = "forgetwhtuno.erenshor.ui.drag.nativeBaselineCaptured.v1";
        private const string ProcessOwner = "forgetwhtuno.erenshor.partytools";
        private static int _owned;
        private static int _epoch;

        internal RectTransform Target;
        internal Action Completed;
        internal Action Activated;
        private RectTransform _parent;
        private Vector2 _startPointer, _startPosition;
        private bool _dragging, _owning;
        private int _ownerEpoch;

        internal static bool OwnsPointerGesture { get { return _owned > 0; } }

        public void OnPointerDown(PointerEventData e)
        {
            if (e == null || e.button != PointerEventData.InputButton.Left) return;
            try { if (Activated != null) Activated(); } catch { }
            Acquire();
        }

        public void OnBeginDrag(PointerEventData e)
        {
            if (e == null || e.button != PointerEventData.InputButton.Left) return;
            if (Target == null) Target = GetComponent<RectTransform>();
            _parent = Target == null ? null : Target.parent as RectTransform;
            if (_parent == null) { End(false); return; }
            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_parent, e.position, e.pressEventCamera, out local)) { End(false); return; }
            _startPointer = local; _startPosition = Target.anchoredPosition; _dragging = true;
            Acquire();
        }

        public void OnDrag(PointerEventData e)
        {
            if (e == null || e.button != PointerEventData.InputButton.Left || !_dragging || _parent == null || Target == null) return;
            Reassert();
            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_parent, e.position, e.pressEventCamera, out local)) return;
            Target.anchoredPosition = _startPosition + (local - _startPointer);
        }

        public void OnEndDrag(PointerEventData e) { if (e == null || e.button == PointerEventData.InputButton.Left) End(true); }
        public void OnPointerUp(PointerEventData e) { if (e == null || e.button == PointerEventData.InputButton.Left) End(true); }

        private void Update()
        {
            if (!_owning) return;
            Reassert();
            try { if (!Input.GetMouseButton(0)) End(_dragging); } catch { End(false); }
        }

        private void OnApplicationFocus(bool focused) { if (!focused) End(false); }
        private void OnApplicationPause(bool paused) { if (paused) End(false); }
        private void OnDisable() { End(false); }
        private void OnDestroy() { End(false); }

        private void Acquire()
        {
            if (_owning && _ownerEpoch != _epoch) _owning = false;
            if (!_owning)
            {
                bool first = _owned == 0;
                _owning = true; _ownerEpoch = _epoch; _owned++;
                if (first) AcquireProcessOwnership();
            }
            Reassert();
        }

        private void Reassert()
        {
            if (!_owning || _ownerEpoch != _epoch) return;
            try { GameData.DraggingUIElement = true; } catch { }
        }

        private void End(bool notify)
        {
            bool completed = _dragging; _dragging = false; _parent = null;
            if (_owning)
            {
                bool current = _ownerEpoch == _epoch; _owning = false;
                if (current) { _owned--; if (_owned < 0) _owned = 0; if (_owned == 0) ReleaseProcessOwnership(); }
            }
            if (notify && completed) { try { if (Completed != null) Completed(); } catch { } }
        }

        internal static void ForceReleaseIfOwned()
        {
            bool had = _owned > 0; _owned = 0; _epoch++; if (_epoch < 0) _epoch = 1;
            if (had || ProcessContainsOwner()) ReleaseProcessOwnership();
        }

        private static void AcquireProcessOwnership()
        {
            HashSet<string> owners = GetProcessOwners(true); if (owners == null) return;
            lock (owners)
            {
                if (owners.Count == 0)
                {
                    bool baseline = false; try { baseline = GameData.DraggingUIElement; } catch { }
                    AppDomain.CurrentDomain.SetData(ProcessBaselineKey, baseline);
                    AppDomain.CurrentDomain.SetData(ProcessBaselineCapturedKey, true);
                }
                owners.Add(ProcessOwner);
            }
            try { GameData.DraggingUIElement = true; } catch { }
        }

        private static void ReleaseProcessOwnership()
        {
            HashSet<string> owners = GetProcessOwners(false); if (owners == null) { RestoreBaseline(); return; }
            bool last; lock (owners) { owners.Remove(ProcessOwner); last = owners.Count == 0; }
            if (last) RestoreBaseline(); else { try { GameData.DraggingUIElement = true; } catch { } }
        }

        private static bool ProcessContainsOwner()
        {
            HashSet<string> owners = GetProcessOwners(false); if (owners == null) return false;
            lock (owners) { return owners.Contains(ProcessOwner); }
        }

        private static HashSet<string> GetProcessOwners(bool create)
        {
            try
            {
                HashSet<string> owners = AppDomain.CurrentDomain.GetData(ProcessOwnersKey) as HashSet<string>;
                if (owners == null && create) { owners = new HashSet<string>(StringComparer.Ordinal); AppDomain.CurrentDomain.SetData(ProcessOwnersKey, owners); }
                return owners;
            }
            catch { return null; }
        }

        private static void RestoreBaseline()
        {
            try
            {
                object capturedValue = AppDomain.CurrentDomain.GetData(ProcessBaselineCapturedKey);
                bool captured = capturedValue is bool && (bool)capturedValue;
                object baselineValue = AppDomain.CurrentDomain.GetData(ProcessBaselineKey);
                bool baseline = baselineValue is bool && (bool)baselineValue;
                if (captured) GameData.DraggingUIElement = baseline;
                AppDomain.CurrentDomain.SetData(ProcessBaselineCapturedKey, false);
                AppDomain.CurrentDomain.SetData(ProcessBaselineKey, false);
            }
            catch { }
        }
    }
}
