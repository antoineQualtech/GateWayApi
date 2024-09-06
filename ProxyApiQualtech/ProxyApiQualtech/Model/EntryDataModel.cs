namespace ProxyApiQualtech.Model
{
    public class EntryDataModel
    {
        public bool IsEpicorApiEndPoint { get; set; }
        public string EpicorEnvironnement { get; set; }
        public string UrlEndPoint { get; set; }
        public Dictionary<string,string> InternalRequestHeaders { get; set; }
        public Dictionary<string, object> RequestBody { get; set; }
        public string RequestType { get; set; }
    }
}
