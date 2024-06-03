public class Camera
{
    public IMyCameraBlock camera;
    public MyDetectedEntityInfo last_scan;
    bool last_flag;
    long time_scan;

    public Camera(IMyCameraBlock camera_)
    {
        camera = camera_;
        camera.EnableRaycast=true;
        last_flag = false;
        time_scan = -1;
    }


    public string GetRayCastInfo()
    {
        string result = "";
        result += "Entity Id: " + last_scan.EntityId;
        result += "\nName: " + last_scan.Name;
        result += "\nType: " + last_scan.Type;
        result += "\nVelocity: " + last_scan.Velocity.ToString("0.000");
        result += "\nRelationship: " + last_scan.Relationship;
        result += "\nPosition: " + last_scan.HitPosition.ToString() + "\n";
        return result;
    }
    int OST_SCAN = 100;
    public int Scan(Vector3D coord)
    {
        time_scan = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        double distance = (coord - camera.GetPosition()).Length() + OST_SCAN;
        if (camera.AvailableScanRange < distance) return 1;
        MyDetectedEntityInfo rayinfo = camera.Raycast(coord+(Vector3D.Normalize(coord-camera.GetPosition())*OST_SCAN));
        if (rayinfo.Type != MyDetectedEntityType.SmallGrid && rayinfo.Type != MyDetectedEntityType.LargeGrid) return 2;
        last_scan = rayinfo;
        last_flag = true;
        if (last_flag) return 0;
        return 0;
    }
}
class Radar
{
    int p_l;
    float p_s;
    List<Camera> cameras;
    MyDetectedEntityInfo last_detect;
    IMyProgrammableBlock rotor_prog;
    public string name;
public bool use;
    long last_detect_t;
    long get_time()
    {
        if (last_detect_t == -1) return -1;
        return DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond - last_detect_t;

    }
    long get_pilings(double distance)
    {
        return (long)(1000 / (cameras.Count * 2) / (p_l * distance));
    }
    public Radar(List<Camera> cameras_, IMyProgrammableBlock rotor_prog_,string name_,int points_level,float point_size)
    {
        p_l = points_level;
        p_s = point_size;
        cameras = cameras_;
        rotor_prog = rotor_prog_;
        name = name_;
        last_detect_t = -1;
use=false;
    }
    List<Vector3D> get_points(Matrix matrix,Vector3D coord)
    {
        List<Vector3D> r=new List<Vector3D>();
        if (p_l == 1) r.Add(coord);
        if (p_l > 1){
            for (int i = 1; i < p_l; i++) {
                r.Add(Vector3D.Normalize(matrix.Up) * p_s * i + coord);
                r.Add(Vector3D.Normalize(-matrix.Up) * p_s * i + coord);
                r.Add(Vector3D.Normalize(-matrix.Left) * p_s * i + coord);
                r.Add(Vector3D.Normalize(matrix.Left) * p_s * i + coord);
            }
        }
        return r;

    }   
 public void FirstScan(MyDetectedEntityInfo info)
    {
 if (info.Type != MyDetectedEntityType.SmallGrid && info.Type != MyDetectedEntityType.LargeGrid) return;
        last_detect = info;
        last_detect_t = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
use=true;
    }
    public string Scan()
    {
        
        if (cameras.Count == 0) return "no cam";
        if (last_detect.Position == Vector3D.Zero) return "wait enemy";
        Vector3D coord_to = last_detect.Position + last_detect.Velocity * get_time() / 1000;
        rotor_prog.TryRun($"set {name} {coord_to.X} {coord_to.Y} {coord_to.Z}");
        if (get_time() < get_pilings((coord_to - cameras[0].camera.GetPosition()).Length()))
            return "wait piling";
        
        List<Vector3D> p = get_points(cameras[0].camera.WorldMatrix,coord_to);
        int i = 0;
string outs="";
        foreach(Camera cam in cameras)
        {
            int r = cam.Scan(p[i]);
outs+=$"{i}- {r}\n";
            if (r == 0)
            {
                last_detect = cam.last_scan;
                last_detect_t = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                break;
            }
            else if(r==2) i++;
if (i >= p.Count) break;
        }
        
        return outs+$"cool {last_detect.Name} {get_time()}";
    }

