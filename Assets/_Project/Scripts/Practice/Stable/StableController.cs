using BarrelRivals.Core.Stable;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BarrelRivals.Practice
{
    public sealed class StableController : MonoBehaviour
    {
        public const string SceneName="MyStable";
        [SerializeField] private Transform horse;
        [SerializeField] private StableAppearance appearance;
        [SerializeField] private RectTransform safeArea;
        [SerializeField] private GameObject stablePanel,gearPanel;
        [SerializeField] private Text title,detail,saveStatus,equipText;
        [SerializeField] private Button equipButton;
        [SerializeField] private Button[] gearButtons;
        [SerializeField] private Text[] gearLabels;
        [SerializeField] private Image[] tabHighlights;
        private string selected;
        private StableProfileStore store;
        public bool IsGearOpen { get; private set; }
        public string SelectedGear => selected;
        public StableProfile Equipped => store.Current.Copy();
        public void Configure(Transform model,StableAppearance view,RectTransform safe,GameObject stable,GameObject gear,
            Text[] labels,Button equip,Button[] options,Text[] optionLabels,Image[] highlights)
        { horse=model;appearance=view;safeArea=safe;stablePanel=stable;gearPanel=gear;title=labels[0];detail=labels[1];saveStatus=labels[2];equipText=labels[3];equipButton=equip;gearButtons=options;gearLabels=optionLabels;tabHighlights=highlights; }
        private void Awake()
        {
            store=StableSession.Store;
            for(int i=0;i<gearButtons.Length;i++) { string id=StableCatalog.Gear[i].Id;gearButtons[i].onClick.AddListener(()=>Preview(id)); }
            equipButton.onClick.AddListener(EquipSelected); ShowStable();
        }
        private void Update()
        {
            if(Screen.width==0 || Screen.height==0)return;
            var area=Screen.safeArea;safeArea.anchorMin=new Vector2(area.xMin/Screen.width,area.yMin/Screen.height);safeArea.anchorMax=new Vector2(area.xMax/Screen.width,area.yMax/Screen.height);
        }
        public void ShowStable()
        {
            IsGearOpen=false;stablePanel.SetActive(true);gearPanel.SetActive(false);
            appearance.Apply(store.Current);saveStatus.text=store.Notice;HighlightTabs();
        }
        public void ShowGear()
        {
            IsGearOpen=true;stablePanel.SetActive(false);gearPanel.SetActive(true);
            Preview(store.Current.saddleId);HighlightTabs();
        }
        public void Preview(string id)
        {
            var item=StableCatalog.Find(id);if(item==null)return;
            selected=id;var preview=store.Current.Copy();preview.TryEquip(item.Slot,id);appearance.Apply(preview);
            bool equipped=store.Current.Equipped(item.Slot)==id;
            title.text=item.Name.ToUpperInvariant();detail.text=item.Description+"\n\nAppearance only · no racing advantage.";
            equipButton.interactable=!equipped;equipText.text=equipped?"EQUIPPED":"EQUIP GEAR";
            saveStatus.text=equipped?store.Notice:"Previewing · equip to save this look.";
            for(int i=0;i<gearLabels.Length;i++) {
                var gear=StableCatalog.Gear[i];bool active=store.Current.Equipped(gear.Slot)==gear.Id;
                gearLabels[i].text=gear.Name+"\n"+(gear.Id==id && !active?"PREVIEW":active?"EQUIPPED":"OWNED");
                gearButtons[i].targetGraphic.color=gear.Id==id?new Color(.28f,.23f,.14f,.98f):new Color(.06f,.07f,.065f,.93f);
            }
        }
        public void EquipSelected()
        {
            var item=StableCatalog.Find(selected);if(item==null || !store.Equip(item.Slot,selected))return;
            Preview(selected);
        }
        public void RotateHorse(float degrees) { horse.Rotate(0,degrees,0,Space.World); }
        public void ResetView() { horse.localRotation=Quaternion.Euler(0,-22,0); }
        public void Race() { SceneManager.LoadScene(ReinsLabController.SceneName); }
        private void HighlightTabs()
        { for(int i=0;i<tabHighlights.Length;i++)tabHighlights[i].color=(i==1)==IsGearOpen?new Color(.82f,.67f,.42f):new Color(.3f,.29f,.25f); }
    }

}
