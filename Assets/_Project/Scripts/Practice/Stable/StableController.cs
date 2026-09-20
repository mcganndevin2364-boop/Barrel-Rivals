using System;
using BarrelRivals.Core.Stable;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BarrelRivals.Practice
{
    /// <summary>Local cosmetic inspection. Preview and equipped state are deliberately separate.</summary>
    public sealed class StableController : MonoBehaviour
    {
        public const string SceneName = "MyStable";
        [Serializable] public sealed class GearCard
        {
            public string id;
            public Button button;
            public Text label, status;
            public Image thumbnail, selection;
        }
        [Serializable] public sealed class SlotTab
        {
            public StableSlot slot;
            public Button button;
            public Image highlight;
            public GameObject content;
        }
        [Serializable] public sealed class ViewBindings
        {
            public RectTransform safeArea, canvasFrame;
            public GameObject stablePanel, gearPanel, riderPanel, skillsPanel, horseOrbit, riderOrbit;
            public Text title, detail, saveStatus, equipText, riderTitle, riderDetail, riderEquipText;
            public Button equipButton, riderEquipButton;
            public Image selectedThumbnail;
            public GearCard[] cards;
            public SlotTab[] slots;
            public Image[] tabHighlights;
        }
        [SerializeField] private Transform horse, riderPreview;
        [SerializeField] private StableAppearance appearance, riderAppearance;
        [SerializeField] private Camera viewCamera;
        [SerializeField] private ViewBindings ui;
        [SerializeField] private Vector3 riderCameraPosition = new Vector3(12.7f, 1.5f, 3.2f);
        [SerializeField] private Vector3 riderCameraLookAt = new Vector3(12, 1.05f, 0);
        private Vector3 stableCameraPosition;
        private Quaternion stableCameraRotation, initialHorseRotation, initialRiderRotation;
        private float stableCameraFov;
        private string selected;
        private StableProfileStore store;
        private int view;
        private StableSlot activeSlot = StableSlot.Saddle;
        public bool IsGearOpen => view != 0;
        public bool IsRiderGearOpen => view == 2;
        public bool IsSkillsOpen => ui.skillsPanel && ui.skillsPanel.activeSelf;
        public string SelectedGear => selected;
        public StableSlot SelectedSlot => activeSlot;
        public StableProfile Equipped => Store.Current;
        private StableProfileStore Store => store ?? (store = StableSession.Store);
        private static readonly Color Gold = new Color(.85f, .67f, .36f);
        private static readonly Color Muted = new Color(.28f, .29f, .28f);

        public void ConfigureReferenceView(Transform model, Camera camera, Transform rider, ViewBindings bindings)
        {
            horse=model; appearance=model.GetComponent<StableAppearance>(); viewCamera=camera;
            riderPreview=rider; riderAppearance=rider ? rider.GetComponent<StableAppearance>() : null; ui=bindings;
        }
        public void ConfigureRiderCamera(Vector3 position, Vector3 lookAt)
        { riderCameraPosition=position; riderCameraLookAt=lookAt; }

        // Kept for older scene-builder callers; bindings still resolve by stable ID, never catalog array position.
        public void Configure(Transform model, StableAppearance visual, RectTransform safe, GameObject stable, GameObject gear,
            Text[] labels, Button equip, Button[] options, Text[] optionLabels, Image[] highlights)
        {
            horse=model; appearance=visual; viewCamera=Camera.main;
            var cards=new GearCard[options.Length];
            for(int i=0;i<options.Length;i++) cards[i]=new GearCard { id=options[i].name.Replace("Select ", ""), button=options[i], label=optionLabels[i] };
            ui=new ViewBindings { safeArea=safe,stablePanel=stable,gearPanel=gear,title=labels[0],detail=labels[1],saveStatus=labels[2],equipText=labels[3],equipButton=equip,cards=cards,slots=Array.Empty<SlotTab>(),tabHighlights=highlights };
        }
        private void Awake()
        {
            store=StableSession.Store;
            initialHorseRotation=horse.localRotation;
            if(riderPreview)initialRiderRotation=riderPreview.localRotation;
            if(viewCamera) { stableCameraPosition=viewCamera.transform.position; stableCameraRotation=viewCamera.transform.rotation; stableCameraFov=viewCamera.fieldOfView; }
            foreach(var card in ui.cards) { string id=card.id; card.button.onClick.AddListener(()=>Preview(id)); }
            ui.equipButton.onClick.AddListener(EquipSelected);
            if(ui.riderEquipButton)ui.riderEquipButton.onClick.AddListener(EquipSelected);
            ShowStable(); RefreshLayout();
        }
        private void Update() { RefreshLayout(); }

        /// <summary>Refresh against the current render destination, not a stale RectTransform layout pass.</summary>
        public void RefreshLayout()
        {
            if(ui==null || !ui.safeArea)return;
            var canvas=ui.safeArea.GetComponentInParent<Canvas>();
            var camera=canvas && canvas.renderMode==RenderMode.ScreenSpaceCamera ? canvas.worldCamera : null;
            var target=camera ? camera.targetTexture : null;
            if(target)RefreshLayout(target.width,target.height,new Rect(0,0,target.width,target.height));
            else RefreshLayout(Screen.width,Screen.height,Screen.safeArea);
        }

        /// <summary>Deterministic capture/device layout; safePixels is relative to this viewport.</summary>
        public void RefreshLayout(int viewportWidth,int viewportHeight,Rect safePixels)
        {
            if(viewportWidth<=0 || viewportHeight<=0 || ui==null || !ui.safeArea)return;
            float left=Mathf.Clamp(safePixels.xMin,0,viewportWidth);
            float right=Mathf.Clamp(safePixels.xMax,left,viewportWidth);
            float bottom=Mathf.Clamp(safePixels.yMin,0,viewportHeight);
            float top=Mathf.Clamp(safePixels.yMax,bottom,viewportHeight);
            ui.safeArea.anchorMin=new Vector2(left/viewportWidth,bottom/viewportHeight);
            ui.safeArea.anchorMax=new Vector2(right/viewportWidth,top/viewportHeight);
            ui.safeArea.offsetMin=ui.safeArea.offsetMax=Vector2.zero;
            if(ui.canvasFrame) {
                float scale=Mathf.Min((right-left)/1280f,(top-bottom)/720f);
                ui.canvasFrame.localScale=Vector3.one*Mathf.Max(.01f,scale);
            }
            if(viewCamera && stableCameraFov>0) {
                float baseFov=IsRiderGearOpen ? 38 : stableCameraFov;
                float aspect=viewportWidth/(float)viewportHeight;
                float correction=Mathf.Max(1,(1280f/720f)/aspect);
                viewCamera.fieldOfView=2*Mathf.Atan(Mathf.Tan(baseFov*Mathf.Deg2Rad*.5f)*correction)*Mathf.Rad2Deg;
            }
        }
        public void ShowStable()
        {
            SetView(0); selected=null; Apply(Store.Current); SetNotice(Store.Notice);
            if(ui.skillsPanel)ui.skillsPanel.SetActive(false);
        }
        public void ShowGear() { ShowSlot(StableSlot.Saddle); }
        public void ShowAppearance() { ShowSlot(StableSlot.Pad); }
        public void ShowRiderGear() { ShowSlot(StableSlot.Gloves); }
        public void SelectSlot(int slot)
        { if(Enum.IsDefined(typeof(StableSlot),slot))ShowSlot((StableSlot)slot); }
        public void ShowSlot(StableSlot slot)
        {
            if(!Enum.IsDefined(typeof(StableSlot),slot))return;
            activeSlot=slot; SetView(slot==StableSlot.Gloves?2:1); Preview(Store.Current.Equipped(slot));
        }
        public void ShowSkills()
        {
            ShowStable(); if(ui.skillsPanel)ui.skillsPanel.SetActive(true);
        }
        public void CloseSkills() { if(ui.skillsPanel)ui.skillsPanel.SetActive(false); }
        public void Preview(string id)
        {
            var item=StableCatalog.Find(id); if(item==null)return;
            activeSlot=item.Slot;
            SetView(item.Slot==StableSlot.Gloves?2:1);
            selected=id; var preview=Store.Current;
            if(!preview.TryEquip(item.Slot,id))return;
            Apply(preview);
            bool equipped=Store.Current.Equipped(item.Slot)==id;
            if(ui.title)ui.title.text=item.Name.ToUpperInvariant();
            if(ui.detail)ui.detail.text=item.Description+"\n\nFree cosmetic · same race performance.";
            if(ui.riderTitle)ui.riderTitle.text=item.Name.ToUpperInvariant();
            if(ui.riderDetail)ui.riderDetail.text=equipped?"Equipped for your next ride.":"Preview · equip to save this look.";
            ui.equipButton.interactable=!equipped;
            ui.equipText.text=equipped?"EQUIPPED":"EQUIP";
            if(ui.riderEquipButton)ui.riderEquipButton.interactable=!equipped;
            if(ui.riderEquipText)ui.riderEquipText.text=equipped?"EQUIPPED":"EQUIP";
            foreach(var card in ui.cards) {
                var gear=StableCatalog.Find(card.id); if(gear==null)continue;
                bool active=Store.Current.Equipped(gear.Slot)==gear.Id;
                bool inspected=gear.Id==id;
                card.label.text=gear.Name;
                if(card.status) {card.status.text=inspected&&!active?"PREVIEW":active?"EQUIPPED":"FREE";card.status.color=active||inspected?Gold:new Color(.59f,.65f,.64f);}
                if(card.selection)card.selection.color=inspected?Gold:new Color(.27f,.31f,.31f,.6f);
                card.button.targetGraphic.color=inspected?new Color(.18f,.16f,.11f,.98f):new Color(.04f,.055f,.055f,.96f);
                if(inspected && ui.selectedThumbnail && card.thumbnail)ui.selectedThumbnail.sprite=card.thumbnail.sprite;
            }
            RefreshSlots(); SetNotice(equipped?Store.Notice:"Preview only · EQUIP saves this style. BACK TO STABLE cancels the preview.");
        }
        public void EquipSelected()
        {
            var item=StableCatalog.Find(selected); if(item==null || !Store.Equip(item.Slot,selected))return;
            Preview(selected);
        }
        public void CancelPreview()
        { if(selected==null)ShowStable();else Preview(Store.Current.Equipped(activeSlot)); }
        public void RotateHorse(float degrees)
        {
            if(IsRiderGearOpen) { if(riderPreview)riderPreview.Rotate(0,degrees,0,Space.World); }
            else if(!IsGearOpen && !IsSkillsOpen)horse.Rotate(0,degrees,0,Space.World);
        }
        public void ResetView()
        { horse.localRotation=initialHorseRotation; if(riderPreview)riderPreview.localRotation=initialRiderRotation; }
        public void Race()
        { Apply(Store.Current); SceneManager.LoadScene(ReinsLabController.SceneName); }
        private void Apply(StableProfile profile)
        { if(appearance)appearance.Apply(profile); if(riderAppearance)riderAppearance.Apply(profile); }
        private void SetNotice(string message) { if(ui.saveStatus)ui.saveStatus.text=message; }
        private void SetView(int next)
        {
            view=next;
            ui.stablePanel.SetActive(next==0); ui.gearPanel.SetActive(next==1);
            if(ui.riderPanel)ui.riderPanel.SetActive(next==2);
            if(ui.skillsPanel)ui.skillsPanel.SetActive(false);
            if(ui.horseOrbit)ui.horseOrbit.SetActive(next==0);
            if(ui.riderOrbit)ui.riderOrbit.SetActive(next==2);
            horse.gameObject.SetActive(next!=2);
            if(riderPreview)riderPreview.gameObject.SetActive(next==2);
            if(viewCamera) {
                if(next==2) { viewCamera.transform.position=riderCameraPosition;viewCamera.transform.LookAt(riderCameraLookAt);viewCamera.fieldOfView=38; }
                else {viewCamera.transform.SetPositionAndRotation(stableCameraPosition,stableCameraRotation);viewCamera.fieldOfView=stableCameraFov;}
            }
            for(int i=0;i<ui.tabHighlights.Length;i++)ui.tabHighlights[i].color=i==next?Gold:Muted;
            RefreshSlots(); RefreshLayout();
        }
        private void RefreshSlots()
        {
            foreach(var tab in ui.slots) {
                bool active=tab.slot==activeSlot;
                tab.content.SetActive(active);
                if(tab.highlight)tab.highlight.color=active?Gold:Muted;
            }
        }
    }
}
