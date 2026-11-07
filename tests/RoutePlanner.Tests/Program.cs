using System.Diagnostics;
using System.Text;
using RoutePlanner.Core;

int passed = 0, failed = 0;
void Check(string name, Action test)
{
    try { test(); Console.WriteLine("PASS " + name); passed++; }
    catch (Exception ex) { Console.WriteLine("FAIL " + name + ": " + ex); failed++; }
}
void True(bool condition) { if (!condition) throw new Exception("Bedingung nicht erfüllt."); }
void Near(double actual, double expected, double epsilon = 1e-6)
{ if (Math.Abs(actual-expected) > epsilon) throw new Exception($"{actual} != {expected}"); }
void Reject(Action action)
{
    try { action(); } catch (InvalidDataException) { return; }
    throw new Exception("Ungültige Daten akzeptiert.");
}
RoutePlan Line() => new() { Points = [new(0,0), new(100,0)], Radius = 10 };
byte[] Enc(string value) => Encoding.UTF8.GetBytes(value);
string Json(RoutePlan route) => Encoding.UTF8.GetString(RouteJson.Serialize(new() { Routes = [route] }));

Check("Segment: senkrechter Abstand", () => Near(Geometry.PointToSegment(new(50,20), new(0,0), new(100,0)), 20));
Check("Segment: Projektion vor/nach Endpunkt", () => {
    Near(Geometry.PointToSegment(new(-3,4),new(0,0),new(100,0)),5);
    Near(Geometry.PointToSegment(new(103,4),new(0,0),new(100,0)),5);
});
Check("Segment: degenerierter Abschnitt", () => Near(Geometry.PointToSegment(new(4,5),new(1,1),new(1,1)),5));
Check("Segment: Punkt exakt auf Strecke", () => Near(Geometry.PointToSegment(new(33,33),new(0,0),new(100,100)),0));
Check("Route: null und leer", () => {
    True(double.IsPositiveInfinity(Geometry.PointToRoute(new(0,0),null,RadiusMode.Corridor)));
    True(double.IsPositiveInfinity(Geometry.PointToRoute(new(0,0),[],RadiusMode.Corridor)));
});
Check("Route: Einzelpunkt und Nullradius", () => {
    var index = new SpawnIndex([new("a",new(10,10)),new("b",new(10,11))]);
    True(index.Query(new() { Points=[new(10,10)], Radius=0 }).Select(p=>p.Id).SequenceEqual(["a"]));
});
Check("Route: nächster Abschnitt und Länge", () => {
    MapPoint[] points=[new(0,0),new(100,0),new(100,100)];
    Near(Geometry.PointToRoute(new(105,50),points,RadiusMode.Corridor),5); Near(Geometry.Length(points),200);
});
Check("Radius: inklusive Grenze, Rundung, außerhalb", () => {
    var points=new[] {new SpawnPoint("edge",new(50,10)),new("out",new(50,10.001)),new("round",new(50,10+1e-9))};
    True(new SpawnIndex(points).Query(Line()).Select(p=>p.Id).SequenceEqual(["edge","round"]));
});
Check("Radius: Wegpunkte statt Korridor", () => {
    var index=new SpawnIndex([new("middle",new(50,0))]); var r=Line();
    True(index.Query(r).Count==1);r.RadiusMode=RadiusMode.Waypoints;True(index.Query(r).Count==0);
});
Check("Planungszustand: leerer Name verhindert keinen Kartenfilter", () => {
    var r=Line() with {Name=""};new SpawnIndex(TestFixtures.Spawns).Query(r);
    Reject(()=>Validation.Route(r));
});
Check("Kartengrenzen: Ecken zulässig, NaN/Infinity/außerhalb ungültig", () => {
    True(Geometry.IsOnMap(new(0,0))&&Geometry.IsOnMap(new(3000,3000)));
    foreach(var p in new MapPoint[] {new(-.1,0),new(0,3000.1),new(double.NaN,0),new(0,double.PositiveInfinity)})
    {True(!Geometry.IsOnMap(p));Reject(()=>Validation.Route(new() {Points=[p]}));}
});
Check("Index: deterministisch identisch mit vollständiger Suche", () => {
    var random=new Random(41);var index=new SpawnIndex(TestFixtures.Spawns);
    foreach(var mode in Enum.GetValues<RadiusMode>()) for(int i=0;i<40;i++) {
        var r=new RoutePlan {Radius=random.NextDouble()*1500, RadiusMode=mode,
            Points=Enumerable.Range(0,random.Next(0,12)).Select(_=>new MapPoint(random.NextDouble()*3000,random.NextDouble()*3000)).ToList()};
        var brute=TestFixtures.Spawns.Where(p=>Geometry.PointToRoute(p.Position,r.Points,mode)<=r.Radius+Geometry.Epsilon).Select(p=>p.Id).Order().ToArray();
        True(index.Query(r).Select(p=>p.Id).Order().SequenceEqual(brute));
    }
});
Check("Import/Export: vollständiger Roundtrip", () => {
    var r=TestFixtures.Route();r.Name="Umlaute äöü";
    var doc=RouteJson.Parse(RouteJson.Serialize(new() {Routes=[r]}));
    True(doc.Routes[0].Id==r.Id&&doc.Routes[0].Name==r.Name&&doc.Routes[0].Points.SequenceEqual(r.Points));
});
Check("Schema: Migration v1/v2 nach v3 ohne simulierten Verlauf", () => {
    foreach(int version in new[]{1,2}) {
        var text=Json(Line()).Replace("\"schemaVersion\": 3","\"schemaVersion\": "+version);
        var node=System.Text.Json.Nodes.JsonNode.Parse(text)!.AsObject();
        node["demoHistory"]=new System.Text.Json.Nodes.JsonArray(new System.Text.Json.Nodes.JsonObject { ["simulated"]=true });
        var doc=RouteJson.Parse(Enc(node.ToJsonString()));
        True(doc.SchemaVersion==3&&doc.Activities.Count==0&&doc.Routes.Count==1);
        True(!Encoding.UTF8.GetString(RouteJson.Serialize(doc)).Contains("demoHistory"));
    }
});
Check("Schema: unbekannte und fehlende Version ablehnen", () => {
    Reject(()=>RouteJson.Parse(Enc("{\"schemaVersion\":99,\"routes\":[]}")));
    Reject(()=>RouteJson.Parse(Enc("{\"routes\":[]}")));
});
Check("Version: alte/fremde Karte warnbar und keine Spawns", () => {
    var r=Line() with {MapVersion="ALT"};True(r.MapVersion!=MapSpace.WorkspaceId);True(new SpawnIndex(TestFixtures.Spawns).Query(r).Count==0);
    True(RouteJson.Parse(Enc(Json(r))).Routes[0].MapVersion=="ALT");
});
Check("Import: defekt, null, doppelte Schlüssel, fremde Felder", () => {
    foreach(var text in new[]{"{","null","[]","{\"schemaVersion\":2,\"schemaVersion\":2,\"routes\":[]}",
        "{\"schemaVersion\":2,\"routes\":null}","{\"schemaVersion\":2,\"routes\":[],\"script\":\"x\"}"})
        Reject(()=>RouteJson.Parse(Enc(text)));
});
Check("Import: fehlende Identität und Koordinaten", () => {
    Reject(()=>RouteJson.Parse(Enc(Json(Line()).Replace("\"x\": 0,",""))));
    Reject(()=>RouteJson.Parse(Enc(Json(Line()).Replace("\"mapVersion\": \""+MapSpace.WorkspaceId+"\",",""))));
});
Check("Import: ungültige Radien und Enum", () => {
    foreach(double radius in new[]{-1,1501,double.NaN,double.PositiveInfinity})Reject(()=>Validation.Route(Line() with {Radius=radius}));
    Reject(()=>Validation.Route(Line() with {RadiusMode=(RadiusMode)10}));
});
Check("Import: Größen-, Punkt-, Namens- und ID-Limits", () => {
    Reject(()=>RouteJson.Parse(new byte[RouteJson.MaxBytes+1]));
    Reject(()=>Validation.Route(new(){Points=Enumerable.Repeat(new MapPoint(0,0),501).ToList()}));
    Reject(()=>Validation.Route(new(){Name="\n"}));Reject(()=>Validation.Route(new(){Id=Guid.Empty}));
    var r=Line();Reject(()=>RouteJson.Serialize(new(){Routes=[r,r]}));
    Reject(()=>RouteJson.Serialize(new(){Routes=Enumerable.Range(0,201).Select(_=>Line()).ToList()}));
});
Check("Duplikation: unabhängige Punkte und neue ID", () => {
    var original=Line();var copy=original.Copy(true);copy.Points[0]=new(10,10);
    True(original.Id!=copy.Id&&original.Points[0]==new MapPoint(0,0));
});
Check("Routenidentität bleibt beim Umbenennen und bei UI-Bindungen stabil", () => {
    var route=Line();HashSet<RoutePlan> selected=[route];int hash=route.GetHashCode();
    route.PropertyChanged+=(_,_)=>{};route.Name="Umbenannt";
    True(route.GetHashCode()==hash&&selected.Contains(route)&&selected.Contains(route.Copy()));
    True(!selected.Contains(route.Copy(true)));
});
Check("Speichern/Laden/Löschen: atomarer Dateiaustausch", () => {
    var directory=Path.Combine(Path.GetTempPath(),"route-tests-"+Guid.NewGuid());
    try {
        var store=new LocalStore(directory);True(store.Load().Routes.Count==0);
        var r=Line();store.Save(new(){Routes=[r]});True(store.Load().Routes[0].Id==r.Id);
        r.Name="Geändert";store.Save(new(){Routes=[r]});True(store.Load().Routes[0].Name=="Geändert");
        True(Directory.GetFiles(directory).Length==1);store.Delete();True(!File.Exists(store.LibraryPath));
    } finally {if(Directory.Exists(directory))Directory.Delete(directory,true);}
});
Check("Beschädigter Datenträger bleibt nach Load unverändert", () => {
    var directory=Path.Combine(Path.GetTempPath(),"route-tests-"+Guid.NewGuid());Directory.CreateDirectory(directory);
    try {var store=new LocalStore(directory);File.WriteAllText(store.LibraryPath,"broken");Reject(()=>store.Load());True(File.ReadAllText(store.LibraryPath)=="broken");}
    finally {Directory.Delete(directory,true);}
});
Check("Manueller Routengang: nur Aktionen und gemessene Zeit", () => {
    var route=TestFixtures.Route();var now=DateTimeOffset.UtcNow;
    var session=new ManualRouteSession(route,()=>now);
    now=now.AddSeconds(45);True(session.VisitedPoints==0&&session.ElapsedSeconds==45);
    RouteActivity? result=null;
    foreach(var point in route.Points) result=session.Advance();
    True(result is not null&&result.Completed&&result.VisitedPoints==route.Points.Count&&result.DurationSeconds==45);
    Near(result!.PlannedDistance,Geometry.Length(route.Points));
    var doc=RouteJson.Parse(RouteJson.Serialize(new(){Activities=[result]}));True(doc.Activities.Count==1);
    Reject(()=>session.Advance());Reject(()=>session.Finish());
});
Check("Manueller Routengang: Abbruch und unveränderliche Routenkopie", () => {
    var route=TestFixtures.Route();var now=DateTimeOffset.UtcNow;
    var session=new ManualRouteSession(route,()=>now);session.Advance();
    route.Points.Clear();now=now.AddSeconds(12);var result=session.Finish();
    True(!result.Completed&&result.VisitedPoints==1&&result.TotalPoints==5&&result.DurationSeconds==12);
    Reject(()=>new ManualRouteSession(new()));Reject(()=>new ManualRouteSession(Line() with {MapVersion="DEMO-ALT"}));
});
Check("Karten und Orte: echte Einträge speichern und passend filtern", () => {
    var map=new MapAsset {Spawns=[new("manual",new(50,1),true),new("static",new(50,20))]};
    var doc=RouteJson.Parse(RouteJson.Serialize(new(){Maps=[map]}));
    True(doc.Maps[0].Spawns[0].ManuallyConfirmed);
    True(new SpawnIndex(map.Spawns,map.Id).Query(Line()).Single().Id=="manual");
    True(new SpawnIndex(map.Spawns,"OTHER").Query(Line()).Count==0);
});
Check("Karten: Pfade, fehlende Metadaten und falsche Orte ablehnen", () => {
    Reject(()=>Validation.Map(new(){Id="../../outside"}));
    Reject(()=>Validation.Map(new(){Spawns=[new("p",new(-1,5))]}));
    Reject(()=>Validation.Map(new(){Spawns=[new("p",new(1,5)),new("p",new(2,5))]}));
    Reject(()=>RouteJson.Parse(Enc("{\"schemaVersion\":3,\"routes\":[],\"maps\":[{\"id\":\"WORKSPACE-3000-V1\"}]}")));
    Reject(()=>RouteJson.Serialize(new(){Maps=[new(),new()]}));
});
Check("Keine automatischen Daten bei neuem Start", () => {
    var doc=new LibraryDocument();True(doc.Routes.Count==0&&doc.Maps.Count==0&&doc.Activities.Count==0);
});
Check("Palette: nur Schwarz, Weiß und Grau in der Anwendungsoberfläche", () => {
    var paths=Directory.GetFiles("src/RoutePlanner.App","*.xaml").Concat(new[]{"src/RoutePlanner.App/MapView.cs","src/RoutePlanner.App/Dialogs.cs"});
    foreach(var path in paths) foreach(System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(File.ReadAllText(path),"#[0-9A-Fa-f]{6}([0-9A-Fa-f]{2})?(?![0-9A-Fa-f])")) {
        var rgb=match.Value[^6..];True(rgb[..2].Equals(rgb[2..4],StringComparison.OrdinalIgnoreCase)&&rgb[..2].Equals(rgb[4..6],StringComparison.OrdinalIgnoreCase));
    }
});
Check("Performance: 100.000 Punkte / 100 Filter, Referenzergebnis", () => {
    var random=new Random(72);var points=Enumerable.Range(0,100000).Select(i=>new SpawnPoint(i.ToString(),new(random.NextDouble()*3000,random.NextDouble()*3000))).ToArray();
    var r=TestFixtures.Route();var watch=Stopwatch.StartNew();var index=new SpawnIndex(points);double build=watch.Elapsed.TotalMilliseconds;
    var expected=points.Count(p=>Geometry.PointToRoute(p.Position,r.Points,r.RadiusMode)<=r.Radius+Geometry.Epsilon);
    watch.Restart();for(int i=0;i<100;i++)True(index.Query(r).Count==expected);watch.Stop();
    Console.WriteLine($"PROFILE index={build:0.0}ms query-mean={watch.Elapsed.TotalMilliseconds/100:0.00}ms " +
        $"managed={GC.GetTotalMemory(false)/1048576.0:0.0}MiB working-set={Environment.WorkingSet/1048576.0:0.0}MiB");
});
Console.WriteLine($"RESULT {passed} passed; {failed} failed");
return failed==0?0:1;

static class TestFixtures
{
    public static IReadOnlyList<SpawnPoint> Spawns {get;}=Enumerable.Range(0,600).Select(i=>new SpawnPoint(i.ToString(),new(100+(i*947+73)%2800,100+(i*613+181)%2800))).ToArray();
    public static RoutePlan Route()=>new(){Name="Teststrecke",Points=[new(600,2400),new(900,1900),new(1450,1600),new(1700,950),new(2400,650)]};
}