    public Vector3D GetPosition()
    {
        return cameras[0].camera.GetPosition();
    }
}
Radar GetNearestRadar(Vector3D point)
{
    int res_index=-1;
    double min = double.MaxValue;
    ScanerCam.GetPosition();
    for(int i = 0; i < radar_list.Count; i++)
    {
        if (radar_list[i].use) continue;
        double d = (radar_list[i].GetPosition() - point).Length();
        if (d< min) { min = d;res_index = i;}
    }
    if (res_index == -1) return null;
    return radar_list[res_index];
}
IMyCameraBlock ScanerCam;
public Program()
{
    ScanerCam = GridTerminalSystem.GetBlockWithName("scaner") as IMyCameraBlock;
    msg_prog = GridTerminalSystem.GetBlockWithName("msg_prog") as IMyProgrammableBlock;
    ScanerCam.EnableRaycast = true;
    string info = parse();
    push_msg("DriverMsg", "parse result: " + info, 15);
        Runtime.UpdateFrequency = UpdateFrequency.Update1;
    
}
List<Radar> radar_list = new List<Radar>();
IMyProgrammableBlock msg_prog;
public void push_msg(string pname,string text,float time)
{
    msg_prog.TryRun($"{text}|{pname}|{time}");
}
//name cam_group point_level point_size
string parse()
{
    string info = Me.CustomData;
    radar_list.Clear();
    if (info=="") return "null data";
    string[] data = info.Split('\n');
    if (data.Length <= 0) return "null data";
    IMyProgrammableBlock rotor = GridTerminalSystem.GetBlockWithName(data[0]) as IMyProgrammableBlock;
    int err = 0;
    for(int i=1;i<data.Length;i++)
    {
        string[] radar_info = data[i].Split(' ');
        Echo(data[i]+$"  {radar_info.Length}");
        if(radar_info.Length < 4) { err++;continue; }
        IMyBlockGroup g = GridTerminalSystem.GetBlockGroupWithName(radar_info[1]);
        List<IMyCameraBlock> cams = new List<IMyCameraBlock>();
        
        g.GetBlocksOfType<IMyCameraBlock>(cams);
        List<Camera> cams2 = new List<Camera>();
        foreach (IMyCameraBlock c in cams) cams2.Add(new Camera(c));
        Radar radar = new Radar(cams2, rotor, radar_info[0], int.Parse(radar_info[2]), float.Parse(radar_info[3]));
        radar_list.Add(radar);
    }
    return $"succeful {radar_list.Count}\nerror count: {err}";
}
double FirstD = 8000;
//bool flag = false;
void Main(string args)
{
    //if (args == "start") flag = false;
    //if (args == "stop") flag = true;
    if (args == "scan")
    {
        MyDetectedEntityInfo rayinfo = ScanerCam.Raycast(FirstD, 0, 0);
        if (rayinfo.Type == MyDetectedEntityType.SmallGrid || rayinfo.Type == MyDetectedEntityType.LargeGrid)
        {
            Vector3D position = rayinfo.HitPosition.Value+rayinfo.Velocity * 0.2f;
            Radar r = GetNearestRadar(position);
            push_msg("DriverMsg", $"set radar name {r.name}\n{position}\n{rayinfo.EntityId}" ,10);
            if (r == null) ;//not found radar
            else r.FirstScan(rayinfo);
        }
        else
 push_msg("DriverMsg", $"error set radar {rayinfo.Type}",10);
    }
    //else if(flag) {
         foreach(Radar r in radar_list)
        {
            r.Scan();
        }
   // }
}   