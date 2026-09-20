using System;
using System.Collections.Generic;
using BarrelRivals.Core.Reins;
using BarrelRivals.Core.Stable;
using BarrelRivals.Practice;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

namespace BarrelRivals.Editor
{
    /// <summary>Live landscape UI, laid out from the user's stable/tack/rider reference.</summary>
    public static class StableUiBuilder
    {
        public const string ThumbnailRoot="Assets/_Project/Art/Reins/Stable/Thumbnails";
        private static readonly Color Ink=new Color(.025f,.036f,.035f,.95f);
        private static readonly Color Gold=new Color(.85f,.67f,.36f);
        private static readonly Color Paper=new Color(.94f,.92f,.85f);
        private static readonly Color Muted=new Color(.62f,.67f,.65f);
        public static StableController Build(Transform horse,Camera camera,Transform riderPreview=null)
        {
            var owner=new GameObject("MyStable controller").AddComponent<StableController>();
            var go=new GameObject("Stable HUD",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            go.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            // A single explicit frame scale owns layout. Canvas units remain render pixels,
            // so a RenderTexture capture never inherits the Editor Game View's scale.
            var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor=1;scaler.referencePixelsPerUnit=100;
            var safe=Group(go.transform,"Safe area");
            // Fit the authored landscape layout inside both safe bounds, including 4:3 tablets.
            var frame=Group(safe,"Landscape frame");
            frame.anchorMin=frame.anchorMax=frame.pivot=new Vector2(.5f,.5f);
            frame.anchoredPosition=Vector2.zero;frame.sizeDelta=new Vector2(1280,720);
            var ui=new StableController.ViewBindings();ui.safeArea=safe;ui.canvasFrame=frame;
            var cards=new List<StableController.GearCard>();var slots=new List<StableController.SlotTab>();
            var header=Panel(frame,"Stable header",new Vector2(0,1),Vector2.zero,new Vector2(1280,82),Ink);
            Label(header,"Brand","BARREL RIVALS",new Vector2(0,1),new Vector2(24,-7),new Vector2(396,45),25,true);
            Label(header,"Location","COPPER CREEK  /  YOUR STABLE",new Vector2(0,1),new Vector2(26,-49),new Vector2(360,24),12).color=Gold;
            string[] tabNames={"MyStable tab","Gear tab","Rider gear tab"};
            string[] tabLabels={"MY STABLE","TACK","RIDER GEAR"};
            var marks=new Image[3];
            for(int i=0;i<3;i++) {
                var tab=Button(header,tabNames[i],tabLabels[i],new Vector2(0,1),new Vector2(458+i*195,-17),new Vector2(183,48),17,out _);
                if(i==0)UnityEventTools.AddPersistentListener(tab.onClick,owner.ShowStable);
                if(i==1)UnityEventTools.AddPersistentListener(tab.onClick,owner.ShowGear);
                if(i==2)UnityEventTools.AddPersistentListener(tab.onClick,owner.ShowRiderGear);
                marks[i]=Panel(tab.transform,"Active tab",new Vector2(.5f,0),new Vector2(0,1),new Vector2(163,2),Gold).GetComponent<Image>();
            }
            ui.tabHighlights=marks;
            Label(header,"Collection","FREE\nCOLLECTION",new Vector2(1,.5f),new Vector2(-24,0),new Vector2(175,42),12,false,TextAnchor.MiddleRight).color=Gold;
            ui.stablePanel=Group(frame,"MyStable content").gameObject;
            BuildStable(ui.stablePanel.transform,owner);
            ui.horseOrbit=Orbit(ui.stablePanel.transform,owner,"Drag horse to rotate",new Vector2(614,354),new Vector2(655,405));
            ui.horseOrbit.transform.SetAsFirstSibling();
            ui.skillsPanel=BuildSkills(ui.stablePanel.transform,owner).gameObject;
            ui.gearPanel=Group(frame,"Gear content").gameObject;
            BuildTack(ui.gearPanel.transform,owner,ui,cards,slots);
            ui.riderPanel=Group(frame,"Rider gear content").gameObject;
            BuildRider(ui.riderPanel.transform,owner,ui,cards);
            var footer=Panel(frame,"Stable footer",new Vector2(0,0),Vector2.zero,new Vector2(1280,82),Ink);
            Panel(footer,"Footer hairline",new Vector2(.5f,1),Vector2.zero,new Vector2(1232,1),new Color(.58f,.45f,.26f,.55f));
            ui.saveStatus=Label(footer,"Save status","",new Vector2(0,.5f),new Vector2(24,0),new Vector2(766,54),14,false,TextAnchor.MiddleLeft);
            var race=Button(footer,"Race","RIDE TO ARENA",new Vector2(1,.5f),new Vector2(-24,0),new Vector2(282,50),18,out var raceText);
            race.targetGraphic.color=Gold;raceText.color=Ink;UnityEventTools.AddPersistentListener(race.onClick,owner.Race);
            ui.cards=cards.ToArray();ui.slots=slots.ToArray();
            owner.ConfigureReferenceView(horse,camera,riderPreview,ui);
            return owner;
        }
        private static void BuildStable(Transform parent,StableController owner)
        {
            var roster=Panel(parent,"Horse roster",new Vector2(0,1),new Vector2(24,-107),new Vector2(236,233),Ink);
            Label(roster,"Roster heading","MY STABLE",new Vector2(0,1),new Vector2(14,-13),new Vector2(208,28),20);
            var card=Panel(roster,"Copper roster card",new Vector2(0,1),new Vector2(11,-54),new Vector2(214,96),new Color(.27f,.21f,.12f,.86f));
            Picture(card,"Copper portrait",StableCatalog.HorseId,new Vector2(0,.5f),new Vector2(4,0),new Vector2(84,82));
            Label(card,"Copper name",StableCatalog.HorseName,new Vector2(0,1),new Vector2(92,-20),new Vector2(117,30),21);
            Label(card,"Copper availability","BAY · YOUR HORSE",new Vector2(0,1),new Vector2(92,-54),new Vector2(117,20),10).color=Gold;
            Label(roster,"Roster truth","01 HORSE IN YOUR STABLE\nMore horses will arrive with progression.",new Vector2(0,1),new Vector2(14,-169),new Vector2(208,53),12).color=Muted;
            var stats=Panel(parent,"Horse traits",new Vector2(1,1),new Vector2(-24,-107),new Vector2(272,336),Ink);
            Label(stats,"Selected horse",StableCatalog.HorseName.ToUpperInvariant(),new Vector2(0,1),new Vector2(20,-18),new Vector2(232,39),28,true);
            Label(stats,"Trait heading","BASE RIDING TRAITS",new Vector2(0,1),new Vector2(20,-64),new Vector2(232,22),12).color=Gold;
            var profile=new ReinsHorseProfile();
            string[] names={"NERVE","FIRE","BIDDABILITY","HEART"};
            int[] values={profile.NervePermille,profile.FirePermille,profile.BiddabilityPermille,profile.HeartPermille};
            for(int i=0;i<4;i++) {
                Label(stats,"Trait "+names[i],names[i],new Vector2(0,1),new Vector2(20,-107-i*40),new Vector2(124,25),12);
                var bar=Panel(stats,"Base "+names[i],new Vector2(0,1),new Vector2(143,-112-i*40),new Vector2(108,8),new Color(.16f,.23f,.23f));
                Panel(bar,"Value",new Vector2(0,.5f),Vector2.zero,new Vector2(108*values[i]/1000f,8),new Color(.22f,.8f,.66f));
                Label(stats,"Value "+names[i],(values[i]/10).ToString(),new Vector2(1,1),new Vector2(-20,-122-i*40),new Vector2(54,17),10,false,TextAnchor.MiddleRight).color=Muted;
            }
            Label(stats,"Trait note","Cosmetic equipment does not change\nthese base ratings. No earned level yet.",new Vector2(0,1),new Vector2(20,-281),new Vector2(234,39),12).color=Muted;
            Label(parent,"Stable instruction","DRAG COPPER TO LOOK AROUND",new Vector2(.5f,0),new Vector2(0,213),new Vector2(440,23),12,false,TextAnchor.MiddleCenter).color=Gold;
            var reset=Button(parent,"Reset view","RESET VIEW",new Vector2(.5f,0),new Vector2(0,173),new Vector2(156,32),12,out _);
            UnityEventTools.AddPersistentListener(reset.onClick,owner.ResetView);
            var appearance=Button(parent,"Appearance","APPEARANCE",new Vector2(0,0),new Vector2(343,94),new Vector2(179,47),15,out _);
            UnityEventTools.AddPersistentListener(appearance.onClick,owner.ShowAppearance);
            var tack=Button(parent,"Customize Copper","TACK",new Vector2(0,0),new Vector2(535,94),new Vector2(179,47),15,out _);
            UnityEventTools.AddPersistentListener(tack.onClick,owner.ShowGear);
            var skills=Button(parent,"Riding traits","SKILLS",new Vector2(0,0),new Vector2(727,94),new Vector2(179,47),15,out _);
            UnityEventTools.AddPersistentListener(skills.onClick,owner.ShowSkills);
        }
        private static RectTransform BuildSkills(Transform parent,StableController owner)
        {
            var shade=Panel(parent,"Riding traits overlay",new Vector2(.5f,.5f),Vector2.zero,new Vector2(1280,556),new Color(.015f,.025f,.026f,.80f));shade.GetComponent<Image>().raycastTarget=true;
            var box=Panel(shade,"Trait information",new Vector2(.5f,.5f),Vector2.zero,new Vector2(670,438),Ink);
            Label(box,"Traits title","COPPER'S RIDING TRAITS",new Vector2(0,1),new Vector2(30,-23),new Vector2(610,39),25,true).color=Gold;
            Label(box,"Traits explanation","BASE RATINGS  ·  50 / 100 EACH\n\nNERVE   Cornering grip.\nFIRE   Top pace and braking balance.\nBIDDABILITY   Turning response to the reins.\nHEART   Pace conditioning in later rounds.\n\nThese are Copper's current base ratings. Free practice uses the first round; Heart does not add pace here. Earned levels and training are still in development. Tack changes appearance only.",new Vector2(0,1),new Vector2(30,-82),new Vector2(610,271),17);
            var close=Button(box,"Close riding traits","BACK TO STABLE",new Vector2(.5f,0),new Vector2(0,21),new Vector2(256,44),16,out _);
            UnityEventTools.AddPersistentListener(close.onClick,owner.CloseSkills);
            return shade;
        }
        private static void BuildTack(Transform parent,StableController owner,StableController.ViewBindings ui,List<StableController.GearCard> cards,List<StableController.SlotTab> slots)
        {
            Panel(parent,"Tack backdrop",new Vector2(0,1),new Vector2(24,-106),new Vector2(860,508),new Color(.025f,.035f,.035f,.89f));
            string[] names={"SADDLES","PADS","REINS","HEADSTALLS"};
            StableSlot[] slotValues={StableSlot.Saddle,StableSlot.Pad,StableSlot.Reins,StableSlot.Headstall};
            for(int s=0;s<slotValues.Length;s++) {
                var slot=slotValues[s];
                var button=Button(parent,"Filter "+slot,names[s],new Vector2(0,1),new Vector2(38+s*210,-120),new Vector2(199,42),14,out _);
                UnityEventTools.AddIntPersistentListener(button.onClick,owner.SelectSlot,(int)slot);
                var mark=Panel(button.transform,"Active slot",new Vector2(.5f,0),Vector2.zero,new Vector2(179,2),Gold).GetComponent<Image>();
                var content=Group(parent,slot+" collection");int i=0;
                foreach(var item in StableCatalog.Gear)if(item.Slot==slot) {
                    cards.Add(Card(content,item,new Vector2(38+i%4*210,-178-i/4*182),new Vector2(199,169)));i++;
                }
                slots.Add(new StableController.SlotTab{slot=slot,button=button,highlight=mark,content=content.gameObject});
            }
            Label(parent,"Tack collection note","YOUR LOOK. YOUR RIDE.",new Vector2(0,1),new Vector2(45,-550),new Vector2(800,42),24,true,TextAnchor.MiddleCenter).color=Gold;
            Label(parent,"Tack fairness","Every style is free · no speed, timing or steering advantage",new Vector2(0,1),new Vector2(45,-591),new Vector2(800,19),12,false,TextAnchor.MiddleCenter).color=Muted;
            var inspect=Panel(parent,"Selected gear",new Vector2(1,1),new Vector2(-24,-106),new Vector2(348,508),Ink);
            Label(inspect,"Gear eyebrow","TACK  /  INSPECT",new Vector2(0,1),new Vector2(22,-15),new Vector2(304,21),12).color=Gold;
            ui.selectedThumbnail=Picture(inspect,"Selected gear image","saddle-ranch",new Vector2(.5f,1),new Vector2(0,-40),new Vector2(302,168));
            ui.title=Label(inspect,"Gear title","",new Vector2(0,1),new Vector2(22,-211),new Vector2(304,65),24,true);
            ui.detail=Label(inspect,"Gear description","",new Vector2(0,1),new Vector2(22,-285),new Vector2(304,124),15);
            ui.equipButton=Button(inspect,"Equip selected","EQUIP",new Vector2(.5f,0),new Vector2(0,55),new Vector2(304,46),18,out ui.equipText);
            ui.equipButton.targetGraphic.color=Gold;ui.equipText.color=Ink;
            var back=Button(inspect,"Cancel tack preview","BACK TO STABLE",new Vector2(.5f,0),new Vector2(0,13),new Vector2(304,33),12,out _);
            UnityEventTools.AddPersistentListener(back.onClick,owner.ShowStable);
        }
        private static void BuildRider(Transform parent,StableController owner,StableController.ViewBindings ui,List<StableController.GearCard> cards)
        {
            Panel(parent,"Rider heading backdrop",new Vector2(0,1),new Vector2(20,-106),new Vector2(476,122),Ink);
            Label(parent,"Rider heading","RIDER GEAR",new Vector2(0,1),new Vector2(28,-112),new Vector2(446,42),28,true);
            Label(parent,"Rider category","GLOVES  /  6 FREE STYLES",new Vector2(0,1),new Vector2(30,-158),new Vector2(444,25),13).color=Gold;
            Label(parent,"Rider description","Visible on your hands during Reins Racing.",new Vector2(0,1),new Vector2(30,-190),new Vector2(442,42),15).color=Muted;
            ui.riderOrbit=Orbit(parent,owner,"Drag glove to rotate",new Vector2(256,401),new Vector2(490,316));
            Label(parent,"Rider motto","RIDE YOUR STYLE.",new Vector2(0,0),new Vector2(28,141),new Vector2(438,40),26,true,TextAnchor.MiddleCenter).color=Gold;
            var reset=Button(parent,"Reset rider view","RESET VIEW",new Vector2(0,0),new Vector2(58,99),new Vector2(155,34),12,out _);UnityEventTools.AddPersistentListener(reset.onClick,owner.ResetView);
            var back=Button(parent,"Cancel rider preview","BACK TO STABLE",new Vector2(0,0),new Vector2(225,99),new Vector2(222,34),12,out _);UnityEventTools.AddPersistentListener(back.onClick,owner.ShowStable);
            Panel(parent,"Rider collection backdrop",new Vector2(0,1),new Vector2(520,-106),new Vector2(736,508),new Color(.025f,.035f,.035f,.90f));
            int i=0;foreach(var item in StableCatalog.Gear)if(item.Slot==StableSlot.Gloves) {
                cards.Add(Card(parent,item,new Vector2(534+i%3*238,-119-i/3*179),new Vector2(224,166)));i++;
            }
            var details=Panel(parent,"Selected rider gear",new Vector2(0,1),new Vector2(534,-487),new Vector2(700,112),Ink);
            ui.riderTitle=Label(details,"Rider gear title","",new Vector2(0,1),new Vector2(15,-13),new Vector2(457,40),22,true);
            ui.riderDetail=Label(details,"Rider gear description","",new Vector2(0,1),new Vector2(15,-61),new Vector2(460,34),14);
            ui.riderEquipButton=Button(details,"Equip rider selected","EQUIP",new Vector2(1,.5f),new Vector2(-14,0),new Vector2(180,50),17,out ui.riderEquipText);
            ui.riderEquipButton.targetGraphic.color=Gold;ui.riderEquipText.color=Ink;
        }
        private static StableController.GearCard Card(Transform parent,StableGear item,Vector2 position,Vector2 size)
        {
            var card=Button(parent,"Select "+item.Id,"",new Vector2(0,1),position,size,14,out var label);
            label.rectTransform.anchorMin=label.rectTransform.anchorMax=label.rectTransform.pivot=new Vector2(.5f,0);
            label.rectTransform.anchoredPosition=new Vector2(0,26);label.rectTransform.sizeDelta=new Vector2(size.x-10,31);label.text=item.Name;
            var image=Picture(card.transform,"Item image",item.Id,new Vector2(.5f,1),new Vector2(0,-7),new Vector2(size.x-14,size.y-67));
            var status=Label(card.transform,"Ownership","FREE",new Vector2(.5f,0),new Vector2(0,7),new Vector2(size.x-12,18),10,false,TextAnchor.MiddleCenter);
            var mark=Panel(card.transform,"Selection",new Vector2(.5f,1),Vector2.zero,new Vector2(size.x,3),Gold).GetComponent<Image>();
            return new StableController.GearCard{id=item.Id,button=card,label=label,status=status,thumbnail=image,selection=mark};
        }
        private static GameObject Orbit(Transform parent,StableController owner,string name,Vector2 position,Vector2 size)
        {
            var orbit=Panel(parent,name,Vector2.zero,position,size,Color.clear);orbit.pivot=new Vector2(.5f,.5f);
            orbit.GetComponent<Image>().raycastTarget=true;orbit.gameObject.AddComponent<StableOrbit>().Configure(owner);return orbit.gameObject;
        }
        private static Image Picture(Transform parent,string name,string id,Vector2 anchor,Vector2 position,Vector2 size)
        {
            var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(ThumbnailRoot+"/"+id+".png");
            if(!sprite)throw new InvalidOperationException("Missing rendered stable thumbnail: "+id);
            var r=Panel(parent,name,anchor,position,size,Color.white);var image=r.GetComponent<Image>();image.sprite=sprite;image.preserveAspect=true;return image;
        }
        private static RectTransform Group(Transform parent,string name)
        {var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;return r;}
        private static RectTransform Panel(Transform parent,string name,Vector2 anchor,Vector2 position,Vector2 size,Color color)
        {var r=new GameObject(name,typeof(RectTransform),typeof(Image)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=anchor;r.anchoredPosition=position;r.sizeDelta=size;var image=r.GetComponent<Image>();image.color=color;image.raycastTarget=false;return r;}
        private static Text Label(Transform parent,string name,string text,Vector2 anchor,Vector2 position,Vector2 size,int fontSize,bool display=false,TextAnchor alignment=TextAnchor.UpperLeft)
        {var r=new GameObject(name,typeof(RectTransform),typeof(Text)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=anchor;r.anchoredPosition=position;r.sizeDelta=size;var label=r.GetComponent<Text>();label.font=AssetDatabase.LoadAssetAtPath<Font>(ReinsPremiumArenaBuilder.Root+"/Fonts/"+(display?"Cinzel-SemiBold.ttf":"Lato-Regular.ttf"));label.fontSize=fontSize;label.color=Paper;label.text=text;label.alignment=alignment;label.horizontalOverflow=HorizontalWrapMode.Wrap;label.verticalOverflow=VerticalWrapMode.Truncate;label.raycastTarget=false;return label;}
        private static Button Button(Transform parent,string name,string text,Vector2 anchor,Vector2 position,Vector2 size,int fontSize,out Text label)
        {var r=Panel(parent,name,anchor,position,size,new Color(.06f,.075f,.072f,.96f));r.GetComponent<Image>().raycastTarget=true;var button=r.gameObject.AddComponent<Button>();button.targetGraphic=r.GetComponent<Image>();var colors=button.colors;colors.disabledColor=new Color(.57f,.57f,.52f,.82f);colors.pressedColor=new Color(.71f,.65f,.50f);colors.highlightedColor=new Color(.96f,.92f,.84f);button.colors=colors;label=Label(r,"Label",text,new Vector2(.5f,.5f),Vector2.zero,size-new Vector2(12,4),fontSize,false,TextAnchor.MiddleCenter);return button;}
    }
}
