using UnityEngine;
using UnityEngine.UI;
using Highfly.Combat;
using Highfly.Run0H;
using Highfly.Run0I2;

namespace Highfly.Run0I
{
    [DisallowMultipleComponent]
    public sealed class HighflyRun0IOverlay : MonoBehaviour
    {
        private Text _metrics;
        private float _nextUiRefresh;
        private bool _wiredDummy;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (FindAnyObjectByType<HighflyRun0IOverlay>() != null) return;
            GameObject go=new GameObject("HIGHFLY_RUN0I_OVERLAY");
            DontDestroyOnLoad(go);
            go.AddComponent<HighflyRun0IOverlay>();
        }

        private void Update()
        {
            if (!_wiredDummy) WireAttackDummy();
            if (Time.unscaledTime<_nextUiRefresh) return;
            _nextUiRefresh=Time.unscaledTime+0.25f;
            RelabelPanel();
            RefreshMetrics();
        }

        private void WireAttackDummy()
        {
            GameObject center=GameObject.Find("DUMMY_CENTRO");
            if (center==null) return;
            if (center.GetComponent<HighflyRun0IAttackDummy>()==null) center.AddComponent<HighflyRun0IAttackDummy>();
            _wiredDummy=true;
        }

        private void RelabelPanel()
        {
            Text[] all=FindObjectsByType<Text>(FindObjectsInactive.Include);
            for (int i=0;i<all.Length;i++)
            {
                Text t=all[i]; if (t==null) continue;
                if (t.text.Contains("RUN0H • BASE ESTABLE"))
                    t.text=t.text.Replace("RUN0H • BASE ESTABLE","RUN0I.3 • SINGLE HUNTER + GUARD");
                if (t.text.Contains("SUPER SKILLS • PRÓXIMO RUN0I"))
                    t.text="SKILLS COMPLETAS • QUALITY GATE";
                if (t.text.StartsWith("S1  LAUNCHER JUMP"))
                    t.text="S1  SONIC LEAP • 0.94s • CD 6s";
                if (t.text.StartsWith("S2  TWIN SLASH"))
                    t.text="S2  OFF • pendiente weaponTags";
                if (t.text.StartsWith("S3  PHANTOM DASH"))
                    t.text="S3  BLOQUEADA • siguiente lote completo";
                if (t.text.StartsWith("S4  MULTI CUT"))
                    t.text="DODGE = SLIDE MOVE • 2.40m • iframe";
                if (t.text.StartsWith("S5  AERIAL PURSUIT"))
                    t.text="PARRY = REPEL COUNTER • dummy centro ataca";
            }
        }

        private void RefreshMetrics()
        {
            if (_metrics==null) BuildMetrics();
            if (_metrics==null) return;
            HighflyLucidCombatBridge combat=HighflyLucidCombatBridge.Instance;
            HighflyRun0HCharacterVisual visual=HighflyRun0HCharacterVisual.Instance;
            HighflyLoadoutDefinition load=visual!=null?HighflyMeleeLibrary.Get(visual.CurrentLoadout):null;
            _metrics.text=
                "RUN0I.3 • SINGLE HUNTER / GUARD CORE\n"+
                "Loadout: "+(visual!=null?visual.CurrentLabel:"...")+"\n"+
                "Guard: "+(load!=null?load.Guard.ToString():"-")+" • Combo: "+(load!=null?load.Grammar:"-")+"\n"+
                "Acción: "+(combat!=null?combat.DebugAction:"-")+"\n"+
                "Hits: "+HighflyLucidCombatBridge.TotalHits+"  Daño: "+HighflyLucidCombatBridge.TotalDamage.ToString("0")+"\n"+
                "Último: "+HighflyLucidCombatBridge.LastHit+"\n"+
                "1 Hunter • trace por mano • Cross/Shield/Pole guard • S2 OFF";
        }

        private void BuildMetrics()
        {
            GameObject canvasGo=new GameObject("RUN0I_METRICS",typeof(Canvas),typeof(CanvasScaler));
            canvasGo.transform.SetParent(transform,false);
            Canvas canvas=canvasGo.GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay; canvas.sortingOrder=6600;
            CanvasScaler scaler=canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1920f,1080f); scaler.matchWidthOrHeight=0.5f;

            GameObject panel=new GameObject("Panel",typeof(RectTransform),typeof(Image));
            panel.transform.SetParent(canvasGo.transform,false);
            RectTransform pr=panel.GetComponent<RectTransform>();
            pr.anchorMin=pr.anchorMax=new Vector2(1f,1f); pr.pivot=new Vector2(1f,1f);
            pr.anchoredPosition=new Vector2(-24f,-24f); pr.sizeDelta=new Vector2(560f,230f);
            panel.GetComponent<Image>().color=new Color(0.018f,0.026f,0.045f,0.90f);

            GameObject textGo=new GameObject("Text",typeof(RectTransform),typeof(Text));
            textGo.transform.SetParent(panel.transform,false);
            RectTransform tr=textGo.GetComponent<RectTransform>();
            tr.anchorMin=Vector2.zero; tr.anchorMax=Vector2.one;
            tr.offsetMin=new Vector2(16f,14f); tr.offsetMax=new Vector2(-16f,-14f);
            _metrics=textGo.GetComponent<Text>();
            _metrics.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _metrics.fontSize=17; _metrics.alignment=TextAnchor.UpperLeft; _metrics.color=Color.white;
        }
    }
}
