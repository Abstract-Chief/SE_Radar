class Rotor
{
    public IMyMotorAdvancedStator rotor;
    float angle_to;
    public float added;
    public float speed;
    public Rotor(IMyMotorAdvancedStator rotor_, float speed_, float added_)
    {
        rotor = rotor_;
        rotor.SetValueFloat("Velocity", 0);
        speed = speed_;
        added = added_;
    }
    public float GetAngleDiff(float angle)
    {
        double r = (angle - rotor.Angle + Math.PI) % (2 * Math.PI) - Math.PI;
        return (float)r < -(float)Math.PI ? (float)r + 2 * (float)Math.PI : (float)r;
    }
    public void Set(float angle)
    {
        angle_to = angle;
    }
    public void Handler()
    {
        if (angle_to == -1) return;
        float diff_a = GetAngleDiff(angle_to + MathHelper.ToRadians(added));
        float s = diff_a * speed;
        rotor.SetValueFloat("Velocity", s);
    }
}
class Turrel
{
    public string name;
    public Rotor h, v;
    public Vector3D point;
    Vector2D GetAnglesCoord_P_R(IMyTerminalBlock block, Vector3D block_coord, Vector3D point)
    {
        point.X -= block_coord.X;
        point.Y -= block_coord.Y;
        point.Z -= block_coord.Z;
        Vector3D X = block.WorldMatrix.Forward;
        Vector3D Y = block.WorldMatrix.Left;
        Vector3D Z = block.WorldMatrix.Up;
        double VX = point.Dot(X);
        double VY = point.Dot(Y);
        double VZ = point.Dot(Z);
        Vector2D result;
        result.X = Math.PI - Math.Atan2(VY, VX);
        result.Y = Math.Atan2(VZ, VX);
        return result;
    }
    public Turrel(string name_,Rotor h_rotor, Rotor v_rotor)
    {
        h = h_rotor;
        v = v_rotor;
        name = name_;
    }
    public void SetTo(Vector3D point_)
    {
        if (point_ != Vector3D.Zero)
        {
            point = point_;
        }
    }
    public void Handler()
    {
         if (h.rotor.IsFunctional && v.rotor.IsFunctional && point != Vector3D.Zero)
         {
        Vector3D vector_v_norm = Vector3D.Normalize(v.rotor.WorldMatrix.Up);
        Vector3D vector_h_norm = Vector3D.Normalize(h.rotor.WorldMatrix.Up);
        Vector3D point2=point-(vector_v_norm*5+vector_h_norm*5);
     Vector2D fh = GetAnglesCoord_P_R(h.rotor, h.rotor.GetPosition(), point2 );
     Vector2D fv = GetAnglesCoord_P_R(v.rotor, v.rotor.GetPosition(), point2 );
     h.Set((float)fh.X);
     v.Set((float)fv.X);
     h.Handler();
     v.Handler();
            }
    }
}
IMyCameraBlock camera;
IMyTextPanel panel;
int distance_cam_scan = 4000;
Program()
{
    parse();
    //Runtime.UpdateFrequency = UpdateFrequency.Update1;
}
Vector3D GetCameraPoint(IMyCameraBlock camera, int size)
{
    MyDetectedEntityInfo rayinfo = camera.Raycast(size, 0, 0);
    if (rayinfo.Type != MyDetectedEntityType.None)
    {
        return (Vector3D)rayinfo.HitPosition;
    }
    return Vector3D.Zero;
}
Rotor CreateRotor(string name, float speed, float added)
{
    IMyMotorAdvancedStator r = GridTerminalSystem.GetBlockWithName(name) as IMyMotorAdvancedStator;
    if (r == null) { Echo("Error: " + name); return null; }
    return new Rotor(r, speed, added);
}
List<Turrel> turrels=new List<Turrel>();
void parse()
{
    string msg = "Info Rotor Controler\n";
    turrels.Clear();
    string info = Me.CustomData;
    string[] lines = info.Split('\n');

    if (lines.Length < 2)
    {
        Echo("Not Found info\n");
        return;
    }
Echo("Parse");
    camera = GridTerminalSystem.GetBlockWithName(lines[0]) as IMyCameraBlock;
    panel = GridTerminalSystem.GetBlockWithName(lines[1]) as IMyTextPanel;
    if (panel == null)
    {
        Echo("Not Found panel with name |" + lines[1] + "|");
        return;
    };
    if (camera == null)
    {
        Echo("Not Found camera with name " + lines[0]);
        return;
    };

Echo("Parse");
    camera.EnableRaycast = true;
    for (int i = 2; i < lines.Length; i++)
    {
        string[] p = lines[i].Split(' ');//rotor_h rotor_v speed
        if (p.Length != 3) continue;
        Rotor h = CreateRotor(p[1], 30,0);
        Rotor v = CreateRotor(p[2], 30,0);
        if (v == null || h == null) continue;
        turrels.Add(new Turrel(p[0],h, v));
        msg += $"name: {p[0]} h: {p[1]} v: {p[2]}\n";
    }

Echo(lines[0]);

Echo(lines[1]);


Echo(lines[2]);    panel.WriteText(msg);
}
Turrel get_turel(string name)
{
    foreach( Turrel a in turrels)
    {
        if (a.name == name) return a;
    }
    return null;
}
public void Main(string args)
{
    Echo(args);
    if (args == "update") parse();
    string[] data = args.Split(' ');
    if (data[0] == "set")
    {
        Vector3D vector;
        if (data.Length == 5)
            vector = new Vector3D(double.Parse(data[2]), double.Parse(data[3]), double.Parse(data[4]));
        else{
            vector = GetCameraPoint(camera, distance_cam_scan);
            Echo("set camera");
        }
        Turrel turel = get_turel(data[1]);
        if(turel!=null){
            Echo("use turrel "+data[1]);
            turel.SetTo(vector);
        }else Echo("not found "+data[1]);
    }
    foreach( Turrel a in turrels)
    {
        a.Handler();
    }
}