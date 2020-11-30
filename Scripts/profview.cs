using System;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using XiaWorld;
using FairyGUI;
using UnityEngine;


namespace profView
{
    public class init
    {
        public void main()
        {
            KLog.Dbg("profView ACS mod init");
            UIHud.getInstance().init();
            EventManager eventManager = new EventManager();
            try
            {
                eventManager.registerEvents();
            }
            catch (Exception e)
            {
                KLog.Err("Error happened!" + e.ToString() + "\n" + e.StackTrace.ToString());
            }
        }
    }

    public class UIHud
    {
        private static UIHud instance;
        internal FairyGUI.GTextField infoTxt = new FairyGUI.GTextField();
        public string text = "";
        public string text_tip = "";
        private UIHud()
        {
        }
        public static UIHud getInstance()
        {
            if (instance == null)
            {
                instance = new UIHud();
            };
            return instance;
        }
        public void init()
        {
            infoTxt.draggable = true;
            infoTxt.SetXY(0, 440);
            infoTxt.color = Color.yellow;
            infoTxt.text = "";
            infoTxt.fontsize = 11;
            infoTxt.name = "profViewStat";
            infoTxt.autoSize = FairyGUI.AutoSizeType.Both;
            infoTxt.visible = true;
            Wnd_GameMain.Instance.AddChild(infoTxt);
        }
        public void update()
        {
            infoTxt.tooltips = text_tip;
            infoTxt.text = text;
            text = "";
        }

    }
    public class EventManager
    {
        public void registerEvents()
        {
            TickCounter tickCounter = new TickCounter();
            TickingUtil tickingUtil = new TickingUtil();
            tickCounter.init();
            XiaWorld.EventMgr.Instance.RegisterEvent(XiaWorld.g_emEvent.UpdateFrame, tickCounter.countcb);
            XiaWorld.EventMgr.Instance.RegisterEvent(XiaWorld.g_emEvent.UpdateFrame, tickingUtil.drawBtn_cb);
            //XiaWorld.EventMgr.Instance.RegisterEvent(XiaWorld.g_emEvent.Render, tickingUtil.drawBtn_cb);
        }
    }
    public class TickCounter
    {
         
        internal int tickCnt;
        internal float tickPerSecond;
        internal DateTime lastTpsUpdate = new DateTime();

        public void init()
        {
            lastTpsUpdate = DateTime.Now;
        }

        public void countcb(Thing sender, object[] obs)
        {
            tickCnt++;
            printFps();
        }
        /*
        m_n43:左上角数字时钟
        m_n137:?
        */
        internal MapGridUtil mapGridUtil = new MapGridUtil();
        public void printFps()
        {
            if (tickCnt == 30)
            {
                Wnd_GameMain.Instance.UIInfo.m_n43.autoSize = FairyGUI.AutoSizeType.Both;
                tickCnt = 0;
                tickPerSecond = (float)(30 / ((DateTime.Now - lastTpsUpdate).TotalMilliseconds / 1000));
                lastTpsUpdate = DateTime.Now;

                UIHud.getInstance().text = "每秒更新次数:" + tickPerSecond.ToString().Substring(0, 5) +
                                                "\n每次更新前进时间:" + (UnityEngine.Time.deltaTime * 1000).ToString().Substring(0, 5) +
                                                "\n游戏速度:x" + (tickPerSecond * UnityEngine.Time.deltaTime).ToString().Substring(0, 4);
                UIHud.getInstance().text += "\n" + mapGridUtil.getPreciseGridData();
                try
                {
                    UIHud.getInstance().text += "\n" + mapGridUtil.getGridDataInfluenceFactor();
                }
                catch (Exception e)
                {
                    KLog.Err("Error happened!" + e.ToString() + "\n" + e.StackTrace.ToString());
                }

                UIHud.getInstance().update();
            }
        }
    }
    public class MapGridUtil
    {
        public AreaBase getAreaByGridKey(int gridKey)
        {
            foreach (var area in XiaWorld.AreaMgr.Instance.m_lisAreas)
            {
                if (area.GridInArea(gridKey))
                {
                    return area;
                }
            }
            return null;
        }

