using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ErenshorPartyTools
{
    internal static class PartyToolsPanel
    {
        private enum PanelMode
        {
            None,
            CommandMenu,
            ReadyCheck,
            PartyRoll,
            FriendAvailability
        }

        private static readonly List<PanelRow> Rows = new List<PanelRow>();
        private static PanelMode _mode;
        private static string _sceneName;
        private static float _expiresAt;
        private static float _nextReadyRefresh;
        private static int _rollSides;
        private static string _title;
        private static Action<PartyToolsAction> _actionHandler;

        private static Texture2D _backgroundTexture;
        private static Texture2D _borderTexture;
        private static GUIStyle _titleStyle;
        private static GUIStyle _nameStyle;
        private static GUIStyle _valueStyle;
        private static GUIStyle _blockedStyle;
        private static GUIStyle _footerStyle;
        private static GUIStyle _buttonStyle;

        private static PanelPositionState _positionState;
        private static bool _dragging;
        private static Vector2 _dragOffset;
        private static Rect _panelRect;

        private const float VisibleSeconds = 9f;
        private const float ReadyRefreshSeconds = 0.25f;
        private const float Width = 310f;
        private const float HeaderHeight = 31f;
        private const float RowHeight = 22f;
        private const float FooterHeight = 25f;
        private const float HorizontalPadding = 12f;
        private const int DragControlHint = 0x45F0118;

        internal static bool PointerIsOverPanel(Vector2 screenPoint)
        {
            return _dragging || (_mode != PanelMode.None && _panelRect.width > 0f && _panelRect.Contains(screenPoint));
        }

        internal static void ConfigurePosition(float offsetX, float offsetY, Action<float, float> persist)
        {
            _positionState = new PanelPositionState(offsetX, offsetY, persist);
        }

        internal static void ShowReadyCheck()
        {
            RefreshReadyRows();

            _actionHandler = null;
            _sceneName = CurrentSceneName();
            _expiresAt = Time.unscaledTime + VisibleSeconds;
            _nextReadyRefresh = Time.unscaledTime + ReadyRefreshSeconds;
            _title = "READY CHECK";
            _mode = PanelMode.ReadyCheck;
        }

        internal static bool IsCommandMenuOpen
        {
            get { return _mode == PanelMode.CommandMenu; }
        }

        internal static void ShowCommandMenu(Action<PartyToolsAction> actionHandler)
        {
            Rows.Clear();
            _actionHandler = actionHandler;
            _sceneName = CurrentSceneName();
            _expiresAt = Time.unscaledTime + 30f;
            _title = "PARTY TOOLS";
            _mode = PanelMode.CommandMenu;
        }

        internal static void ShowPartyRoll(int sides, List<PanelRow> rows)
        {
            Rows.Clear();
            if (rows != null) Rows.AddRange(rows);

            _actionHandler = null;
            _sceneName = CurrentSceneName();
            _expiresAt = Time.unscaledTime + VisibleSeconds;
            _rollSides = sides;
            _title = "PARTY ROLL (1-" + _rollSides.ToString() + ")";
            _mode = PanelMode.PartyRoll;
        }

        internal static void ShowFriendAvailability(List<PanelRow> rows)
        {
            Rows.Clear();
            if (rows != null) Rows.AddRange(rows);
            _actionHandler = null;
            _sceneName = CurrentSceneName();
            _expiresAt = Time.unscaledTime + VisibleSeconds;
            _title = "FRIENDS ONLINE";
            _mode = PanelMode.FriendAvailability;
        }

        internal static void Tick()
        {
            if (_mode == PanelMode.None) return;
            if (_mode == PanelMode.CommandMenu && Input.GetKeyDown(KeyCode.Escape))
            {
                Close();
                return;
            }
            if (Time.unscaledTime >= _expiresAt)
            {
                Close();
                return;
            }

            string currentScene = CurrentSceneName();
            if (!string.Equals(currentScene, _sceneName, StringComparison.Ordinal))
            {
                Close();
                return;
            }

            if (_mode == PanelMode.ReadyCheck && Time.unscaledTime >= _nextReadyRefresh)
            {
                RefreshReadyRows();
                _nextReadyRefresh = Time.unscaledTime + ReadyRefreshSeconds;
            }
        }

        internal static void Draw()
        {
            if (_mode == PanelMode.None) return;
            EnsureStyles();

            float contentHeight = _mode == PanelMode.CommandMenu
                ? (4f * 30f) + 8f
                : Rows.Count * RowHeight;
            float height = HeaderHeight + contentHeight + FooterHeight + 12f;
            EnsurePositionState();
            PanelPosition position = _positionState.ResolveAndRecover(Screen.width, Screen.height, Width, height);
            Rect panel = new Rect(position.X, position.Y, Width, height);

            HandleDrag(ref panel);
            _panelRect = panel;

            int previousDepth = GUI.depth;
            try
            {
                GUI.depth = -45;
                GUI.DrawTexture(panel, _backgroundTexture);
                DrawBorder(panel);

                GUI.Label(new Rect(panel.x + HorizontalPadding, panel.y + 7f, panel.width - (HorizontalPadding * 2f), 22f), _title, _titleStyle);

                float y = panel.y + HeaderHeight;
                if (_mode == PanelMode.CommandMenu)
                {
                    DrawActionButton(panel, ref y, "READY CHECK", PartyToolsAction.ReadyCheck);
                    DrawActionButton(panel, ref y, "ROLL 1-100", PartyToolsAction.Roll);
                    DrawActionButton(panel, ref y, "PARTY ROLL 1-100", PartyToolsAction.PartyRoll);
                    DrawActionButton(panel, ref y, "FRIENDS ONLINE", PartyToolsAction.FriendAvailability);
                }
                else
                {
                    for (int i = 0; i < Rows.Count; i++)
                    {
                        PanelRow row = Rows[i];
                        if (row == null) continue;
                        GUI.Label(new Rect(panel.x + HorizontalPadding, y, 170f, RowHeight), row.Name, _nameStyle);
                        GUI.Label(new Rect(panel.x + 185f, y, panel.width - 197f, RowHeight), row.Value,
                            row.Blocked ? _blockedStyle : _valueStyle);
                        y += RowHeight;
                    }
                }

                string footer = _mode == PanelMode.CommandMenu
                    ? "Escape closes - /tools opens this menu"
                    : _mode == PanelMode.ReadyCheck
                    ? "Verified local party state only"
                    : _mode == PanelMode.PartyRoll
                        ? "Local social roll; no loot authority"
                        : "Advisory only; no auto-invites";
                GUI.Label(new Rect(panel.x + HorizontalPadding, panel.y + panel.height - FooterHeight, panel.width - (HorizontalPadding * 2f), 18f), footer, _footerStyle);
            }
            finally
            {
                GUI.depth = previousDepth;
            }
        }

        internal static void Close()
        {
            if (_positionState != null) _positionState.CommitIfMoved();
            _mode = PanelMode.None;
            _sceneName = null;
            _expiresAt = 0f;
            _nextReadyRefresh = 0f;
            _rollSides = 0;
            _title = null;
            _actionHandler = null;
            Rows.Clear();
            _dragging = false;
            _panelRect = new Rect();
        }

        internal static void Dispose()
        {
            Close();
            if (_backgroundTexture != null)
            {
                UnityEngine.Object.Destroy(_backgroundTexture);
                _backgroundTexture = null;
            }
            if (_borderTexture != null)
            {
                UnityEngine.Object.Destroy(_borderTexture);
                _borderTexture = null;
            }
            _titleStyle = null;
            _nameStyle = null;
            _valueStyle = null;
            _blockedStyle = null;
            _footerStyle = null;
            _buttonStyle = null;
            _positionState = null;
        }

        private static void EnsurePositionState()
        {
            if (_positionState == null) _positionState = new PanelPositionState(0f, 0f, null);
        }

        private static void RefreshReadyRows()
        {
            List<ReadyRow> readyRows = PartyStateReader.BuildReadyRows();
            Rows.Clear();
            for (int i = 0; i < readyRows.Count; i++)
            {
                ReadyRow row = readyRows[i];
                if (row == null) continue;
                ReadyState state = row.State;
                Rows.Add(new PanelRow(row.Name, ReadyStateText(state), state != ReadyState.Ready));
            }
        }

        private static string ReadyStateText(ReadyState state)
        {
            switch (state)
            {
                case ReadyState.Dead: return "DEAD";
                case ReadyState.InCombat: return "IN COMBAT";
                case ReadyState.Unavailable: return "UNAVAILABLE";
                default: return "READY";
            }
        }

        private static string CurrentSceneName()
        {
            try { return SceneManager.GetActiveScene().name; }
            catch { return string.Empty; }
        }

        private static void EnsureStyles()
        {
            if (_titleStyle != null && _backgroundTexture != null && _borderTexture != null) return;

            if (_backgroundTexture == null)
                _backgroundTexture = MakeTexture(new Color(0.035f, 0.055f, 0.065f, 0.82f));
            if (_borderTexture == null)
                _borderTexture = MakeTexture(new Color(0.48f, 0.76f, 0.78f, 0.90f));

            _titleStyle = new GUIStyle(GUI.skin.label);
            _titleStyle.fontSize = 14;
            _titleStyle.fontStyle = FontStyle.Bold;
            _titleStyle.clipping = TextClipping.Clip;
            _titleStyle.normal.textColor = new Color(0.82f, 0.96f, 0.97f, 1f);

            _nameStyle = new GUIStyle(GUI.skin.label);
            _nameStyle.fontSize = 12;
            _nameStyle.clipping = TextClipping.Clip;
            _nameStyle.normal.textColor = new Color(0.88f, 0.92f, 0.91f, 1f);

            _valueStyle = new GUIStyle(GUI.skin.label);
            _valueStyle.fontSize = 12;
            _valueStyle.fontStyle = FontStyle.Bold;
            _valueStyle.alignment = TextAnchor.UpperRight;
            _valueStyle.clipping = TextClipping.Clip;
            _valueStyle.normal.textColor = new Color(0.68f, 0.94f, 0.86f, 1f);

            _blockedStyle = new GUIStyle(_valueStyle);
            _blockedStyle.normal.textColor = new Color(0.95f, 0.82f, 0.56f, 1f);

            _footerStyle = new GUIStyle(GUI.skin.label);
            _footerStyle.fontSize = 10;
            _footerStyle.alignment = TextAnchor.LowerLeft;
            _footerStyle.clipping = TextClipping.Clip;
            _footerStyle.normal.textColor = new Color(0.66f, 0.76f, 0.76f, 0.95f);

            _buttonStyle = new GUIStyle(GUI.skin.button);
            _buttonStyle.fontSize = 12;
            _buttonStyle.fontStyle = FontStyle.Bold;
            _buttonStyle.alignment = TextAnchor.MiddleLeft;
            _buttonStyle.padding = new RectOffset(10, 8, 0, 0);
            _buttonStyle.normal.textColor = new Color(0.82f, 0.96f, 0.97f, 1f);
        }

        private static void DrawActionButton(Rect panel, ref float y, string label, PartyToolsAction action)
        {
            Rect button = new Rect(panel.x + HorizontalPadding, y, panel.width - (HorizontalPadding * 2f), 25f);
            y += 30f;
            if (!GUI.Button(button, label, _buttonStyle)) return;

            Action<PartyToolsAction> handler = _actionHandler;
            Close();
            if (handler != null) handler(action);
        }

        private static Texture2D MakeTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.hideFlags = HideFlags.HideAndDontSave;
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private static void DrawBorder(Rect rect)
        {
            const float thickness = 1f;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), _borderTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), _borderTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), _borderTexture);
            GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), _borderTexture);
        }

        private static void HandleDrag(ref Rect panel)
        {
            Event current = Event.current;
            if (current == null) return;
            int controlId = GUIUtility.GetControlID(DragControlHint, FocusType.Passive);
            Rect titleBar = new Rect(panel.x, panel.y, panel.width, HeaderHeight);
            if (current.type == EventType.MouseDown && current.button == 0 && titleBar.Contains(current.mousePosition))
            {
                GUIUtility.hotControl = controlId;
                _dragging = true;
                _dragOffset = current.mousePosition - new Vector2(panel.x, panel.y);
                current.Use();
                return;
            }
            if (GUIUtility.hotControl != controlId) return;
            if (current.type == EventType.MouseDrag && _dragging)
            {
                float desiredX = current.mousePosition.x - _dragOffset.x;
                float desiredY = current.mousePosition.y - _dragOffset.y;
                PanelPosition position = _positionState.MoveTo(
                    Screen.width, Screen.height, Width, panel.height, desiredX, desiredY);
                panel.x = position.X;
                panel.y = position.Y;
                current.Use();
                return;
            }
            if (current.type == EventType.MouseUp && current.button == 0 && _dragging)
            {
                GUIUtility.hotControl = 0;
                _dragging = false;
                _positionState.CommitIfMoved();
                current.Use();
            }
        }

    }
}
