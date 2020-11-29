using System;
using System.Threading;
using System.Collections.Generic;

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
        public string text;
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
            infoTxt.fontsize = 12;
            infoTxt.name = "profViewStat";
            infoTxt.autoSize = FairyGUI.AutoSizeType.Both;
            infoTxt.visible = true;
            Wnd_GameMain.Instance.AddChild(infoTxt);
        }
        public void update()
        {
            infoTxt.text = text;
            text = "";
        }

    }
    public class EventManager
    {
        public void registerEvents()
        {
            TickCounter tickCounter = new TickCounter();
            tickCounter.init();
            XiaWorld.EventMgr.Instance.RegisterEvent(XiaWorld.g_emEvent.UpdateFrame, tickCounter.countcb);
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

                UIHud.getInstance().text = "tps:" + tickPerSecond.ToString().Substring(0, 5) +
                                                "\nmspt:" + (UnityEngine.Time.deltaTime * 1000).ToString().Substring(0, 5) +
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
                data += "风水值:" + area_room.FengshuiValue.ToString() ; //GameDefine.RoomFengShuiRankMap :: -1e8..none..-1e5..worst..-6..verybad..-2..bad..3..good..8..verygood..13..best
                if( GameDefine.GetRoomFengshui(area_room.FengshuiValue) != XiaWorld.g_emFengshuiRank.Best){
                    float nextLevelValue = 0.0f;
                    foreach (KeyValuePair<float,XiaWorld.g_emFengshuiRank> kvp in GameDefine.RoomFengShuiRankMap)
                    {
                        if (kvp.Value == GameDefine.GetRoomFengshui(area_room.FengshuiValue) -1)
                        {
                            nextLevelValue = kvp.Key;
                        }
                    }
                    data += "(还差"+ (nextLevelValue - area_room.FengshuiValue).ToString()  +"到达下一级)\n";
                }

            }
            float l = curMap.GetLing(mouseGrid);
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
}