        public string getPreciseGridData()
        {
            string data = "";
            int mouseGrid = UI_WorldLayer.Instance.MouseGridKey;
            XiaWorld.Map curMap = XiaWorld.World.Instance.map;
            AreaBase area = getAreaByGridKey(mouseGrid);
            if (area != null && area.def.AreaType == "Room")
            {    //AreaTypeDef.xml
                AreaRoom area_room = area as AreaRoom;
                data += "拥挤程度:" + area_room.GetLayoutEvaluate().ToString() + "\n"; //GameDefine.LayoutValueDesc :: 0..空旷..0.2..恰当..0.7..拥挤..1
                if (area_room.FengshuiValue + 1e8 < 1)
                {
                    data += "风水值:房间无效(此房间不包含风水建筑)\n";
                }
                else
                {
                    data += "风水值:" + area_room.FengshuiValue.ToString(); //GameDefine.RoomFengShuiRankMap :: -1e8..none..-1e5..worst..-6..verybad..-2..bad..3..good..8..verygood..13..best
                    if (GameDefine.GetRoomFengshui(area_room.FengshuiValue) != XiaWorld.g_emFengshuiRank.Best)
                    {
                        float nextLevelValue = 0.0f;
                        foreach (KeyValuePair<float, XiaWorld.g_emFengshuiRank> kvp in GameDefine.RoomFengShuiRankMap)
                        {
                            if (kvp.Value == GameDefine.GetRoomFengshui(area_room.FengshuiValue) - 1)
                            {
                                nextLevelValue = kvp.Key;
                            }
                        }
                        data += "(还差" + (nextLevelValue - area_room.FengshuiValue).ToString() + "到达下一级)\n";
                    }
                    else
                    {
                        data += "\n";
                    }
                }
            }
                return data;
        }
        public struct itemHeat
        {
            public string itemName;
            public float heat;
        };
        public string getGridDataInfluenceFactor()
        {
            string data = "";
            int mouseGrid = UI_WorldLayer.Instance.MouseGridKey;
            XiaWorld.Map curMap = XiaWorld.World.Instance.map;
            AreaBase area = getAreaByGridKey(mouseGrid);
            if (area != null && area.def.AreaType == "Room")
            {
                AreaRoom area_room = area as AreaRoom;
                //温度
                data += "温度:(室内)";
                List<itemHeat> heatInfluenceFactor = new List<itemHeat>(10);
                foreach (var thing in area_room.m_lisThingsInRoom)
                {
                    if (thing != null && thing.IsValid)
                    {
                        if (thing.def.Heat != null)
                        {
                            itemHeat tmp = new itemHeat();
                            tmp.itemName = thing.GetName();
                            tmp.heat = thing.def.Heat.RoomValue * 25f * thing.def.Heat.Failing * thing.def.Heat.Failing;
                            heatInfluenceFactor.Add(tmp);
                        }
                        else if (thing.StuffDef != null && thing.StuffDef.Heat != null)
                        {
                            itemHeat tmp = new itemHeat();
                            tmp.itemName = thing.GetName();
                            tmp.heat = thing.StuffDef.Heat.RoomValue * 25f * thing.StuffDef.Heat.Failing * thing.StuffDef.Heat.Failing * Mathf.Max(1f, (float)thing.StuffCount) / 4f;
                            heatInfluenceFactor.Add(tmp);
                        }
                    }
                }
                foreach (var thing in area_room.m_lisWalls)
                {
                    if (thing != null && thing.IsValid)
                    {
                        if (thing.StuffDef != null && thing.StuffDef.Heat != null)
                        {
                            itemHeat tmp = new itemHeat();
                            tmp.itemName = thing.GetName();
                            tmp.heat = thing.StuffDef.Heat.RoomValue * 25f * thing.StuffDef.Heat.Failing * thing.StuffDef.Heat.Failing * Mathf.Max(1f, (float)thing.StuffCount) / 4f;
                            heatInfluenceFactor.Add(tmp);
                        }
                    }
                }
                Dictionary<itemHeat, int> DicTmp = new Dictionary<itemHeat, int>(10);
                float targetTemp = 0.0f;
                foreach (var item in heatInfluenceFactor){
                    targetTemp += item.heat;
                }
                foreach (var item in heatInfluenceFactor)
                {
                    if (DicTmp.ContainsKey(item))
                    {
                        DicTmp[item]++;
                    }
                    else
                    {
                        DicTmp.Add(item, 1);
                    }
                }
                foreach (var item in DicTmp)
                {
                    data += item.Key.itemName + "(" + item.Key.heat.ToString() + ")" + "x" + item.Value.ToString() + ",";
                }
                targetTemp /= area_room.m_lisGrids.Count;
                targetTemp += curMap.World.Weather.GetBaseTemperature() + curMap.GetMapProperty(g_emMapProperty.TempAddV);
                data += "/房间大小(" + area_room.m_lisGrids.Count + "),";
                data += "天气(" + curMap.World.Weather.GetBaseTemperature().ToString() + "),";
                data += "地图全局(" + curMap.GetMapProperty(g_emMapProperty.TempAddV).ToString() + ")";
                data += "->目标温度:" + targetTemp.ToString() +"\n";

                //风水


            }
            return data;

        }

    }
    
