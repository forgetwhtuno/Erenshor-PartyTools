using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ErenshorPartyTools
{
    internal static class PartyToolsPanel
    {
        private enum PanelMode { Overview, ReadyCheck, LocalRoll, PartyRoll, FriendAvailability }
        private const int SortingOrder = 521;
        private const float ReadyRefreshSeconds = 0.25f;
        private const float Width = PartyToolsUiGeometry.PanelWidth;
        private const float Height = PartyToolsUiGeometry.PanelHeight;

        private static readonly List<PanelRow> Rows = new List<PanelRow>();
        private static readonly List<RowView> RowViews = new List<RowView>();
        private static PanelMode _mode = PanelMode.Overview;
        private static string _sceneName;
        private static float _nextReadyRefresh;
        private static int _rollSides;
        private static bool _open;
        private static bool _built;
        private static bool _launcherVisible;
        private static GameObject _root, _panelObject, _launcherObject;
        private static RectTransform _panel, _launcher, _rowContent;
        private static RectTransform _header, _headerGrip, _closeRect, _statusRect, _actionsRect, _resultHeader, _viewport, _footerRect;
        private static TextMeshProUGUI _title, _status, _resultTitle, _footer, _launcherText;
        private static float _panelX = PartyToolsUiGeometry.Unset, _panelY = PartyToolsUiGeometry.Unset;
        private static float _launcherX = PartyToolsUiGeometry.Unset, _launcherY = PartyToolsUiGeometry.Unset;
        private static Action<float, float> _persistPanel, _persistLauncher;
        private static float _screenW, _screenH;

        private sealed class RowView
        {
            internal GameObject Root;
            internal TextMeshProUGUI Name;
            internal TextMeshProUGUI Value;
        }

        internal static bool IsOpen { get { return _open; } }
        internal static bool IsCommandMenuOpen { get { return _open && _mode == PanelMode.Overview; } }

        internal static void ConfigurePosition(float panelX, float panelY, float launcherX, float launcherY,
            Action<float, float> persistPanel, Action<float, float> persistLauncher)
        {
            _panelX = PartyToolsUiGeometry.Interpret(panelX); _panelY = PartyToolsUiGeometry.Interpret(panelY);
            _launcherX = PartyToolsUiGeometry.Interpret(launcherX); _launcherY = PartyToolsUiGeometry.Interpret(launcherY);
            _persistPanel = persistPanel; _persistLauncher = persistLauncher;
        }

        internal static void ShowCommandMenu(Action<PartyToolsAction> ignoredCompatibilityHandler)
        {
            Rows.Clear(); _mode = PanelMode.Overview; _sceneName = CurrentSceneName(); _open = true;
            SetResultRows();
        }

        internal static void ShowReadyCheck()
        {
            RefreshReadyRows(); _mode = PanelMode.ReadyCheck; _sceneName = CurrentSceneName(); _nextReadyRefresh = Time.unscaledTime + ReadyRefreshSeconds; _open = true;
            SetResultRows();
        }

        internal static void ShowLocalRoll(string playerName, int sides, int value)
        {
            Rows.Clear(); Rows.Add(new PanelRow(playerName, value.ToString() + " (1-" + sides.ToString() + ")", false));
            _rollSides = sides; _mode = PanelMode.LocalRoll; _sceneName = CurrentSceneName(); _open = true; SetResultRows();
        }

        internal static void ShowPartyRoll(int sides, List<PanelRow> rows)
        {
            Rows.Clear(); if (rows != null) Rows.AddRange(rows); _rollSides = sides; _mode = PanelMode.PartyRoll; _sceneName = CurrentSceneName(); _open = true; SetResultRows();
        }

        internal static void ShowFriendAvailability(List<PanelRow> rows)
        {
            Rows.Clear(); if (rows != null) Rows.AddRange(rows); _mode = PanelMode.FriendAvailability; _sceneName = CurrentSceneName(); _open = true; SetResultRows();
        }

        internal static void Tick(bool launcherVisible)
        {
            _launcherVisible = launcherVisible;
            if (!SuiteUiPolicy.IsGameplayReady()) { Close(); HideAll(); PartyToolsDragGuard.ForceReleaseIfOwned(); return; }
            if (EventSystem.current == null) { HideAll(); PartyToolsDragGuard.ForceReleaseIfOwned(); return; }
            if (!EnsureBuilt()) return;
            if (_screenW != Screen.width || _screenH != Screen.height) { _screenW = Screen.width; _screenH = Screen.height; ApplyPositions(); }

            if (PartyToolsPanelPolicy.ShouldClose(_open, true, _sceneName, CurrentSceneName())) Close();
            if (_open && _mode == PanelMode.ReadyCheck && Time.unscaledTime >= _nextReadyRefresh)
            {
                RefreshReadyRows(); _nextReadyRefresh = Time.unscaledTime + ReadyRefreshSeconds; SetResultRows();
            }
            _panelObject.SetActive(_open);
            _launcherObject.SetActive(launcherVisible);
            UpdateLabels();
        }

        internal static void ResetPosition()
        {
            _panelX = _panelY = PartyToolsUiGeometry.Unset;
            if (_panel != null) ApplyPanelPosition();
            PersistPanelPosition();
        }

        internal static void ResetLauncherPosition()
        {
            _launcherX = _launcherY = PartyToolsUiGeometry.Unset;
            if (_launcher != null) ApplyLauncherPosition();
            PersistLauncherPosition();
        }

        internal static void Close()
        {
            _open = false; _sceneName = null; _nextReadyRefresh = 0f;
            if (_panelObject != null) _panelObject.SetActive(false);
            PartyToolsDragGuard.ForceReleaseIfOwned();
        }

        internal static void ReleaseDrag() { PartyToolsDragGuard.ForceReleaseIfOwned(); }

        internal static void Dispose()
        {
            PartyToolsDragGuard.ForceReleaseIfOwned(); RowViews.Clear(); Rows.Clear();
            if (_root != null) { try { UnityEngine.Object.DestroyImmediate(_root); } catch { } }
            _root = _panelObject = _launcherObject = null; _panel = _launcher = _rowContent = null;
            _header = _headerGrip = _closeRect = _statusRect = _actionsRect = _resultHeader = _viewport = _footerRect = null;
            _title = _status = _resultTitle = _footer = _launcherText = null; _built = false; _open = false;
        }

        internal static string RunSelfTests()
        {
            string geometry = PartyToolsUiGeometry.RunSelfTests(); string launcher = SuiteLauncherPolicy.RunSelfTests();
            return geometry.StartsWith("PASS", StringComparison.Ordinal) && launcher.StartsWith("PASS", StringComparison.Ordinal)
                ? "PASS partytools retained ui" : "FAIL " + geometry + "; " + launcher;
        }

        private static bool EnsureBuilt()
        {
            if (_built) return true;
            try
            {
                _root = new GameObject("ErenshorPartyTools.RetainedUI"); UnityEngine.Object.DontDestroyOnLoad(_root);
                Canvas canvas = _root.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.overrideSorting = true; canvas.sortingOrder = SortingOrder;
                CanvasScaler scaler = _root.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize; scaler.scaleFactor = 1f;
                _root.AddComponent<GraphicRaycaster>();
                BuildLauncher(); BuildPanel(); _screenW = Screen.width; _screenH = Screen.height; ApplyPositions(); _built = true; SetResultRows(); UpdateLabels(); return true;
            }
            catch
            {
                if (_root != null) { try { UnityEngine.Object.DestroyImmediate(_root); } catch { } }
                _root = null; _built = false; return false;
            }
        }

        private static void BuildLauncher()
        {
            _launcherObject = MakePanel("Party Tools Launcher", _root.transform, new Color32(23, 27, 34, 238));
            _launcher = _launcherObject.GetComponent<RectTransform>(); BaseRect(_launcher, PartyToolsUiGeometry.LauncherWidth, PartyToolsUiGeometry.LauncherHeight);
            RectTransform grip = MakeRect("Grip", _launcher, 18f, PartyToolsUiGeometry.LauncherHeight, 0f, 0f); AddImage(grip, new Color32(61, 71, 84, 255));
            PartyToolsDragGuard drag = grip.gameObject.AddComponent<PartyToolsDragGuard>(); drag.Target = _launcher; drag.Completed = PersistLauncherPosition;
            AddText(grip, "⋮", 18, TextAlignmentOptions.Center, Color.white);
            RectTransform button = MakeRect("Open", _launcher, PartyToolsUiGeometry.LauncherWidth - 18f, PartyToolsUiGeometry.LauncherHeight, 18f, 0f);
            Button b = AddButton(button, "Party Tools", delegate { PartyToolsControlApi.TogglePanel(); }); _launcherText = b.GetComponentInChildren<TextMeshProUGUI>();
        }

        private static void BuildPanel()
        {
            _panelObject = MakePanel("Party Tools Panel", _root.transform, new Color32(18, 22, 29, 246));
            _panel = _panelObject.GetComponent<RectTransform>(); BaseRect(_panel, Width, Height);
            _header = MakeRect("Header", _panel, Width, 34f, 0f, Height - 34f); AddImage(_header, new Color32(39, 47, 58, 255));
            _headerGrip = MakeRect("Header Drag Surface", _header, Width - 46f, 34f, 0f, 0f);
            AddImage(_headerGrip, new Color(0f, 0f, 0f, 0f));
            _title = AddText(_headerGrip, "PARTY TOOLS", 17, TextAlignmentOptions.MidlineLeft, Color.white); SetOffsets(_title.rectTransform, 10f, 0f, 0f, 0f);
            PartyToolsDragGuard drag = _headerGrip.gameObject.AddComponent<PartyToolsDragGuard>(); drag.Target = _panel; drag.Completed = PersistPanelPosition;
            _closeRect = MakeRect("Close", _header, 36f, 28f, Width - 40f, 3f); AddButton(_closeRect, "X", delegate { PartyToolsControlApi.ClosePanel(); });

            _statusRect = MakeRect("Status", _panel, Width - 20f, 36f, 10f, Height - 76f);
            _status = AddText(_statusRect, string.Empty, 13, TextAlignmentOptions.MidlineLeft, new Color32(195, 205, 216, 255));

            _actionsRect = MakeRect("Actions", _panel, Width - 20f, 76f, 10f, Height - 154f);
            AddButton(Cell(_actionsRect,0,2,0,2), "Ready Check", delegate { PartyToolsControlApi.ReadyCheck(); });
            AddButton(Cell(_actionsRect,1,2,0,2), "Roll 1-100", delegate { PartyToolsControlApi.Roll(100); });
            AddButton(Cell(_actionsRect,0,2,1,2), "Party Roll 1-100", delegate { PartyToolsControlApi.PartyRoll(100); });
            AddButton(Cell(_actionsRect,1,2,1,2), "Friends Online", delegate { PartyToolsControlApi.ShowFriendAvailability(); });

            _resultHeader = MakeRect("Result Header", _panel, Width - 20f, 24f, 10f, Height - 184f);
            _resultTitle = AddText(_resultHeader, "READY", 13, TextAlignmentOptions.MidlineLeft, new Color32(132, 176, 220, 255));

            _viewport = MakeRect("Rows Viewport", _panel, Width - 20f, 190f, 10f, 66f); AddImage(_viewport, new Color32(12, 15, 20, 150)); _viewport.gameObject.AddComponent<RectMask2D>();
            ScrollRect scroll = _viewport.gameObject.AddComponent<ScrollRect>(); scroll.horizontal = false; scroll.vertical = true; scroll.scrollSensitivity = 22f;
            _rowContent = MakeRect("Rows", _viewport, 0f, 1f, 0f, 0f); _rowContent.anchorMin = new Vector2(0f,1f); _rowContent.anchorMax = new Vector2(1f,1f); _rowContent.pivot = new Vector2(.5f,1f); _rowContent.anchoredPosition = Vector2.zero; _rowContent.sizeDelta = new Vector2(0f,1f);
            VerticalLayoutGroup layout = _rowContent.gameObject.AddComponent<VerticalLayoutGroup>(); layout.spacing = 3f; layout.padding = new RectOffset(5,5,5,5); layout.childControlHeight = true; layout.childControlWidth = true; layout.childForceExpandHeight = false; layout.childForceExpandWidth = true;
            ContentSizeFitter fitter = _rowContent.gameObject.AddComponent<ContentSizeFitter>(); fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = _viewport; scroll.content = _rowContent;

            _footerRect = MakeRect("Footer", _panel, Width - 20f, 42f, 10f, 12f);
            _footer = AddText(_footerRect, "Deterministic local-party utilities. Commands remain available for compatibility.", 11, TextAlignmentOptions.TopLeft, new Color32(155, 165, 178, 255));
        }

        private static void UpdateLabels()
        {
            if (!_built) return;
            if (_launcherText != null) _launcherText.text = "Party Tools";
            if (_status != null) _status.text = PartyToolsControlApi.GetStatus();
            if (_title != null) _title.text = "PARTY TOOLS" + (PartyToolsControlApi.GetBasicState().RaidActive ? "  •  RAID LIMITS" : string.Empty);
            if (_resultTitle != null)
            {
                if (_mode == PanelMode.ReadyCheck) _resultTitle.text = "READY CHECK — VERIFIED LOCAL PARTY";
                else if (_mode == PanelMode.LocalRoll) _resultTitle.text = "ROLL 1-" + _rollSides;
                else if (_mode == PanelMode.PartyRoll) _resultTitle.text = "PARTY ROLL 1-" + _rollSides;
                else if (_mode == PanelMode.FriendAvailability) _resultTitle.text = "FRIENDS ONLINE";
                else _resultTitle.text = "TOOLS";
            }
        }

        private static void RefreshReadyRows()
        {
            List<ReadyRow> ready = PartyStateReader.BuildReadyRows(); Rows.Clear();
            for (int i=0;i<ready.Count;i++) { ReadyRow r=ready[i]; if (r!=null) Rows.Add(new PanelRow(r.Name, ReadyCheckPresentation.Text(r.State), r.State!=ReadyState.Ready)); }
        }

        private static void SetResultRows()
        {
            if (!_built || _rowContent == null) return;
            while (RowViews.Count < Rows.Count) RowViews.Add(CreateRow());
            while (RowViews.Count > Rows.Count)
            {
                RowView last=RowViews[RowViews.Count-1]; RowViews.RemoveAt(RowViews.Count-1); if(last.Root!=null) UnityEngine.Object.DestroyImmediate(last.Root);
            }
            for (int i=0;i<RowViews.Count;i++)
            {
                PanelRow row=Rows[i]; RowView view=RowViews[i]; view.Name.text=row.Name??string.Empty; view.Value.text=row.Value??string.Empty;
                view.Value.color=row.Blocked?new Color32(226,157,110,255):new Color32(188,221,188,255);
            }
            Canvas.ForceUpdateCanvases(); LayoutRebuilder.ForceRebuildLayoutImmediate(_rowContent); UpdateLabels();
        }

        private static RowView CreateRow()
        {
            GameObject go=new GameObject("Row",typeof(RectTransform),typeof(LayoutElement)); RectTransform rt=go.GetComponent<RectTransform>(); rt.SetParent(_rowContent,false); go.GetComponent<LayoutElement>().preferredHeight=25f;
            TextMeshProUGUI name=AddText(Cell(rt,0,2,0,1),string.Empty,13,TextAlignmentOptions.MidlineLeft,new Color32(220,224,230,255));
            TextMeshProUGUI value=AddText(Cell(rt,1,2,0,1),string.Empty,13,TextAlignmentOptions.MidlineRight,new Color32(188,221,188,255));
            return new RowView{Root=go,Name=name,Value=value};
        }

        private static void ApplyPositions() { ApplyPanelPosition(); ApplyLauncherPosition(); }
        private static void ApplyPanelPosition() { if(_panel==null)return; PartyToolsUiRect r=PartyToolsUiGeometry.ResolvePanel(_panelX,_panelY,Screen.width,Screen.height); ResizePanel(r.Width,r.Height); _panel.anchoredPosition=new Vector2(r.X,r.Y); PartyToolsUiGeometry.Normalize(r,Screen.width,Screen.height,out _panelX,out _panelY); }
        private static void ApplyLauncherPosition() { if(_launcher==null)return; PartyToolsUiRect r=PartyToolsUiGeometry.ResolveLauncher(_launcherX,_launcherY,Screen.width,Screen.height); _launcher.anchoredPosition=new Vector2(r.X,r.Y); PartyToolsUiGeometry.Normalize(r,Screen.width,Screen.height,out _launcherX,out _launcherY); }
        private static void ResizePanel(float width,float height)
        {
            if(_panel==null)return; _panel.sizeDelta=new Vector2(width,height);
            if(_header!=null){_header.sizeDelta=new Vector2(width,34f);_header.anchoredPosition=new Vector2(0f,height-34f);}
            if(_headerGrip!=null)_headerGrip.sizeDelta=new Vector2(Math.Max(80f,width-46f),34f);
            if(_closeRect!=null)_closeRect.anchoredPosition=new Vector2(Math.Max(4f,width-40f),3f);
            if(_statusRect!=null){_statusRect.sizeDelta=new Vector2(Math.Max(80f,width-20f),36f);_statusRect.anchoredPosition=new Vector2(10f,height-76f);}
            if(_actionsRect!=null){_actionsRect.sizeDelta=new Vector2(Math.Max(80f,width-20f),76f);_actionsRect.anchoredPosition=new Vector2(10f,height-154f);}
            if(_resultHeader!=null){_resultHeader.sizeDelta=new Vector2(Math.Max(80f,width-20f),24f);_resultHeader.anchoredPosition=new Vector2(10f,height-184f);}
            float viewH=Math.Max(80f,height-265f);
            if(_viewport!=null){_viewport.sizeDelta=new Vector2(Math.Max(80f,width-20f),viewH);_viewport.anchoredPosition=new Vector2(10f,66f);}
            if(_rowContent!=null)_rowContent.sizeDelta=new Vector2(0f,_rowContent.sizeDelta.y);
            if(_footerRect!=null){_footerRect.sizeDelta=new Vector2(Math.Max(80f,width-20f),42f);_footerRect.anchoredPosition=new Vector2(10f,12f);}
        }
        private static void PersistPanelPosition() { if(_panel==null)return; PartyToolsUiRect r=PartyToolsUiGeometry.Clamp(new PartyToolsUiRect(_panel.anchoredPosition.x,_panel.anchoredPosition.y,_panel.sizeDelta.x,_panel.sizeDelta.y),Screen.width,Screen.height); _panel.anchoredPosition=new Vector2(r.X,r.Y); PartyToolsUiGeometry.Normalize(r,Screen.width,Screen.height,out _panelX,out _panelY); if(_persistPanel!=null)_persistPanel(_panelX,_panelY); }
        private static void PersistLauncherPosition() { if(_launcher==null)return; PartyToolsUiRect r=PartyToolsUiGeometry.Clamp(new PartyToolsUiRect(_launcher.anchoredPosition.x,_launcher.anchoredPosition.y,PartyToolsUiGeometry.LauncherWidth,PartyToolsUiGeometry.LauncherHeight),Screen.width,Screen.height); _launcher.anchoredPosition=new Vector2(r.X,r.Y); PartyToolsUiGeometry.Normalize(r,Screen.width,Screen.height,out _launcherX,out _launcherY); if(_persistLauncher!=null)_persistLauncher(_launcherX,_launcherY); }
        private static void HideAll(){if(_panelObject!=null)_panelObject.SetActive(false);if(_launcherObject!=null)_launcherObject.SetActive(false);}
        private static string CurrentSceneName(){try{return SceneManager.GetActiveScene().name??string.Empty;}catch{return string.Empty;}}

        private static GameObject MakePanel(string name,Transform parent,Color color){GameObject go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(CanvasGroup));go.transform.SetParent(parent,false);go.GetComponent<Image>().color=color;CanvasGroup g=go.GetComponent<CanvasGroup>();g.interactable=true;g.blocksRaycasts=true;return go;}
        private static void BaseRect(RectTransform rt,float w,float h){rt.anchorMin=rt.anchorMax=rt.pivot=Vector2.zero;rt.sizeDelta=new Vector2(w,h);}
        private static RectTransform MakeRect(string name,Transform parent,float w,float h,float x,float y){GameObject go=new GameObject(name,typeof(RectTransform));RectTransform rt=go.GetComponent<RectTransform>();rt.SetParent(parent,false);BaseRect(rt,w,h);rt.anchoredPosition=new Vector2(x,y);return rt;}
        private static void AddImage(RectTransform rt,Color color){Image i=rt.gameObject.AddComponent<Image>();i.color=color;}
        private static Button AddButton(RectTransform rt,string label,UnityEngine.Events.UnityAction action){Image i=rt.gameObject.GetComponent<Image>();if(i==null)i=rt.gameObject.AddComponent<Image>();i.color=new Color32(53,66,82,255);Button b=rt.gameObject.GetComponent<Button>();if(b==null)b=rt.gameObject.AddComponent<Button>();b.targetGraphic=i;b.onClick.AddListener(action);AddText(rt,label,13,TextAlignmentOptions.Center,Color.white);return b;}
        private static TextMeshProUGUI AddText(RectTransform parent,string text,int size,TextAlignmentOptions align,Color color){GameObject go=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI));RectTransform rt=go.GetComponent<RectTransform>();rt.SetParent(parent,false);rt.anchorMin=Vector2.zero;rt.anchorMax=Vector2.one;rt.offsetMin=Vector2.zero;rt.offsetMax=Vector2.zero;TextMeshProUGUI t=go.GetComponent<TextMeshProUGUI>();t.text=text;t.fontSize=size;t.alignment=align;t.color=color;t.raycastTarget=false;t.enableWordWrapping=true;return t;}
        private static void SetOffsets(RectTransform rt,float l,float b,float r,float t){rt.anchorMin=Vector2.zero;rt.anchorMax=Vector2.one;rt.offsetMin=new Vector2(l,b);rt.offsetMax=new Vector2(r,t);}
        private static RectTransform Cell(RectTransform parent,int col,int cols,int row,int rows){GameObject go=new GameObject("Cell",typeof(RectTransform));RectTransform rt=go.GetComponent<RectTransform>();rt.SetParent(parent,false);float x0=(float)col/cols,x1=(float)(col+1)/cols,y0=1f-(float)(row+1)/rows,y1=1f-(float)row/rows;rt.anchorMin=new Vector2(x0,y0);rt.anchorMax=new Vector2(x1,y1);rt.offsetMin=new Vector2(3f,3f);rt.offsetMax=new Vector2(-3f,-3f);return rt;}
    }
}
