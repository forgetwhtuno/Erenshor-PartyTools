using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ErenshorPartyTools
{
    internal sealed class PartyToolsDragGuard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerUpHandler
    {
        private static int _owned;
        internal RectTransform Target;
        internal Action Completed;
        private RectTransform _parent;
        private Vector2 _startPointer, _startPosition;
        private bool _dragging, _owning;

        public void OnBeginDrag(PointerEventData e)
        {
            if (Target == null) Target = GetComponent<RectTransform>();
            _parent = Target == null ? null : Target.parent as RectTransform;
            if (_parent == null) return;
            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_parent, e.position, e.pressEventCamera, out local)) return;
            _startPointer = local; _startPosition = Target.anchoredPosition; _dragging = true;
            if (!_owning) { _owning = true; _owned++; }
            GameData.DraggingUIElement = true;
        }

        public void OnDrag(PointerEventData e)
        {
            if (!_dragging || _parent == null || Target == null) return;
            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_parent, e.position, e.pressEventCamera, out local)) return;
            Target.anchoredPosition = _startPosition + (local - _startPointer);
        }

        public void OnEndDrag(PointerEventData e) { End(true); }
        public void OnPointerUp(PointerEventData e) { End(false); }
        private void OnDisable() { End(true); }
        private void OnDestroy() { End(true); }

        private void End(bool notify)
        {
            bool completed = _dragging; _dragging = false;
            if (_owning) { _owning = false; _owned--; if (_owned < 0) _owned = 0; if (_owned == 0) { try { GameData.DraggingUIElement = false; } catch { } } }
            if (notify && completed) { try { if (Completed != null) Completed(); } catch { } }
        }

        internal static void ForceReleaseIfOwned()
        {
            if (_owned > 0) { _owned = 0; try { GameData.DraggingUIElement = false; } catch { } }
            else _owned = 0;
        }
    }
}