    public class TickingUtil : MonoBehaviour{
        bool have_init = false;
         public void drawBtn_cb(Thing sender, object[] obs){
             drawBtn();
         }
         public void clickBtn_cb(EventContext evt){
            string btnName = (evt.data as GObject).name;
            if(btnName == "pause"){
                if(XiaWorld.MainManager.Instance.Runing)
                XiaWorld.MainManager.Instance.Pause(false);
                return;
            }
            if(btnName == "play"){
                MainManager.Instance.Play(1,false);
                return;
            }
            if(btnName == "fast1"){
                MainManager.Instance.Play(2,false);
                return;
            }
            if(btnName == "fast2"){
                MainManager.Instance.Play(3,false);
                return;
            }

            if(btnName == "tickWarp"){
                showTimeLeapDialog();
            }
            
         }
        public void drawBtn(){
            if(have_init) return;
            KLog.Dbg("profView:ui init start");
            var UIInfo = Wnd_GameMain.Instance.UIInfo;
            if (UIInfo == null) return;
            var gamePlay = Wnd_GameMain.Instance.UIInfo.m_gameplay;
            if (gamePlay == null) return;
            var ElementBtn = Wnd_GameMain.Instance.UIInfo.m_ShowElement;
            ElementBtn.x = gamePlay.x + gamePlay.width + 25;
            ElementBtn.y = gamePlay.y - 10;
            var warpBtn = gamePlay.AddItemFromPool();
            warpBtn.name = "tickWarp";
            warpBtn.icon = "res/Sprs/ui/icon_ronglian";
            warpBtn.scale = new Vector2(1.0f/3.5f,1.0f/3.5f);
            warpBtn.tooltips = "启动时间跳跃\n(ps.本功能不太完善)";
            gamePlay.onClickItem.Set(clickBtn_cb);
            have_init = true;
            KLog.Dbg("profView:ui init okay");
        }

        public void showTimeLeapDialog(){
            int num = 0;
            Wnd_Message wnd_Message = Wnd_Message.Show("一刻=0.1s", 2, delegate(string s){
			if (string.IsNullOrEmpty(s))
			{
				return;
			}
			int.TryParse(s, out num);
            doTimeLeap(num);
		    }, true, "输入时间跳跃刻数", 1, 0);
            
        }
        public void doTimeLeap(int tickCount){
           KLog.Dbg("profView:Start time warp!!");
            MainManager.Instance.Pause();
            MainManager.Instance.Play();
            GameMain.Instance.CloseAutoSave = true;
            //change
            // GameWatch.Instance.PlayOrPauseBossVoice(true);
            // List<Thing> thingList = ThingMgr.Instance.GetThingList(g_emThingType.Npc);
            // if (thingList != null)
            // {
            //     foreach (Thing thing in thingList)
            //     {
            //         Npc npc = (Npc)thing;
            //         if (!(npc.view == null) && npc.AtG && !npc.IsHide)
            //         {
            //             npc.view.ContinueAni();
            //         }
            //     }
            // }
            DateTime t0 = DateTime.Now;
            int tickCountp =tickCount;
            float preTimeScale = Time.timeScale;
            Time.timeScale = 0.1f;
            while(tickCount > 0){
                tickCount--;
                MainManager.Instance.Step(0.1f);
			    GlobleEvent.Instance.Step();
                MainManager.Instance.Render(0.1f);
            }
            TimeSpan dt2 = DateTime.Now - t0;
            KLog.Dbg("Time leap finished @ " + (tickCountp/(float)dt2.TotalSeconds).ToString() + "tps");
            Wnd_Message.Show("时间跳跃完成,速度:" +(tickCountp/(float)dt2.TotalSeconds).ToString() +"tps" , 1, null, true, "时间跳跃", 0, 0, string.Empty);
            Time.timeScale = preTimeScale;
            GameMain.Instance.CloseAutoSave = false;
            MainManager.Instance.Play();
            MainManager.Instance.Pause();
        }
    }
}