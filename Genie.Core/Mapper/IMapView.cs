namespace GenieClient.Mapper
{
    // Delegates (moved from MapForm nested types)
    public delegate void MapLoadedEventHandler();
    public delegate void ListResetEventHandler();
    public delegate void ClickNodeEventHandler(string zoneid, int nodeid);
    public delegate void ZoneIDChangeEventHandler(string zoneid);
    public delegate void ZoneNameChangeEventHandler(string zonename);
    public delegate void ToggleRecordEventHandler(bool toggle);
    public delegate void ToggleAllowDuplicatesEventHandler(bool toggle);
    public delegate void EchoMapPathEventHandler();
    public delegate void MoveMapPathEventHandler();

    public interface IMapView
    {
        string CharacterName { get; set; }
        string ZoneID { get; set; }
        string ZoneName { get; set; }
        string MapFile { get; }
        bool IsClosing { get; set; }
        bool Visible { get; }
        NodeList NodeList { get; }

        void SetNodeList(NodeList nl);
        bool LoadXML(string sPath);
        bool SaveXML(string sPath = "");
        void UpdateGraph(Node n, NodeList nl, Direction dir);
        void ClearMap();
        void UpdateMap();
        void UpdatePanelColor();
        void UpdateMainWindowTitle();
        void SetDestinationNode(Node n);
        void SelectNodes(string roomname, string roomdesc = "");
        void EraseRoom(Node n);
        void SetRecordToggle(bool toggle);
        void SetSnapToggle(bool toggle);
        void SetAllowDuplicatesToggle(bool toggle);
        void SetLockPositionsToggle(bool toggle);
        void Close();
        void Show();

        event MapLoadedEventHandler MapLoaded;
        event ListResetEventHandler ListReset;
        event ClickNodeEventHandler ClickNode;
        event ZoneIDChangeEventHandler ZoneIDChange;
        event ZoneNameChangeEventHandler ZoneNameChange;
        event ToggleRecordEventHandler ToggleRecord;
        event ToggleAllowDuplicatesEventHandler ToggleAllowDuplicates;
        event EchoMapPathEventHandler EchoMapPath;
        event MoveMapPathEventHandler MoveMapPath;
    }
}
